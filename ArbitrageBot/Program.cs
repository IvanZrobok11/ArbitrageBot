using ArbitrageBot.BackgroundServices;
using ArbitrageBot.BackgroundServices.Base;
using ArbitrageBot.Extensions;
using BusinessLogic;
using BusinessLogic.Models;
using DAL;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Serilog;
using TelegramBot;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

builder.Services.AddDAL(builder.Configuration);

services.Configure<BackgroundServicesOption>(builder.Configuration.GetSection(BackgroundServicesOption.SectionKey));
services.AddHealthTrackedBackgroundServices();
services.AddHostedService<AssetsBackgroundService>();

// Add health checks
services.AddHealthChecks()
    .AddSqlite(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "sqlite")
    .AddBackgroundServicesCheck(name: "background_services", staleExecutionThreshold: TimeSpan.FromMinutes(10))
    .AddMemoryHealthCheck();

builder.Services.Configure<CryptoAPISettings>(builder.Configuration.GetSection(CryptoAPISettings.SectionKey));

builder.Services.AddControllers()
    .AddTelegramBotControllers()
    .AddJsonOptions((option) => option.JsonSerializerOptions.WriteIndented = true);

builder.Services.AddTelegramBot(builder.Configuration);
builder.Services.AddCryptoApiServices();

builder.Services.AddApiVersioning(options =>
{
    options.ReportApiVersions = true; // Add headers API-version in response
    options.AssumeDefaultVersionWhenUnspecified = true; // Use default version if not set
    options.DefaultApiVersion = new ApiVersion(1, 0); // Default version
    options.ApiVersionReader = new UrlSegmentApiVersionReader(); // Use URL API version reader
});

//var allowedOrigins = builder.Configuration.GetSection("allowedOrigins").Get<string[]>();
//builder.Services.AddCors(options =>
//{
//    options.AddDefaultPolicy(policy =>
//    {
//        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
//    });
//});

var logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("../data/logs/app-log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

// Registration Serilog like logger provider
builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger);

var app = builder.Build();

var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
var log = loggerFactory.CreateLogger("ArbitrageBotApp");
log.LogInformation("Application is running..");

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.MapHealthCheckEndpoint();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
//app.UseCors();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
