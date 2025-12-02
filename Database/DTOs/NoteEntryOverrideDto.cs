namespace Database.DTOs;

public class NoteEntryOverrideDto
{
    public string Id { get; set; } = string.Empty;
    public required string NoteEntryId { get; set; }
    public int FilmCountMin { get; set; }
    public int FilmCountMax { get; set; }
    public double? Time { get; set; }
    public string? Step { get; set; }
    public string? Details { get; set; }
    public double? TemperatureMin { get; set; }
    public double? TemperatureMax { get; set; }
}
