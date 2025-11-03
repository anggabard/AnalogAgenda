using Database.Helpers;

namespace AnalogAgenda.Server.Tests.Database;

public class BlobUrlHelperTests
{
    [Fact]
    public void GetUrlFromImageImageInfo_WithValidInputs_ReturnsCorrectUrl()
    {
        // Arrange
        var accountName = "teststorage";
        var containerName = "images";
        var imageId = Guid.NewGuid();

        // Act
        var result = BlobUrlHelper.GetUrlFromImageImageInfo(accountName, containerName, imageId);

        // Assert
        var expected = $"https://{accountName}.blob.core.windows.net/{containerName}/{imageId}";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetImageInfoFromUrl_WithValidUrl_ReturnsCorrectInfo()
    {
        // Arrange
        var accountName = "teststorage";
        var containerName = "images";
        var imageId = Guid.NewGuid();
        var url = $"https://{accountName}.blob.core.windows.net/{containerName}/{imageId}";

        // Act
        var result = BlobUrlHelper.GetImageInfoFromUrl(url);

        // Assert
        Assert.Equal(accountName, result.AccountName);
        Assert.Equal(containerName, result.ContainerName);
        Assert.Equal(imageId, result.ImageId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void GetImageInfoFromUrl_WithInvalidUrl_ThrowsArgumentException(string url)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => BlobUrlHelper.GetImageInfoFromUrl(url));
    }

    [Fact]
    public void GetImageInfoFromUrl_WithInvalidUriFormat_ThrowsUriFormatException()
    {
        // Arrange
        var invalidUrl = "not-a-valid-url";

        // Act & Assert
        Assert.Throws<UriFormatException>(() => BlobUrlHelper.GetImageInfoFromUrl(invalidUrl));
    }

    [Fact]
    public void GetImageInfoFromUrl_WithInvalidSegmentCount_ThrowsArgumentException()
    {
        // Arrange
        var urlWithOneSegment = "https://teststorage.blob.core.windows.net/images";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => BlobUrlHelper.GetImageInfoFromUrl(urlWithOneSegment));
        Assert.Contains("Expected container and image ID", exception.Message);
    }

    [Fact]
    public void GetImageInfoFromUrl_WithInvalidGuid_ThrowsArgumentException()
    {
        // Arrange
        var urlWithInvalidGuid = "https://teststorage.blob.core.windows.net/images/not-a-guid";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => BlobUrlHelper.GetImageInfoFromUrl(urlWithInvalidGuid));
        Assert.Contains("Invalid image ID format", exception.Message);
    }

}

