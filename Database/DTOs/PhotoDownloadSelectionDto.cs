namespace Database.DTOs;

public class PhotoDownloadSelectionDto
{
    public string FilmId { get; set; } = string.Empty;

    public List<string> Ids { get; set; } = [];

    public bool Small { get; set; }
}
