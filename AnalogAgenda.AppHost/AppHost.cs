var builder = DistributedApplication.CreateBuilder(args);

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
});

builder.Build().Run();
