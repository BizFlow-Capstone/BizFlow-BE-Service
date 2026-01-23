using Autofac;
using Autofac.Extensions.DependencyInjection;
using BizFlow.Api.Common.Middleware;
using BizFlow.Application;
using BizFlow.Application.Common.Models;
using BizFlow.Infrastructure;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
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
            // Suppress default response
            context.HandleResponse();
            return Task.CompletedTask;
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

app.UseAuthentication();
app.UseJwtAuthenticationMiddleware();
app.UseAuthorization();

app.MapControllers();

app.Run();
