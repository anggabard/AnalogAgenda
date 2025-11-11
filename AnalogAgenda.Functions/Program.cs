using AnalogAgenda.EmailSender;
using Configuration;
using Database.Data;
using Database.Services;
using Database.Services.Interfaces;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();


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
