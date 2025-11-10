namespace Database.DTOs;

public class PhotoCreateDto
{
    public required string FilmId { get; set; }
    public required string ImageBase64 { get; set; }
    public int? Index { get; set; } // Optional index (0-999). If not provided, next available index is used
}
