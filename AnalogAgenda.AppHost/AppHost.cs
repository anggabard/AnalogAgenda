var builder = DistributedApplication.CreateBuilder(args);

// Docker Compose environment for deployment to Ubuntu server (aspire deploy / aspire do prepare-compose)
var compose = builder.AddDockerComposeEnvironment("compose");

// Add SQL Server SA password parameter (stored in user secrets)
var sqlPassword = builder.AddParameter("sql-password", secret: true);

// Add SQL Server and Database
// Pin to SQL Server 2022-latest to prevent permission issues from image updates
// Use WithDataVolume() for automatic volume management with correct permissions
var sqlServer = builder.AddSqlServer("sql", password: sqlPassword)
    .WithImage("mssql/server", "2022-latest")
    .WithDataVolume()
    .WithHostPort(1433)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithEnvironment("ACCEPT_EULA", "Y");

var database = sqlServer.AddDatabase("analogagendadb");

sqlServer.PublishAsDockerComposeService(static (_, _) => { });

// Add Azure Storage - use Azurite emulator (AppHost is only used for local development)
// In production, apps are deployed separately with Dockerfiles and use Azure AD authentication
var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator(azurite =>
    {
        azurite.WithDataVolume();
        azurite.WithLifetime(ContainerLifetime.Persistent);
        azurite.WithBlobPort(10000);
        azurite.WithQueuePort(10001);
        azurite.WithTablePort(10002);
    });

// Use "analogagendastorage" as the blob resource name - this determines the connection string name
var blobStorage = storage.AddBlobs("analogagendastorage");

// For Docker Compose deployment, add explicit Azurite container (AzureStorageResource is not IComputeResource)
var azuriteContainer = builder.AddContainer("azurite", "mcr.microsoft.com/azure-storage/azurite", "latest")
    .WithEndpoint(port: 10000, targetPort: 10000, name: "blob")
    .WithEndpoint(port: 10001, targetPort: 10001, name: "queue")
    .WithEndpoint(port: 10002, targetPort: 10002, name: "table")
    .WithVolume("azurite-data", "/data")
    .PublishAsDockerComposeService(static (_, _) => { });

// Add the backend API - wait for database and blob storage to be ready
var backend = builder.AddProject<Projects.AnalogAgenda_Server>("analogagenda-server")
    .WithReference(database)
    .WaitFor(database)
    .WithReference(blobStorage)
    .WaitFor(blobStorage)
    .PublishAsDockerFile()
    .PublishAsDockerComposeService(static (_, _) => { })
    .WithHttpEndpoint(name: "backend-http");

// Add the frontend Angular app - wait for backend to be ready
var frontend = builder.AddNpmApp("analogagenda-client", "../analogagenda.client")
    .WithReference(backend)
    .WaitFor(backend)
    .PublishAsDockerFile()
    .PublishAsDockerComposeService(static (_, _) => { })
    .WithHttpEndpoint(name: "frontend-http");

frontend.WithEnvironment((context) =>
{
    var endpoint = frontend.GetEndpoint("frontend-http");
    var targetPort = endpoint.Property(EndpointProperty.TargetPort);
    context.EnvironmentVariables.Add("PORT", ReferenceExpression.Create($"{targetPort}"));
    return Task.CompletedTask;
});

frontend.WithExternalHttpEndpoints();

backend.WithEnvironment((context) =>
{
    var frontendEndpoint = frontend.GetEndpoint("frontend-http");
    var frontendUrl = frontendEndpoint.Property(EndpointProperty.Url);
    context.EnvironmentVariables.Add("FRONTEND_URL", frontendUrl);
    return Task.CompletedTask;
});

// Azure Functions - required for the stack (timer jobs, garbage collection, etc.)
// Uses same DB and storage; connection string key aligned to "analogagendadb" in Program.cs
builder.AddProject<Projects.AnalogAgenda_Functions>("analogagenda-functions")
    .WithReference(database)
    .WaitFor(database)
    .WithReference(blobStorage)
    .WaitFor(blobStorage)
    .PublishAsDockerComposeService(static (_, _) => { })
    .WithHttpEndpoint(name: "functions-http", port: 7071);

builder.Build().Run();
