using Application;

using Infrastructure;

using Web.Middleware;

var builder = WebApplication.CreateBuilder(args);

var envFile = builder.Environment.IsDevelopment() ? ".env.local" : ".env";
DotNetEnv.Env.Load(Path.Combine(Directory.GetCurrentDirectory(), envFile));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Must be registered before all other middleware so every exception is caught
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.Run();
