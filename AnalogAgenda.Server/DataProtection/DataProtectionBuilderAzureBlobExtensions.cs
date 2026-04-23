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
    /// <param name="createContainerIfNotExists">
    /// When true, calls <see cref="BlobContainerClient.CreateIfNotExists"/> during registration (synchronous network I/O and requires create permission on the account).
    /// Leave false in production when the container is provisioned ahead of time with least-privilege identities.
    /// </param>
    public static IDataProtectionBuilder PersistKeysToAzureBlobStorage(
        this IDataProtectionBuilder builder,
        string connectionString,
        string containerName,
        string blobName,
        bool createContainerIfNotExists = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(connectionString);
        ArgumentException.ThrowIfNullOrEmpty(containerName);
        ArgumentException.ThrowIfNullOrEmpty(blobName);

        var serviceClient = new BlobServiceClient(connectionString);
        var container = serviceClient.GetBlobContainerClient(containerName);
        if (createContainerIfNotExists)
        {
            container.CreateIfNotExists(PublicAccessType.None);
        }

        var blobClient = container.GetBlobClient(blobName);

        builder.Services.Configure<KeyManagementOptions>(options =>
        {
            options.XmlRepository = new AzureBlobXmlRepository(blobClient);
        });

        return builder;
    }
}
