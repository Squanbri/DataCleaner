using System.Text.Json.Serialization;
using DataCleaner.Api.Data;
using DataCleaner.Api.Endpoints;
using DataCleaner.Api.Messaging;
using DataCleaner.Api.Normalization;
using DataCleaner.Api.Services;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection(StorageOptions.SectionName));

builder.Services.AddOpenApi();
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(o => o
    .UseNpgsql(builder.Configuration.GetConnectionString("Default"))
    .UseSnakeCaseNamingConvention());

builder.Services.AddSingleton<PhoneNormalizer>();
builder.Services.AddSingleton<EmailNormalizer>();
builder.Services.AddSingleton<NameNormalizer>();
builder.Services.AddSingleton<BirthDateParser>();
builder.Services.AddSingleton<CityNormalizer>();
builder.Services.AddSingleton<RecordNormalizer>();
builder.Services.AddSingleton<SampleDataGenerator>();
builder.Services.AddSingleton<RabbitMqConnection>();
builder.Services.AddSingleton<ImportPublisher>();
builder.Services.AddHostedService<ImportProcessor>();

builder.Services.AddScoped<ImportService>();
builder.Services.AddScoped<DeduplicationService>();
builder.Services.AddScoped<QualityReportService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.MapImportEndpoints();
app.MapReportEndpoints();
app.MapControllerRoute("default", "{controller=Imports}/{action=Index}/{id?}");

app.Run();
