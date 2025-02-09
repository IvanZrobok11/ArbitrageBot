using BusinessLogic;
using BusinessLogic.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.Configure<CryptoApiSettings>(builder.Configuration.GetSection("CryptoApi"));
builder.Services.AddControllers().AddJsonOptions((option) => option.JsonSerializerOptions.WriteIndented = true);
builder.Services.AddHttpClient();
builder.Services.AddCryptoApiServices();

//var allowedOrigins = builder.Configuration.GetSection("allowedOrigins").Get<string[]>();

//builder.Services.AddCors(options =>
//{
//    options.AddDefaultPolicy(policy =>
//    {
//        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
//    });
//});

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();
//app.UseCors();
app.UseAuthorization();

app.MapControllers();

app.Run();
