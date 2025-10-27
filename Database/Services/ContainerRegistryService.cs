using Azure.Containers.ContainerRegistry;
using Configuration.Sections;
using Database.Helpers;
using Database.Services.Interfaces;
using System.Collections.Concurrent;

namespace Database.Services;

public sealed class ContainerRegistryService(AzureAd azureAdCfg, ContainerRegistry containerRegistryCfg) : IContainerRegistryService
{
    private readonly Lazy<ContainerRegistryClient> _client = new(() => new ContainerRegistryClient(
        new Uri($"https://{containerRegistryCfg.Name}.azurecr.io"),
        azureAdCfg.GetClientSecretCredential()));
    private readonly ConcurrentDictionary<string, ContainerRepository> _repositoryCache = new();

    public ContainerRepository GetRepository(string repositoryName)
    {
        if (string.IsNullOrWhiteSpace(repositoryName))
            throw new ArgumentException("Repository name cannot be null or empty.", nameof(repositoryName));

        return _repositoryCache.GetOrAdd(repositoryName, _client.Value.GetRepository);
    }

    public RegistryArtifact GetArtifact(string repositoryName, string digest)
    {
        if (string.IsNullOrWhiteSpace(repositoryName))
            throw new ArgumentException("Repository name cannot be null or empty.", nameof(repositoryName));
        if (string.IsNullOrWhiteSpace(digest))
            throw new ArgumentException("Digest cannot be null or empty.", nameof(digest));

        return _client.Value.GetArtifact(repositoryName, digest);
    }

    public async Task DeleteArtifactAsync(string repositoryName, string digest)
    {
        var artifact = GetArtifact(repositoryName, digest);
        await artifact.DeleteAsync();
    }
}

