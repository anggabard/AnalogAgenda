using Azure.Containers.ContainerRegistry;
using Database.Services.Interfaces;

namespace AnalogAgenda.Functions.Services;

/// <summary>
/// No-op implementation when Container Registry is not configured (e.g. Docker deploy on laptop).
/// </summary>
public sealed class NoOpContainerRegistryService : IContainerRegistryService
{
    public ContainerRepository GetRepository(string repositoryName) =>
        throw new InvalidOperationException("Container registry is not configured. Set ContainerRegistry in configuration for Azure deployment.");

    public RegistryArtifact GetArtifact(string repositoryName, string digest) =>
        throw new InvalidOperationException("Container registry is not configured. Set ContainerRegistry in configuration for Azure deployment.");

    public Task DeleteArtifactAsync(string repositoryName, string digest) =>
        throw new InvalidOperationException("Container registry is not configured. Set ContainerRegistry in configuration for Azure deployment.");
}
