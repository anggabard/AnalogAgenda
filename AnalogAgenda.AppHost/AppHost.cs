var builder = DistributedApplication.CreateBuilder(args);

// Add the backend API
var backend = builder.AddProject<Projects.AnalogAgenda_Server>("analogagenda-server")
    .PublishAsDockerFile()
    .WithHttpEndpoint(name: "backend-http");

// Add the Azure Functions
var functions = builder.AddAzureFunctionsProject<Projects.AnalogAgenda_Functions>("analogagenda-functions")
    .PublishAsDockerFile()
    .WithEnvironment("AzureWebJobsScriptRoot", "/home/site/wwwroot")
    .WithEnvironment("FUNCTIONS_WORKER_RUNTIME", "dotnet-isolated");

// Add the frontend Angular app as a proper Aspire resource
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

builder.Build().Run();
