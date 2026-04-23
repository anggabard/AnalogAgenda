using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace AnalogAgenda.Server.DataProtection;

internal interface IBlobXmlRepositoryStorage
{
    /// <summary>Null when the blob does not exist (HTTP 404).</summary>
    BlobDataSnapshot? TryDownload();

    /// <exception cref="RequestFailedException">HTTP 409 or 412 on optimistic concurrency conflict.</exception>
    ETag Upload(Stream content, BlobUploadOptions options);
}

internal readonly record struct BlobDataSnapshot(byte[] Contents, ETag ETag);

internal sealed class BlobClientXmlRepositoryStorage : IBlobXmlRepositoryStorage
{
    private readonly BlobClient _blobClient;

    public BlobClientXmlRepositoryStorage(BlobClient blobClient) =>
        _blobClient = blobClient ?? throw new ArgumentNullException(nameof(blobClient));

    public BlobDataSnapshot? TryDownload()
    {
        try
        {
            var downloadResult = _blobClient.DownloadContent().Value;
            return new BlobDataSnapshot(
                downloadResult.Content.ToMemory().ToArray(),
                downloadResult.Details.ETag
            );
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public ETag Upload(Stream content, BlobUploadOptions options)
    {
        var uploadResponse = _blobClient.Upload(content, options);
        return uploadResponse.Value.ETag;
    }
}
