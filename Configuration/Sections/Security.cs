namespace Configuration.Sections;

public class Security 
{
    public required string Salt { get; set; }
    public string BackendApiUrl { get; set; } = string.Empty;
}

