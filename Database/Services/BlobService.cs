using Azure.Storage.Blobs;
using Configuration.Sections;
using Database.DBObjects.Enums;
using Database.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Database.Services;

public sealed class BlobService(AzureAd azureAdCfg, Storage storageCfg, IConfiguration configuration) : BaseAzureService<BlobContainerClient>(azureAdCfg, storageCfg, configuration, "blob"), IBlobService
{
    public BlobContainerClient GetBlobContainer(string containerName) => GetValidatedClient(containerName);

    public BlobContainerClient GetBlobContainer(ContainerName containerName) =>
        GetBlobContainer(containerName.ToString());

    protected override void ValidateResourceName(string resourceName)
    {
        if (string.IsNullOrWhiteSpace(resourceName))
            throw new ArgumentException($"Error: Container name cannot be null or empty.");

        // Basic container name validation according to Azure blob naming rules
        if (resourceName.Length < 3 || resourceName.Length > 63)
            throw new ArgumentException($"Error: Container name '{resourceName}' must be between 3 and 63 characters.");

        if (!resourceName.All(c => char.IsLetterOrDigit(c) || c == '-'))
            throw new ArgumentException($"Error: Container name '{resourceName}' can only contain lowercase letters, numbers, and hyphens.");

        if (resourceName.StartsWith('-') || resourceName.EndsWith('-'))
            throw new ArgumentException($"Error: Container name '{resourceName}' cannot start or end with a hyphen.");
    }

    protected override BlobContainerClient CreateClient(string resourceName)
    {
        BlobServiceClient serviceClient;
        
        // Use connection string if available (dev mode with Azurite), otherwise use Azure AD (prod mode)
        if (!string.IsNullOrEmpty(ConnectionString))
        {
            serviceClient = new BlobServiceClient(ConnectionString);
        }
        else if (Credential != null)
        {
            serviceClient = new BlobServiceClient(AccountUri, Credential);
        }
        else
        {
            throw new InvalidOperationException("Either ConnectionString or Credential must be provided for blob storage authentication.");
        }
        
        return serviceClient.GetBlobContainerClient(resourceName);
    }
}
