namespace Configuration.Sections;

public class Storage 
{
    public required string AccountName { get; set; }
    
    // Connection string parts for development (optional)
    public string? DefaultEndpointsProtocol { get; set; }
    public string? AccountKey { get; set; }
    public string? BlobEndpoint { get; set; }
    public string? EndpointSuffix { get; set; }
}
