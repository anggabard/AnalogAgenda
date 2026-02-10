using AnalogAgenda.EmailSender;
using AnalogAgenda.Functions.Services;
using Configuration;
using Configuration.Sections;
using Database.Data;
using Database.Services;
using Database.Services.Interfaces;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Optional config for Docker/laptop deploy: bind without ValidateOnStart so app starts when sections are missing
builder.Services.AddOptions<AzureAd>().BindConfiguration("AzureAd");
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<AzureAd>>().Value);

builder.Services.AddOptions<ContainerRegistry>().BindConfiguration("ContainerRegistry");
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<ContainerRegistry>>().Value);

builder.Services.AddOptions<Smtp>().BindConfiguration("Smtp");
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<Smtp>>().Value);

builder.Services.AddStorageConfigBinding();
builder.Services.AddSecurityConfigBinding();

// Add DbContext for Azure Functions (align with AppHost/Aspire key "analogagendadb", fallback for Azure deploy)
var connectionString = builder.Configuration.GetSection("ConnectionStrings")["analogagendadb"]
    ?? builder.Configuration.GetSection("ConnectionStrings")["AnalogAgendaDb"];
builder.Services.AddDbContext<AnalogAgendaDbContext>(options =>
    options.UseSqlServer(connectionString));

// Email: use real sender when SMTP configured, else no-op (e.g. Docker deploy)
builder.Services.AddSingleton<IEmailSender>(sp =>
{
    var smtp = sp.GetRequiredService<Smtp>();
    return !string.IsNullOrWhiteSpace(smtp.Host)
        ? new EmailSender(smtp)
        : new NoOpEmailSender();
});

builder.Services.AddScoped<IDatabaseService, DatabaseService>();

// Container registry: use real service when configured, else no-op (e.g. Docker deploy)
builder.Services.AddSingleton<IContainerRegistryService>(sp =>
{
    var azureAd = sp.GetRequiredService<AzureAd>();
    var containerRegistry = sp.GetRequiredService<ContainerRegistry>();
    if (!string.IsNullOrWhiteSpace(containerRegistry.Name) && containerRegistry.RepositoryNames is { Count: > 0 })
        return new ContainerRegistryService(azureAd, containerRegistry);
    return new NoOpContainerRegistryService();
});

builder.Services.AddSingleton<IBlobService, BlobService>();
builder.Services.AddHttpClient();

builder.Build().Run();
