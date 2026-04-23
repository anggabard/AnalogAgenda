using System.Collections.ObjectModel;
using System.Runtime.ExceptionServices;
using System.Xml;
using System.Xml.Linq;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.DataProtection.Repositories;

namespace AnalogAgenda.Server.DataProtection;

/// <summary>
/// <see cref="IXmlRepository"/> backed by a single Azure blob (same semantics as the former Azure.Extensions package).
/// </summary>
internal sealed class AzureBlobXmlRepository : IXmlRepository
{
    private const int ConflictMaxRetries = 5;
    private static readonly TimeSpan ConflictBackoffPeriod = TimeSpan.FromMilliseconds(200);
    private static readonly XName RepositoryElementName = "repository";
    private static readonly BlobHttpHeaders BlobHttpHeaders = new() { ContentType = "application/xml; charset=utf-8" };

    private readonly Random _random = new();
    private BlobData? _cachedBlobData;
    private readonly BlobClient _blobClient;

    public AzureBlobXmlRepository(BlobClient blobClient) =>
        _blobClient = blobClient ?? throw new ArgumentNullException(nameof(blobClient));

    public IReadOnlyCollection<XElement> GetAllElements()
    {
        var data = GetLatestData();
        var doc = CreateDocumentFromBlobData(data);
        return new ReadOnlyCollection<XElement>([.. doc.Root!.Elements()]);
    }

    public void StoreElement(XElement element, string? friendlyName)
    {
        ArgumentNullException.ThrowIfNull(element);

        ExceptionDispatchInfo? lastError = null;

        for (var i = 0; i < ConflictMaxRetries; i++)
        {
            if (i > 1)
                Thread.Sleep(GetRandomizedBackoffPeriod());

            if (i > 0)
                GetLatestData();

            var latestData = Volatile.Read(ref _cachedBlobData);
            var doc = CreateDocumentFromBlobData(latestData);
            doc.Root!.Add(element);

            using var serializedDoc = new MemoryStream();
            doc.Save(serializedDoc, SaveOptions.DisableFormatting);
            serializedDoc.Position = 0;

            BlobRequestConditions requestConditions = latestData != null
                ? new BlobRequestConditions { IfMatch = latestData.ETag }
                : new BlobRequestConditions { IfNoneMatch = ETag.All };

            try
            {
                var uploadResponse = _blobClient.Upload(
                    serializedDoc,
                    new BlobUploadOptions
                    {
                        HttpHeaders = BlobHttpHeaders,
                        Conditions = requestConditions,
                    });

                Volatile.Write(
                    ref _cachedBlobData,
                    new BlobData
                    {
                        BlobContents = serializedDoc.ToArray(),
                        ETag = uploadResponse.Value.ETag,
                    });
                return;
            }
            catch (RequestFailedException ex) when (ex.Status is 409 or 412)
            {
                lastError = ExceptionDispatchInfo.Capture(ex);
            }
        }

        lastError?.Throw();
    }

    private static XDocument CreateDocumentFromBlobData(BlobData? blobData)
    {
        if (blobData is null || blobData.BlobContents.Length == 0)
            return new XDocument(new XElement(RepositoryElementName));

        using var memoryStream = new MemoryStream(blobData.BlobContents);
        var xmlReaderSettings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            IgnoreProcessingInstructions = true,
        };
        using var xmlReader = XmlReader.Create(memoryStream, xmlReaderSettings);
        return XDocument.Load(xmlReader);
    }

    private BlobData? GetLatestData()
    {
        try
        {
            var downloadResult = _blobClient.DownloadContent().Value;
            var latestCachedData = new BlobData
            {
                BlobContents = downloadResult.Content.ToMemory().ToArray(),
                ETag = downloadResult.Details.ETag,
            };
            Volatile.Write(ref _cachedBlobData, latestCachedData);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            Volatile.Write(ref _cachedBlobData, null);
        }

        return Volatile.Read(ref _cachedBlobData);
    }

    private int GetRandomizedBackoffPeriod()
    {
        var multiplier = 0.8 + (_random.NextDouble() * 0.2);
        return (int)(multiplier * ConflictBackoffPeriod.TotalMilliseconds);
    }

    private sealed class BlobData
    {
        public required byte[] BlobContents;
        public required ETag? ETag;
    }
}
