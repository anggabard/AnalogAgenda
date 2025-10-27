using AnalogAgenda.EmailSender;
using Configuration;
using Database.Services;
using Database.Services.Interfaces;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services.AddSmtpConfigBinding();
builder.Services.AddAzureAdConfigBinding();
builder.Services.AddStorageConfigBinding();
builder.Services.AddContainerRegistryConfigBinding();

builder.Services.AddSingleton<IEmailSender, EmailSender>();
builder.Services.AddSingleton<ITableService, TableService>();
builder.Services.AddSingleton<IContainerRegistryService, ContainerRegistryService>();

builder.Build().Run();
