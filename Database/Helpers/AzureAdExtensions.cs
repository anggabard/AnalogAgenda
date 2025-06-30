using Azure.Identity;
using Configuration.Sections;

namespace Database.Helpers;

public static class AzureAdExtensions
{
    public static ClientSecretCredential GetClientSecretCredential(this AzureAd azureAd)
    {
        return new ClientSecretCredential(
                azureAd.TenantId,
                azureAd.ClientId,
                azureAd.ClientSecret);

    }
}
