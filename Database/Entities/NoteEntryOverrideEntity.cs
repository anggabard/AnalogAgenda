using Database.DTOs;

namespace Database.Entities;

public class NoteEntryOverrideEntity : BaseEntity
{
    public string NoteEntryId { get; set; } = default!;
    public int FilmCountMin { get; set; }
    public int FilmCountMax { get; set; }
    public double? Time { get; set; }
    public string? Step { get; set; }
    public string? Details { get; set; }
    public double? TemperatureMin { get; set; }
    public double? TemperatureMax { get; set; }

    // Navigation property
    public NoteEntryEntity NoteEntry { get; set; } = default!;

    protected override int IdLength() => 8;
}
