using Azure.Containers.ContainerRegistry;

namespace Database.Services.Interfaces;

public interface IContainerRegistryService
{
    ContainerRepository GetRepository(string repositoryName);
    RegistryArtifact GetArtifact(string repositoryName, string digest);
    Task DeleteArtifactAsync(string repositoryName, string digest);
}

