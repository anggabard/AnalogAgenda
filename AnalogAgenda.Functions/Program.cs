using AnalogAgenda.EmailSender;
using Configuration.Sections;
using Database.Services;
using Database.Services.Interfaces;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
// builder.Services
//     .AddApplicationInsightsTelemetryWorkerService()
//     .ConfigureFunctionsApplicationInsights();

//builder.Services.Configure<Smtp>(builder.Configuration.GetSection("Smtp"));

builder.Services.AddOptions<Smtp>().BindConfiguration("Smtp").ValidateOnStart();
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<Smtp>>().Value);

builder.Services.AddOptions<AzureAd>().BindConfiguration("AzureAd").ValidateOnStart();
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<AzureAd>>().Value);

builder.Services.AddOptions<Storage>().BindConfiguration("Storage").ValidateOnStart();
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<Storage>>().Value);

builder.Services.AddSingleton<IEmailSender, EmailSender>();
builder.Services.AddSingleton<ITableService, TableService>();

builder.Build().Run();
