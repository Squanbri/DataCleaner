using DataCleaner.Api.Data;
using DataCleaner.Api.Normalization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDbContext>(o => o
    .UseNpgsql(builder.Configuration.GetConnectionString("Default"))
    .UseSnakeCaseNamingConvention());

builder.Services.AddSingleton<PhoneNormalizer>();
builder.Services.AddSingleton<EmailNormalizer>();
builder.Services.AddSingleton<NameNormalizer>();
builder.Services.AddSingleton<BirthDateParser>();
builder.Services.AddSingleton<CityNormalizer>();
builder.Services.AddSingleton<RecordNormalizer>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();
