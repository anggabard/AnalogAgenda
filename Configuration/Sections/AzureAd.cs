namespace Configuration.Sections;

public class AzureAd
{
    public required string ClientId { get; set; }
    public required string TenantId { get; set; }
    public required string ClientSecret { get; set; }
}
