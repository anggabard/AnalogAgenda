namespace Database.DTOs;

public class CollectionDownloadSelectedDto
{
    public List<string> Ids { get; set; } = [];

    public bool Small { get; set; }
}

public class CollectionSetFeaturedDto
{
    public string PhotoId { get; set; } = string.Empty;
}
