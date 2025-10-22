var builder = DistributedApplication.CreateBuilder(args);

// Add the backend API
var backend = builder.AddProject<Projects.AnalogAgenda_Server>("analogagenda-server");

// Add the frontend Angular app as a proper Aspire resource
var frontend = builder.AddNpmApp("analogagenda-client", "../analogagenda.client")
    .WithReference(backend)
    .WaitFor(backend)
    .WithHttpEndpoint(name: "http");

frontend.WithEnvironment((context) =>
{
    var endpoint = frontend.GetEndpoint("http");
    var targetPort = endpoint.Property(EndpointProperty.TargetPort);
    context.EnvironmentVariables.Add("PORT", ReferenceExpression.Create($"{targetPort}"));
});

frontend.WithExternalHttpEndpoints();

builder.Build().Run();
