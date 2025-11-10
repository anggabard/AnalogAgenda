using AnalogAgenda.Server.Services.Implementations;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AnalogAgenda.Server.Tests.Services;

public class InMemoryImageCacheServiceTests
{
    private readonly Mock<ILogger<InMemoryImageCacheService>> _mockLogger;
    private readonly InMemoryImageCacheService _cacheService;

    public InMemoryImageCacheServiceTests()
    {
        _mockLogger = new Mock<ILogger<InMemoryImageCacheService>>();
        _cacheService = new InMemoryImageCacheService(_mockLogger.Object);
    }

    [Fact]
    public void TryGetPreview_NonExistentImage_ReturnsFalse()
    {
        // Arrange
        var imageId = Guid.NewGuid();

        // Act
        var result = _cacheService.TryGetPreview(imageId, out var cachedImage);

        // Assert
        Assert.False(result);
        Assert.Null(cachedImage);
    }

    [Fact]
    public void SetPreview_ValidImage_StoresSuccessfully()
    {
        // Arrange
        var imageId = Guid.NewGuid();
        var imageBytes = new byte[] { 1, 2, 3, 4, 5 };
        var contentType = "image/jpeg";

        // Act
        _cacheService.SetPreview(imageId, imageBytes, contentType);
        var result = _cacheService.TryGetPreview(imageId, out var cachedImage);

        // Assert
        Assert.True(result);
        Assert.NotNull(cachedImage);
        Assert.Equal(imageBytes, cachedImage.Value.imageBytes);
        Assert.Equal(contentType, cachedImage.Value.contentType);
    }

    [Fact]
    public void TryGetPreview_ExistingImage_ReturnsCorrectData()
    {
        // Arrange
        var imageId = Guid.NewGuid();
        var imageBytes = new byte[] { 10, 20, 30, 40 };
        var contentType = "image/png";
        _cacheService.SetPreview(imageId, imageBytes, contentType);

        // Act
        var result = _cacheService.TryGetPreview(imageId, out var cachedImage);

        // Assert
        Assert.True(result);
        Assert.NotNull(cachedImage);
        Assert.Equal(imageBytes, cachedImage.Value.imageBytes);
        Assert.Equal(contentType, cachedImage.Value.contentType);
    }

    [Fact]
    public void SetPreview_OverwriteExistingImage_UpdatesSuccessfully()
    {
        // Arrange
        var imageId = Guid.NewGuid();
        var originalBytes = new byte[] { 1, 2, 3 };
        var updatedBytes = new byte[] { 4, 5, 6, 7, 8 };
        var originalContentType = "image/jpeg";
        var updatedContentType = "image/png";

        // Act
        _cacheService.SetPreview(imageId, originalBytes, originalContentType);
        _cacheService.SetPreview(imageId, updatedBytes, updatedContentType);
        var result = _cacheService.TryGetPreview(imageId, out var cachedImage);

        // Assert
        Assert.True(result);
        Assert.NotNull(cachedImage);
        Assert.Equal(updatedBytes, cachedImage.Value.imageBytes);
        Assert.Equal(updatedContentType, cachedImage.Value.contentType);
    }

    [Fact]
    public void RemovePreview_ExistingImage_RemovesSuccessfully()
    {
        // Arrange
        var imageId = Guid.NewGuid();
        var imageBytes = new byte[] { 1, 2, 3 };
        var contentType = "image/jpeg";
        _cacheService.SetPreview(imageId, imageBytes, contentType);

        // Act
        _cacheService.RemovePreview(imageId);
        var result = _cacheService.TryGetPreview(imageId, out var cachedImage);

        // Assert
        Assert.False(result);
        Assert.Null(cachedImage);
    }

    [Fact]
    public void RemovePreview_NonExistentImage_DoesNotThrow()
    {
        // Arrange
        var imageId = Guid.NewGuid();

        // Act & Assert
        var exception = Record.Exception(() => _cacheService.RemovePreview(imageId));
        Assert.Null(exception);
    }

    [Fact]
    public void ClearAll_MultipleCachedImages_RemovesAll()
    {
        // Arrange
        var imageId1 = Guid.NewGuid();
        var imageId2 = Guid.NewGuid();
        var imageId3 = Guid.NewGuid();
        var imageBytes = new byte[] { 1, 2, 3 };
        var contentType = "image/jpeg";

        _cacheService.SetPreview(imageId1, imageBytes, contentType);
        _cacheService.SetPreview(imageId2, imageBytes, contentType);
        _cacheService.SetPreview(imageId3, imageBytes, contentType);

        // Act
        _cacheService.ClearAll();

        // Assert
        Assert.False(_cacheService.TryGetPreview(imageId1, out _));
        Assert.False(_cacheService.TryGetPreview(imageId2, out _));
        Assert.False(_cacheService.TryGetPreview(imageId3, out _));
    }

    [Fact]
    public void SetPreview_EmptyByteArray_StoresSuccessfully()
    {
        // Arrange
        var imageId = Guid.NewGuid();
        var imageBytes = Array.Empty<byte>();
        var contentType = "image/jpeg";

        // Act
        _cacheService.SetPreview(imageId, imageBytes, contentType);
        var result = _cacheService.TryGetPreview(imageId, out var cachedImage);

        // Assert
        Assert.True(result);
        Assert.NotNull(cachedImage);
        Assert.Empty(cachedImage.Value.imageBytes);
        Assert.Equal(contentType, cachedImage.Value.contentType);
    }

    [Fact]
    public void SetPreview_DifferentContentTypes_StoresCorrectly()
    {
        // Arrange
        var imageId1 = Guid.NewGuid();
        var imageId2 = Guid.NewGuid();
        var imageId3 = Guid.NewGuid();
        var imageBytes = new byte[] { 1, 2, 3 };

        // Act
        _cacheService.SetPreview(imageId1, imageBytes, "image/jpeg");
        _cacheService.SetPreview(imageId2, imageBytes, "image/png");
        _cacheService.SetPreview(imageId3, imageBytes, "image/webp");

        // Assert
        _cacheService.TryGetPreview(imageId1, out var cached1);
        _cacheService.TryGetPreview(imageId2, out var cached2);
        _cacheService.TryGetPreview(imageId3, out var cached3);

        Assert.Equal("image/jpeg", cached1!.Value.contentType);
        Assert.Equal("image/png", cached2!.Value.contentType);
        Assert.Equal("image/webp", cached3!.Value.contentType);
    }
}

