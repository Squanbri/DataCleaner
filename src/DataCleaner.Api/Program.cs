using System.Text.Json.Serialization;
using DataCleaner.Api.Data;
using DataCleaner.Api.Endpoints;
using DataCleaner.Api.Normalization;
using DataCleaner.Api.Services;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
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
builder.Services.AddScoped<ImportService>();
builder.Services.AddScoped<DeduplicationService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.MapImportEndpoints();

app.Run();
