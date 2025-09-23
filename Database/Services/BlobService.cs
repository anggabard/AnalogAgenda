using Azure.Storage.Blobs;
using Configuration.Sections;
using Database.DBObjects.Enums;
using Database.Services.Interfaces;

namespace Database.Services;

public sealed class BlobService(AzureAd azureAdCfg, Storage storageCfg) : BaseAzureService<BlobContainerClient>(azureAdCfg, storageCfg, "blob"), IBlobService
{
    public BlobContainerClient GetBlobContainer(string containerName) => GetValidatedClient(containerName);

    public BlobContainerClient GetBlobContainer(ContainerName containerName) => 
        GetBlobContainer(containerName.ToString());

    protected override void ValidateResourceName(string resourceName)
    {
        var serviceClient = new BlobServiceClient(AccountUri, Credential);
        var containerClient = serviceClient.GetBlobContainerClient(resourceName);
        if (containerClient == null || !containerClient.Exists()) 
            throw new ArgumentException($"Error: '{resourceName}' is not a valid Container.");
    }

    protected override BlobContainerClient CreateClient(string resourceName)
    {
        var serviceClient = new BlobServiceClient(AccountUri, Credential);
        return serviceClient.GetBlobContainerClient(resourceName);
    }
}
