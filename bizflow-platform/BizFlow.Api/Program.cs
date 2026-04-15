using Autofac;
using Autofac.Extensions.DependencyInjection;
using BizFlow.Api.Common.Filters;
using BizFlow.Api.Common.Middleware;
using BizFlow.Api.Hubs;
using BizFlow.Api.Services;
using BizFlow.Application;
using BizFlow.Application.Common.Configuration;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Application.Interfaces.Repositories;
using Resend;
using BizFlow.Infrastructure;
using BizFlow.Infrastructure.Consumers;
using BizFlow.Infrastructure.Jobs;
using BizFlow.Infrastructure.Services;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using MassTransit;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json;
using Hangfire;
using Hangfire.MySql;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);

var defaultConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var isHangfireEnabled = IsHangfireStorageReachable(defaultConnectionString, out var hangfireDisableReason);

// Pre-create hf_DistributedLock with PRIMARY KEY before Hangfire initializes.
// Aiven (and some managed MySQL) enforces sql_require_primary_key; Hangfire.MySql v2
// generates this table without a PK, causing schema bootstrap to fail on those hosts.
// Using IF NOT EXISTS makes this a no-op on subsequent startups.
if (isHangfireEnabled)
{
    try
    {
        using var preConn = new MySqlConnection(defaultConnectionString);
        await preConn.OpenAsync();
        using var preCmd = preConn.CreateCommand();
        // Hangfire.MySql v2 requires: Resource (PK), CreatedAt, ExpireAt.
        // Aiven enforces sql_require_primary_key so we must create with an explicit PK.
        preCmd.CommandText = """
            CREATE TABLE IF NOT EXISTS `hf_DistributedLock` (
              `Resource` varchar(100) NOT NULL,
              `CreatedAt` datetime NOT NULL,
              `ExpireAt` datetime NOT NULL,
              CONSTRAINT `PK_HangFire_DistributedLock` PRIMARY KEY (`Resource`)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
            """;
        await preCmd.ExecuteNonQueryAsync();
    }
    catch (Exception ex)
    {
        // Non-fatal: log and let Hangfire surface the real error if table truly can't be created
        Console.Error.WriteLine($"[Hangfire pre-migration warning] {ex.Message}");
    }
}

// Hangfire Configuration
if (isHangfireEnabled)
{
    builder.Services.AddHangfire(configuration => configuration
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseStorage(
            new MySqlStorage(
                defaultConnectionString!,
                new MySqlStorageOptions
                {
                    TablesPrefix = "hf_"
                }
            )
        ));

    // Add the processing server as IHostedService
    builder.Services.AddHangfireServer();
}

// Add services to the container.

builder.Services.AddControllers(options =>
{
    // Register custom model binder for handling JSON strings in form data
    options.ModelBinderProviders.Insert(0, new BizFlow.Api.Common.ModelBinders.FormDataJsonModelBinderProvider());
    options.Filters.Add<RequireAuthenticatedFreeFeatureFilter>();
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    // options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var messageService = context.HttpContext.RequestServices.GetRequiredService<IMessageService>();
        var fieldErrors = new Dictionary<string, List<string>>();
        foreach (var (fieldKey, state) in context.ModelState)
        {
            foreach (var error in state.Errors)
            {
                var raw = string.IsNullOrWhiteSpace(error.ErrorMessage)
                    ? error.Exception?.Message
                    : error.ErrorMessage;
                var keyOrText = string.IsNullOrWhiteSpace(raw) ? MessageKeys.ValidationError : raw;
                var localized = messageService.GetMessage(keyOrText);
                var key = string.IsNullOrEmpty(fieldKey) ? "." : fieldKey;
                if (!fieldErrors.TryGetValue(key, out var list))
                {
                    list = new List<string>();
                    fieldErrors[key] = list;
                }

                list.Add(localized);
            }
        }

        var errorsObj = fieldErrors.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray());
        var response = ApiResponse.ErrorResponse(
            MessageKeys.ValidationError,
            messageService.GetMessage(MessageKeys.ValidationError),
            errorsObj);
        return new BadRequestObjectResult(response);
    };
});

builder.Services.AddEndpointsApiExplorer();

// Add HttpContextAccessor
builder.Services.AddHttpContextAccessor();
builder.Services.AddSignalR();
builder.Services.AddScoped<INotificationRealtimePublisher, SignalRNotificationRealtimePublisher>();

builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();
    x.AddConsumer<NotificationDispatchRequestedConsumer>();

    x.AddConfigureEndpointsCallback((context, endpointName, cfg) =>
    {
        cfg.UseMessageRetry(retry => retry.Interval(3, TimeSpan.FromSeconds(5)));
        cfg.UseDelayedRedelivery(redelivery => redelivery.Interval(2, TimeSpan.FromSeconds(15)));
    });

    x.UsingRabbitMq((context, cfg) =>
    {
        var host = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
        var port = int.TryParse(builder.Configuration["RabbitMQ:Port"], out var rabbitPort) ? rabbitPort : 5672;
        var username = builder.Configuration["RabbitMQ:Username"] ?? "guest";
        var password = builder.Configuration["RabbitMQ:Password"] ?? "guest";

        cfg.Host(host, (ushort)port, "/", h =>
        {
            h.Username(username);
            h.Password(password);
        });

        cfg.UseDelayedMessageScheduler();

        cfg.ConfigureEndpoints(context);
    });
});
//===================================================================================
// Use Autofac as the DI container
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    containerBuilder.RegisterModule(new ApplicationModule());
    containerBuilder.RegisterModule(new InfrastructureModule(builder.Configuration));
});

var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
if (jwtSettings == null || string.IsNullOrEmpty(jwtSettings.Secret))
{
    throw new InvalidOperationException("JWT Secret Key is not configured in JwtSettings:Secret");
}

var key = Encoding.UTF8.GetBytes(jwtSettings.Secret);

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.Configure<GoogleAuthConfig>(builder.Configuration.GetSection("GoogleAuth"));
builder.Services.Configure<FirebaseAuthConfig>(builder.Configuration.GetSection("FirebaseAuth"));
builder.Services.Configure<PaginationSettings>(builder.Configuration.GetSection(PaginationSettings.SectionName));
builder.Services.Configure<GeneralLedgerSettings>(builder.Configuration.GetSection(GeneralLedgerSettings.SectionName));
builder.Services.Configure<ImageSettings>(builder.Configuration.GetSection(ImageSettings.SectionName));
builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection(CloudinarySettings.SectionName));
builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection(StripeSettings.SectionName));
builder.Services.Configure<ResendSettings>(builder.Configuration.GetSection(ResendSettings.SectionName));
builder.Services.Configure<AppPublicUrlsOptions>(builder.Configuration.GetSection(AppPublicUrlsOptions.SectionName));
builder.Services.AddHttpClient<IResend, ResendClient>();
builder.Services.AddOptions<ResendClientOptions>()
    .Configure<IOptions<ResendSettings>>((opts, resendSection) =>
    {
        var s = resendSection.Value;
        if (!string.IsNullOrWhiteSpace(s.ApiKey))
        {
            opts.ApiToken = s.ApiKey;
        }

        opts.ThrowExceptions = false;
    });
builder.Services.AddScoped<IEmailSender, ResendEmailSender>();
builder.Services.Configure<FreePlanOptions>(builder.Configuration.GetSection(FreePlanOptions.SectionName));
builder.Services.Configure<AccountPurgeOptions>(builder.Configuration.GetSection(AccountPurgeOptions.SectionName));
builder.Services.Configure<AiServiceSettings>(builder.Configuration.GetSection(AiServiceSettings.SectionName));

// AI Service HTTP Client
builder.Services.AddHttpClient("AiService", (sp, client) =>
{
    var aiSettings = builder.Configuration.GetSection(AiServiceSettings.SectionName).Get<AiServiceSettings>()
                     ?? new AiServiceSettings();
    client.BaseAddress = new Uri(aiSettings.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(aiSettings.TimeoutSeconds);
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            if (context.Exception is SecurityTokenExpiredException)
            {
                context.Response.Headers.Append("IS-TOKEN-EXPIRED", "true");
            }
            return Task.CompletedTask;
        },
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrWhiteSpace(accessToken) && path.StartsWithSegments("/hubs/notifications"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            context.HandleResponse();

            var isTokenExpired = context.Response.Headers.ContainsKey("IS-TOKEN-EXPIRED");
            var messageKey = isTokenExpired ? MessageKeys.TokenExpired : MessageKeys.Unauthorized;
            var messageService = context.HttpContext.RequestServices.GetRequiredService<IMessageService>();

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";

            var response = new ApiResponse
            {
                Success = false,
                MessageCode = messageKey,
                Message = messageService.GetMessage(messageKey),
                Timestamp = DateTime.UtcNow
            };

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            return context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
        }
    };

})
.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
{
    var googleConfig = builder.Configuration.GetSection("GoogleAuth").Get<GoogleAuthConfig>();
    if (googleConfig != null)
    {
        options.ClientId = googleConfig.ClientId;
        options.ClientSecret = googleConfig.ClientSecret;
        options.CallbackPath = "/api/auth/google-callback";
    }
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthJwtConstants.Policies.PasswordReset, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim(AuthJwtConstants.PurposeClaimType, AuthJwtConstants.PasswordResetPurpose);
        policy.RequireClaim(AuthJwtConstants.PasswordResetNonceClaimType);
    });
});

// Swagger Configuration with Bearer Token
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BizFlow Platform API",
        Version = "v1",
        Description = "API for BizFlow Platform - Support Digital Transformation",
        Contact = new OpenApiContact
        {
            Name = "Minh-Thien Le",
            Email = "lmthien.30@gmail.com"
        }
    });

    // JWT Bearer Security Definition
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.\n\n" +
                      "Enter 'Bearer' [space] and then your token in the text input below.\n\n" +
                      "Example: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Map DateOnly to string format
    c.MapType<DateOnly>(() => new OpenApiSchema
    {
        Type = "string",
        Format = "date"
    });

    // Include XML comments (optional)
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Enable Swagger annotations
    c.EnableAnnotations();
});


builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true; 
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/json", "application/xml", "text/plain", "image/svg+xml" }); 
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});

builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
   
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});

builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(builder => builder.Expire(TimeSpan.FromSeconds(10)));

    options.AddPolicy("PublicData", builder =>
        builder.Expire(TimeSpan.FromMinutes(5))
               .SetVaryByQuery("*")); // Vary cache by query parameters
});

// CORS Configuration - Allow All
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

//===================================================================================
var app = builder.Build();

try
{
    using (var scope = app.Services.CreateScope())
    {
        var subscriptionService = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();
        await subscriptionService.EnsureFreePlanSetupAsync();
    }
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "EnsureFreePlanSetupAsync failed; API may still run but free subscription provisioning can be incomplete.");
}

// Configure the HTTP request pipeline.

app.UseGlobalExceptionMiddleware();
app.UseMiddleware<CharsetMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "BizFlow Platform API v1");
    c.RoutePrefix = "swagger";
    c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
});

app.UseHttpsRedirection();
// Hangfire HTML pages are sensitive to middleware caching/compression.
// Exclude /hangfire endpoints to avoid "ERR_CONTENT_DECODING_FAILED" in browser.
app.UseWhen(ctx => !ctx.Request.Path.StartsWithSegments("/hangfire"), app =>
{
    app.UseResponseCompression();
});

// Use CORS
app.UseCors("AllowAll");
app.UseWhen(ctx => !ctx.Request.Path.StartsWithSegments("/hangfire"), app =>
{
    app.UseOutputCache();
});
app.UseAuthentication();
app.UseActiveAccountMiddleware();
app.UsePasswordResetTokenRestriction();
app.UseJwtAuthenticationMiddleware();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

if (isHangfireEnabled)
{
    // Hangfire Dashboard
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = app.Environment.IsDevelopment()
            ? [new AllowHangfireDashboardAuthorizationFilter()]
            : [new AdminJwtHangfireDashboardAuthorizationFilter(
                jwtSettings.Secret,
                jwtSettings.Issuer,
                jwtSettings.Audience
              )]
    });

    // Remove recurring entries whose job types were deleted (avoids TypeLoadException on trigger).
    RecurringJob.RemoveIfExists("subscription-free-backfill-monthly");

    // Schedule Recurring Job
    RecurringJob.AddOrUpdate<ImageCleanupJob>(
        "image-cleanup",
        job => job.ExecuteAsync(),
         "5 17 * * *" // 17:05 Vietnam time (UTC+7)
                      //"5 17 * * *", // Every day at 17:05 Vietnam time (UTC+7)
                      //TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time") // Use "SE Asia Standard Time" for Vietnam time
                      // "* * * * *" // Every minute
    );

    RecurringJob.AddOrUpdate<SubscriptionExpiryCheckJob>(
        "subscription-expiry-check",
        job => job.ExecuteAsync(),
        "0 * * * *");

    RecurringJob.AddOrUpdate<SubscriptionReminderJob>(
        "subscription-reminder",
        job => job.ExecuteAsync(),
        "0 1 * * *");

    RecurringJob.AddOrUpdate<UsageSnapshotJob>(
        "usage-snapshot",
        job => job.ExecuteAsync(),
        "0 17 * * *");

    RecurringJob.AddOrUpdate<FirestoreSyncJob>(
        "firestore-sync",
        job => job.ExecuteAsync(),
        "0 */6 * * *");

    RecurringJob.AddOrUpdate<StaleTransactionCleanupJob>(
        "stale-txn-cleanup",
        job => job.ExecuteAsync(),
        "30 3 * * *");

    RecurringJob.AddOrUpdate<StripePendingReconcileJob>(
        "stripe-pending-reconcile",
        job => job.ExecuteAsync(),
        "*/15 * * * *");

    RecurringJob.AddOrUpdate<StripeRefundReconcileJob>(
        "stripe-refund-reconcile",
        job => job.ExecuteAsync(),
        "7 */2 * * *");

    // Reconcile effective price by discount window and sync Stripe Price daily at 01:00 UTC.
    RecurringJob.AddOrUpdate<SubscriptionPlanStripeCatalogSyncJob>(
        "subscription-plan-stripe-catalog-sync",
        job => job.ExecuteAsync(),
        "0 1 * * *");
    RecurringJob.AddOrUpdate<ScheduledNotificationDispatchJob>(
        "scheduled-notification-dispatch",
        job => job.ExecuteAsync(),
        "* * * * *"
    );

    RecurringJob.AddOrUpdate<NotificationOutboxJob>(
        "notification-outbox",
        job => job.ExecuteAsync(),
        "* * * * *"
    );

    RecurringJob.AddOrUpdate<NotificationRetentionJob>(
        "notification-retention",
        job => job.ExecuteAsync(),
        "0 2 * * *"
    );

    RecurringJob.AddOrUpdate<OtpCleanupJob>(
        "otp-cleanup",
        job => job.ExecuteAsync(),
        "0 * * * *" // Hourly
    );

    RecurringJob.AddOrUpdate<AccountHardDeleteJob>(
        "account-hard-delete",
        job => job.ExecuteAsync(),
        "15 * * * *");

    // ── AI Nightly Jobs ──────────────────────────────────────────
    RecurringJob.AddOrUpdate<AiForecastJob>(
        "ai-forecast",
        job => job.ExecuteAsync(),
        "0 18 * * *"); // 01:00 Vietnam time (UTC+7)

    RecurringJob.AddOrUpdate<AiAnomalyPatternJob>(
        "ai-anomaly-pattern",
        job => job.ExecuteAsync(),
        "0 19 * * *"); // 02:00 Vietnam time (UTC+7)

    RecurringJob.AddOrUpdate<AiReorderJob>(
        "ai-reorder",
        job => job.ExecuteAsync(),
        "0 20 * * *"); // 03:00 Vietnam time (UTC+7)

    RecurringJob.AddOrUpdate<AiProductInsightsJob>(
        "ai-product-insights",
        job => job.ExecuteAsync(),
        "30 20 * * *"); // 03:30 Vietnam time (UTC+7)
}
else
{
    app.Logger.LogWarning("Hangfire is disabled: {Reason}", hangfireDisableReason);
}

// One-time startup backfill: ensure all profiles have an active subscription (free if missing).
using (var startupScope = app.Services.CreateScope())
{
    var startupSubscriptionService = startupScope.ServiceProvider.GetRequiredService<ISubscriptionService>();
    var startupLogger = startupScope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("StartupFreeSubscriptionBackfill");

    try
    {
        var successCount = await startupSubscriptionService.EnsureFreeSubscriptionsForOwnersWithoutActiveAsync();
        startupLogger.LogInformation(
            "Startup free-subscription backfill completed. Success={SuccessCount}",
            successCount);
    }
    catch (Exception ex)
    {
        startupLogger.LogError(ex, "Startup free-subscription backfill failed");
    }
}

app.Run();

static bool IsHangfireStorageReachable(string? connectionString, out string reason)
{
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        reason = "ConnectionStrings:DefaultConnection is missing or empty.";
        return false;
    }

    try
    {
        var connectionStringBuilder = new MySqlConnectionStringBuilder(connectionString)
        {
            ConnectionTimeout = 3
        };

        using var connection = new MySqlConnection(connectionStringBuilder.ConnectionString);
        connection.Open();
        reason = string.Empty;
        return true;
    }
    catch (Exception ex)
    {
        reason = $"unable to connect to MySQL ({ex.Message})";
        return false;
    }
}
