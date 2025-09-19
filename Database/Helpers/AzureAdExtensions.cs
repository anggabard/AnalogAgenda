using Azure.Identity;
using Configuration.Sections;

namespace Database.Helpers;

public static class AzureAdExtensions
{
    public static ClientSecretCredential GetClientSecretCredential(this AzureAd azureAd)
    {
        if (string.IsNullOrEmpty(azureAd.TenantId))
            throw new ArgumentException("TenantId cannot be null or empty.", nameof(azureAd));
        if (string.IsNullOrEmpty(azureAd.ClientId))
            throw new ArgumentException("ClientId cannot be null or empty.", nameof(azureAd));
        if (string.IsNullOrEmpty(azureAd.ClientSecret))
            throw new ArgumentException("ClientSecret cannot be null or empty.", nameof(azureAd));

        return new ClientSecretCredential(
                azureAd.TenantId,
                azureAd.ClientId,
                azureAd.ClientSecret);
    }
}
