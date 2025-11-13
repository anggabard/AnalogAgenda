namespace Database.DTOs;

public class ExposureDateDto
{
    public string Id { get; set; } = string.Empty;
    public string FilmId { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public string Description { get; set; } = string.Empty;
}

