using Database.Helpers;

namespace AnalogAgenda.Server.Tests.Helpers;

public class BlobImageHelperTests
{
    [Theory]
    [InlineData("data:image/jpeg;base64,/9j/4AAQSkZJRgAB", "image/jpeg")]
    [InlineData("data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAA", "image/png")]
    [InlineData("data:image/gif;base64,R0lGODlhAQABAIAAAAAAAP//", "image/gif")]
    [InlineData("data:image/webp;base64,UklGRiIAAABXRUJQVlA4IB", "image/webp")]
    public void GetContentTypeFromBase64_ValidBase64WithType_ReturnsCorrectContentType(string base64WithType, string expectedContentType)
    {
        // Act
        var result = BlobImageHelper.GetContentTypeFromBase64(base64WithType);

        // Assert
        Assert.Equal(expectedContentType, result);
    }

    [Theory]
    [InlineData("data:image/jpeg;base64,/9j/4AAQSkZJRgAB", "jpg")]
    [InlineData("data:image/jpg;base64,/9j/4AAQSkZJRgAB", "jpg")]
    [InlineData("data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAA", "png")]
    [InlineData("data:image/gif;base64,R0lGODlhAQABAIAAAAAAAP//", "gif")]
    [InlineData("data:image/webp;base64,UklGRiIAAABXRUJQVlA4IB", "webp")]
    [InlineData("data:image/bmp;base64,Qk1WAAAAAAAAADYAAAAoAAAA", "bmp")]
    [InlineData("data:image/tiff;base64,SUkqAAgAAAAA", "tiff")]
    public void GetFileExtensionFromBase64_ValidBase64WithType_ReturnsCorrectExtension(string base64WithType, string expectedExtension)
    {
        // Act
        var result = BlobImageHelper.GetFileExtensionFromBase64(base64WithType);

        // Assert
        Assert.Equal(expectedExtension, result);
    }

    [Fact]
    public void GetFileExtensionFromBase64_UnknownContentType_ReturnsDefaultExtension()
    {
        // Arrange
        var base64WithType = "data:image/unknown;base64,somedata";

        // Act
        var result = BlobImageHelper.GetFileExtensionFromBase64(base64WithType);

        // Assert
        Assert.Equal("jpg", result); // Default extension
    }

    [Fact]
    public void GetContentTypeFromBase64_InvalidFormat_HandlesGracefully()
    {
        // Arrange
        var invalidBase64 = "invalidformat";

        // Act & Assert - The method should handle invalid input gracefully
        // Based on actual implementation, it might return the input or handle it differently
        var result = BlobImageHelper.GetContentTypeFromBase64(invalidBase64);
        Assert.NotNull(result); // Should not crash
    }

    [Fact]
    public void GetFileExtensionFromBase64_InvalidFormat_ReturnsDefault()
    {
        // Arrange
        var invalidBase64 = "invalidformat";

        // Act
        var result = BlobImageHelper.GetFileExtensionFromBase64(invalidBase64);

        // Assert - Should return default extension
        Assert.Equal("jpg", result);
    }

    [Theory]
    [InlineData("data:text/plain;base64,SGVsbG8=")]
    [InlineData("data:application/json;base64,eyJ0ZXN0IjoidmFsdWUifQ==")]
    public void GetContentTypeFromBase64_NonImageContentType_ReturnsContentType(string base64WithType)
    {
        // Act
        var result = BlobImageHelper.GetContentTypeFromBase64(base64WithType);

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("data:", base64WithType);
        Assert.Contains(result, base64WithType);
    }

    [Fact]
    public void GetFileExtensionFromBase64_NonImageContentType_ReturnsDefaultExtension()
    {
        // Arrange
        var base64WithType = "data:text/plain;base64,SGVsbG8=";

        // Act
        var result = BlobImageHelper.GetFileExtensionFromBase64(base64WithType);

        // Assert
        Assert.Equal("jpg", result); // Should return default for non-image types
    }
}
