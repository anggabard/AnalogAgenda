using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Add SQL Server and Database
var sqlServer = builder.AddSqlServer("sql")
    .WithDataVolume()
    .WithHostPort(1433)
    .WithLifetime(ContainerLifetime.Persistent);

var database = sqlServer.AddDatabase("analogagendadb");

// Add Azure Storage - use Azurite emulator (AppHost is only used for local development)
// In production, apps are deployed separately with Dockerfiles and use Azure AD authentication
var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator(azurite =>
    {
        azurite.WithDataVolume();
        azurite.WithLifetime(ContainerLifetime.Persistent);
    });
// Use "analogagendastorage" as the blob resource name - this determines the connection string name
var blobStorage = storage.AddBlobs("analogagendastorage");

// Add the backend API - wait for database and blob storage to be ready
var backend = builder.AddProject<Projects.AnalogAgenda_Server>("analogagenda-server")
    .WithReference(database)
    .WaitFor(database)
    .PublishAsDockerFile()
    .WithHttpEndpoint(name: "backend-http");

// Add blob storage reference and wait for it
backend.WithReference(blobStorage)
    .WaitFor(blobStorage);

// Add the frontend Angular app - wait for backend to be ready
var frontend = builder.AddNpmApp("analogagenda-client", "../analogagenda.client")
    .PublishAsDockerFile()
    .WithReference(backend)
    .WaitFor(backend)
    .WithHttpEndpoint(name: "frontend-http");

frontend.WithEnvironment((context) =>
{
    var endpoint = frontend.GetEndpoint("frontend-http");
    var targetPort = endpoint.Property(EndpointProperty.TargetPort);
    context.EnvironmentVariables.Add("PORT", ReferenceExpression.Create($"{targetPort}"));
});

frontend.WithExternalHttpEndpoints();

backend.WithEnvironment((context) =>
{
    var frontendEndpoint = frontend.GetEndpoint("frontend-http");
    var frontendUrl = frontendEndpoint.Property(EndpointProperty.Url);
    context.EnvironmentVariables.Add("FRONTEND_URL", frontendUrl);
    
    // Configure storage for local development with Azurite
    // Aspire automatically provides the connection string via the reference as ConnectionStrings__analogagendastorage
    // BaseAzureService will automatically read from ConnectionStrings if Storage__ConnectionString is not set
    // In production (deployed separately), apps use Azure AD authentication from configuration
    context.EnvironmentVariables.Add("Storage__AccountName", "devstoreaccount1");
});

builder.Build().Run();
