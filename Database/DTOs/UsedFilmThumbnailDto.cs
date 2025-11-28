namespace Database.DTOs;

public class UsedFilmThumbnailDto
{
    public string Id { get; set; } = string.Empty;

    public required string FilmName { get; set; }

    public string ImageId { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;

    public string ImageBase64 { get; set; } = string.Empty;
}

