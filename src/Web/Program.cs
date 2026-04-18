using System.Text;
using System.Text.Json.Serialization;

using Application;
using Application.Common.Interfaces;

using Infrastructure;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

using Scalar.AspNetCore;

using Serilog;
using Serilog.Events;

using Web.Endpoints;
using Web.Middleware;
using Web.OpenApi;
using Web.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Define CORS Policy Name
const string CorsPolicy = "AllowFlutterFrontend";

var envFile = builder.Environment.IsDevelopment() ? ".env.local" : ".env";
DotNetEnv.Env.Load(Path.Combine(Directory.GetCurrentDirectory(), envFile));
builder.Configuration.AddEnvironmentVariables();

var corsOrigins = (builder.Configuration["Cors:AllowedOrigins"]
    ?? throw new InvalidOperationException("Cors:AllowedOrigins is not configured."))
    .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

var allowAnyLocalhostOrigin = builder.Configuration.GetValue<bool>("Cors:AllowAnyLocalhostOrigin");

if (!allowAnyLocalhostOrigin && corsOrigins.Length == 0)
    throw new InvalidOperationException("Cors:AllowedOrigins must contain at least one origin.");

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog((context, config) =>
{
    var logsPath = Path.Combine(context.HostingEnvironment.ContentRootPath, "..", "..", "logs", "log-.txt");
    config
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File(
            path: logsPath,
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 14,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}");
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// 2. Register CORS services
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
    {
        if (allowAnyLocalhostOrigin)
        {
            policy.SetIsOriginAllowed(origin =>
            {
                if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                    return false;

                return uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                    || uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase);
            });
        }
        else
        {
            policy.WithOrigins(corsOrigins);
        }

        policy.AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SupportNonNullableReferenceTypes();
    options.SchemaFilter<StringEnumSchemaFilter>();
});

var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSerilogRequestLogging();

// 3. Enable CORS (Must be before Authentication and Endpoints)
app.UseCors(CorsPolicy);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.MapScalarApiReference(options =>
    {
        options.OpenApiRoutePattern = "/swagger/v1/swagger.json";
    });
}

app.UseAuthentication();
app.UseAuthorization();

AuthEndpoints.Map(app);
UserEndpoints.Map(app);
BoardEndpoints.Map(app);

app.Run();
