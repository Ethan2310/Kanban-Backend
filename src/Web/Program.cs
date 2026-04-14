using Application;

using Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Load .env.local in development, .env in production
var envFile = builder.Environment.IsDevelopment() ? ".env.local" : ".env";
DotNetEnv.Env.Load(Path.Combine(Directory.GetCurrentDirectory(), envFile));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.Run();
