using Autofac;
using Autofac.Extensions.DependencyInjection;
using BizFlow.Api.Common.Middleware;
using BizFlow.Application;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Infrastructure;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json;
using Hangfire;
using Hangfire.MySql;
using BizFlow.Infrastructure.Jobs;
using Microsoft.AspNetCore.ResponseCompression;

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
builder.Services.AddEndpointsApiExplorer();

// Add HttpContextAccessor
builder.Services.AddHttpContextAccessor();
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
app.UseResponseCompression();

// Use CORS
app.UseCors("AllowAll");
app.UseOutputCache();
app.UseAuthentication();
app.UseJwtAuthenticationMiddleware();
app.UseAuthorization();

app.MapControllers();

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

app.Run();
