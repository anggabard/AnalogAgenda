using AnalogAgenda.EmailSender;
using Configuration;
using Database.Data;
using Database.Services;
using Database.Services.Interfaces;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Configure max request body size to handle large photo uploads
// Base64 encoding increases size by ~33%, so 30MB files become ~40MB when encoded
builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 60_000_000; // 60MB to handle 30MB files when base64 encoded
});

builder.Services.AddSmtpConfigBinding();
builder.Services.AddAzureAdConfigBinding();
builder.Services.AddStorageConfigBinding();
builder.Services.AddSecurityConfigBinding();
builder.Services.AddContainerRegistryConfigBinding();

// Add DbContext for Azure Functions
var connectionString = builder.Configuration.GetConnectionString("AnalogAgendaDb");
if (!string.IsNullOrEmpty(connectionString))
{
    builder.Services.AddDbContext<AnalogAgendaDbContext>(options =>
        options.UseSqlServer(connectionString));
}

builder.Services.AddSingleton<IEmailSender, EmailSender>();
builder.Services.AddScoped<IDatabaseService, DatabaseService>();
builder.Services.AddSingleton<IContainerRegistryService, ContainerRegistryService>();
builder.Services.AddSingleton<IBlobService, BlobService>();
builder.Services.AddHttpClient();

// Configure JSON serialization options globally for case-insensitive property matching
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

builder.Build().Run();
