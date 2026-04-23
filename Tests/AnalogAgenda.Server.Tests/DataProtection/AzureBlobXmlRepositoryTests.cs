using System.Linq;
using System.Xml.Linq;
using AnalogAgenda.Server.DataProtection;
using Azure;
using Azure.Storage.Blobs.Models;

namespace AnalogAgenda.Server.Tests.DataProtection;

public sealed class AzureBlobXmlRepositoryTests
{
    [Fact]
    public void GetAllElements_EmptyStore_ReturnsEmptySequence()
    {
        var fake = new FakeBlobXmlRepositoryStorage();
        var repo = new AzureBlobXmlRepository(fake);

        var elements = repo.GetAllElements();

        Assert.Empty(elements);
    }

    [Fact]
    public void GetAllElements_WithBlob_ReturnsRootChildren()
    {
        var doc = new XDocument(new XElement("repository", new XElement("key", new XAttribute("id", "a"))));
        var fake = new FakeBlobXmlRepositoryStorage();
        fake.SetExistingRepositoryXml(doc);
        var repo = new AzureBlobXmlRepository(fake);

        var elements = repo.GetAllElements().ToList();

        Assert.Single(elements);
        Assert.Equal("key", elements[0].Name.LocalName);
    }

    [Fact]
    public void StoreElement_EmptyStore_UsesIfNoneMatchAll_OnFirstUpload()
    {
        var fake = new FakeBlobXmlRepositoryStorage();
        var repo = new AzureBlobXmlRepository(fake);
        var el = new XElement("key", new XAttribute("id", "k1"));

        repo.StoreElement(el, friendlyName: null);

        Assert.Equal(1, fake.UploadCallCount);
        Assert.True(fake.LastUploadUsedIfNoneMatchAll);
        Assert.NotNull(fake.CurrentBlobXml);
        Assert.Contains("k1", fake.CurrentBlobXml!, StringComparison.Ordinal);
    }

    [Fact]
    public void StoreElement_WhenBlobExists_PrimesEtag_SingleUploadUsesIfMatch()
    {
        var seed = new XDocument(new XElement("repository", new XElement("existing")));
        var fake = new FakeBlobXmlRepositoryStorage();
        fake.SetExistingRepositoryXml(seed);
        var repo = new AzureBlobXmlRepository(fake);

        repo.StoreElement(new XElement("key", new XAttribute("id", "new")), friendlyName: null);

        Assert.Equal(1, fake.UploadCallCount);
        Assert.False(fake.LastUploadUsedIfNoneMatchAll);
        Assert.Contains("existing", fake.CurrentBlobXml!, StringComparison.Ordinal);
        Assert.Contains("new", fake.CurrentBlobXml!, StringComparison.Ordinal);
    }

    [Fact]
    public void StoreElement_On412_RetriesAndEventuallySucceeds()
    {
        var seed = new XDocument(new XElement("repository"));
        var fake = new FakeBlobXmlRepositoryStorage();
        fake.SetExistingRepositoryXml(seed);
        fake.Throw412OnNextUpload = true;
        var repo = new AzureBlobXmlRepository(fake);

        repo.StoreElement(new XElement("key", new XAttribute("id", "after-retry")), friendlyName: null);

        Assert.True(fake.UploadCallCount >= 2, $"expected retries, got {fake.UploadCallCount}");
        Assert.Contains("after-retry", fake.CurrentBlobXml!, StringComparison.Ordinal);
    }

    /// <summary>In-memory blob behavior matching optimistic concurrency rules used by <see cref="AzureBlobXmlRepository"/>.</summary>
    private sealed class FakeBlobXmlRepositoryStorage : IBlobXmlRepositoryStorage
    {
        private byte[]? _blob;
        private ETag _etag = new ETag("\"0\"");

        public int UploadCallCount { get; private set; }
        public bool LastUploadUsedIfNoneMatchAll { get; private set; }
        public string? CurrentBlobXml => _blob is null ? null : System.Text.Encoding.UTF8.GetString(_blob);
        public bool Throw412OnNextUpload { get; set; }

        public void SetExistingRepositoryXml(XDocument doc)
        {
            using var ms = new MemoryStream();
            doc.Save(ms, SaveOptions.DisableFormatting);
            _blob = ms.ToArray();
            _etag = new ETag("\"seed\"");
        }

        public BlobDataSnapshot? TryDownload()
        {
            if (_blob is null)
                return null;
            return new BlobDataSnapshot((byte[])_blob.Clone(), _etag);
        }

        public ETag Upload(Stream content, BlobUploadOptions options)
        {
            UploadCallCount++;

            if (Throw412OnNextUpload)
            {
                Throw412OnNextUpload = false;
                throw new RequestFailedException(412, "simulated conflict", "ConditionNotMet", innerException: null);
            }

            var bytes = ReadFully(content);
            var conditions = options.Conditions ?? new BlobRequestConditions();
            LastUploadUsedIfNoneMatchAll = conditions.IfNoneMatch == ETag.All;

            if (_blob is null)
            {
                if (conditions.IfNoneMatch != ETag.All)
                    throw new InvalidOperationException("Test fake: expected create via If-None-Match: *.");
            }
            else
            {
                if (conditions.IfNoneMatch == ETag.All)
                    throw new RequestFailedException(409, "blob exists", "BlobAlreadyExists", innerException: null);

                if (!conditions.IfMatch.HasValue || conditions.IfMatch.Value != _etag)
                    throw new RequestFailedException(412, "etag mismatch", "ConditionNotMet", innerException: null);
            }

            _blob = bytes;
            _etag = new ETag($"\"u{UploadCallCount}\"");
            return _etag;
        }

        private static byte[] ReadFully(Stream s)
        {
            if (s is MemoryStream ms)
            {
                return ms.ToArray();
            }

            using var copy = new MemoryStream();
            s.CopyTo(copy);
            return copy.ToArray();
        }
    }
}
