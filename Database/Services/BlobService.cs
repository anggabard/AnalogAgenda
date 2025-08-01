using Azure.Storage.Blobs;
using Configuration.Sections;
using Database.DBObjects.Enums;
using Database.Helpers;
using Database.Services.Interfaces;
using System.Collections.Concurrent;

namespace Database.Services;

public sealed class BlobService(AzureAd azureAdCfg, Storage storageCfg) : IBlobService
{
    private readonly Uri _accountUri = new($"https://{storageCfg.AccountName}.blob.core.windows.net");
    private readonly ConcurrentDictionary<string, BlobContainerClient> _cache = new();

    public BlobContainerClient GetBlobContainer(string containerName)
        => _cache.GetOrAdd(containerName, name =>
        {
            var serviceClient = new BlobServiceClient(_accountUri, azureAdCfg.GetClientSecretCredential());
            var containerClient = serviceClient.GetBlobContainerClient(name);
            if (containerClient == null || !containerClient.Exists()) throw new ArgumentException($"Error: '{name}' is not a valid Container.");

            return containerClient;
        });

    public BlobContainerClient GetBlobContainer(ContainerName containerName)
    {
        return GetBlobContainer(containerName.ToString());
    }
}
