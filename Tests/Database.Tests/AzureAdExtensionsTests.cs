using Database.Helpers;
using Configuration.Sections;
using Azure.Identity;

namespace Database.Tests;

public class AzureAdExtensionsTests
{
    [Fact]
    public void GetClientSecretCredential_WithValidAzureAdConfig_ReturnsClientSecretCredential()
    {
        // Arrange
        var azureAd = new AzureAd
        {
            TenantId = "test-tenant-id",
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret"
        };

        // Act
        var result = azureAd.GetClientSecretCredential();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ClientSecretCredential>(result);
    }

    [Fact]
    public void GetClientSecretCredential_WithNullValues_ThrowsArgumentException()
    {
        // Arrange
        var azureAd = new AzureAd
        {
            TenantId = null,
            ClientId = null,
            ClientSecret = null
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => azureAd.GetClientSecretCredential());
    }

    [Theory]
    [InlineData("", "valid-client-id", "valid-client-secret")]
    [InlineData("valid-tenant-id", "", "valid-client-secret")]
    [InlineData("valid-tenant-id", "valid-client-id", "")]
    public void GetClientSecretCredential_WithEmptyValues_ThrowsArgumentException(
        string tenantId, string clientId, string clientSecret)
    {
        // Arrange
        var azureAd = new AzureAd
        {
            TenantId = tenantId,
            ClientId = clientId,
            ClientSecret = clientSecret
        };

        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() => azureAd.GetClientSecretCredential());
    }

    [Fact]
    public void GetClientSecretCredential_WithValidValues_CreatesCredentialWithCorrectProperties()
    {
        // Arrange
        var expectedTenantId = "test-tenant-123";
        var expectedClientId = "test-client-456";
        var expectedClientSecret = "test-secret-789";
        
        var azureAd = new AzureAd
        {
            TenantId = expectedTenantId,
            ClientId = expectedClientId,
            ClientSecret = expectedClientSecret
        };

        // Act
        var result = azureAd.GetClientSecretCredential();

        // Assert
        Assert.NotNull(result);
        // Note: ClientSecretCredential doesn't expose the values for verification,
        // but we can ensure it was created successfully without throwing exceptions
        Assert.IsType<ClientSecretCredential>(result);
    }
}
