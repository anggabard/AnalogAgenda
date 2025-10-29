var builder = DistributedApplication.CreateBuilder(args);

// Add the backend API
var backend = builder.AddProject<Projects.AnalogAgenda_Server>("analogagenda-server")
    .PublishAsDockerFile()
    .WithHttpEndpoint(name: "backend-http");

// Add the frontend Angular app
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
