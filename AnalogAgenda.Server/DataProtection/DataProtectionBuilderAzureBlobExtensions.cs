using AnalogAgenda.Server.DataProtection;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.DataProtection;

/// <summary>
/// Registers an XML key repository backed by Azure Blob Storage (same behavior as the Azure.Extensions package without its legacy DataProtection dependency).
/// </summary>
public static class DataProtectionBuilderAzureBlobExtensions
{
    public static IDataProtectionBuilder PersistKeysToAzureBlobStorage(
        this IDataProtectionBuilder builder,
        string connectionString,
        string containerName,
        string blobName)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(connectionString);
        ArgumentException.ThrowIfNullOrEmpty(containerName);
        ArgumentException.ThrowIfNullOrEmpty(blobName);

        var serviceClient = new BlobServiceClient(connectionString);
        var container = serviceClient.GetBlobContainerClient(containerName);
        container.CreateIfNotExists(PublicAccessType.None);
        var blobClient = container.GetBlobClient(blobName);

        builder.Services.Configure<KeyManagementOptions>(options =>
        {
            options.XmlRepository = new AzureBlobXmlRepository(blobClient);
        });

        return builder;
    }
}
