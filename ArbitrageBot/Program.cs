using ArbitrageBot.BackgroundServices;
using BusinessLogic;
using BusinessLogic.HttpClientPolicy;
using BusinessLogic.Models;
using DAL;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using TelegramBot.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDAL(builder.Configuration);

builder.Services.AddHostedService<AssetsBackgroundService>();
builder.Services.Configure<BackgroundServicesOption>(builder.Configuration.GetSection(BackgroundServicesOption.SectionKey));

builder.Services.Configure<CryptoAPISettings>(builder.Configuration.GetSection(CryptoAPISettings.SectionKey));

builder.Services.AddControllers()
    .AddTelegramBotControllers()
    .AddJsonOptions((option) => option.JsonSerializerOptions.WriteIndented = true);

builder.Services.AddTelegramBot(builder.Configuration).AddHttpClientPolicy(builder.Configuration);
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

var app = builder.Build();

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

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();
//app.UseCors();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
