using Autofac;
using Autofac.Extensions.DependencyInjection;
using BizFlow.Api.Common.Middleware;
using BizFlow.Api.Hubs;
using BizFlow.Api.Services;
using BizFlow.Application;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Infrastructure;
using BizFlow.Infrastructure.Consumers;
using BizFlow.Infrastructure.Jobs;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using MassTransit;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json;
using Hangfire;
using Hangfire.MySql;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Hangfire Configuration
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseStorage(
        new MySqlStorage(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            new MySqlStorageOptions
            {
                TablesPrefix = "hf_"
            }
        )
    ));

// Add the processing server as IHostedService
builder.Services.AddHangfireServer();

// Add services to the container.

builder.Services.AddControllers(options =>
{
    // Register custom model binder for handling JSON strings in form data
    options.ModelBinderProviders.Insert(0, new BizFlow.Api.Common.ModelBinders.FormDataJsonModelBinderProvider());
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

// Configure the HTTP request pipeline.

app.UseGlobalExceptionMiddleware();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BizFlow Platform API v1");
        c.RoutePrefix = "swagger";
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
    });
}

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
app.UseJwtAuthenticationMiddleware();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

// Hangfire Dashboard
app.UseHangfireDashboard();

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

// Giá hiệu dụng theo cửa sổ giảm giá + đồng bộ Stripe Price — mỗi ngày 01:00 UTC
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

app.Run();
