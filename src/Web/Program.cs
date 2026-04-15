using System.Text;

using Application;
using Application.Common.Interfaces;

using Infrastructure;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

using Web.Endpoints;
using Web.Middleware;
using Web.Services;

var builder = WebApplication.CreateBuilder(args);

var envFile = builder.Environment.IsDevelopment() ? ".env.local" : ".env";
DotNetEnv.Env.Load(Path.Combine(Directory.GetCurrentDirectory(), envFile));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

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

// Must be registered before all other middleware so every exception is caught
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

AuthEndpoints.Map(app);

app.Run();
