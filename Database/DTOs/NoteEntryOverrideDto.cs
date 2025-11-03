using Database.Entities;

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

    public NoteEntryOverrideEntity ToEntity()
    {
        return new NoteEntryOverrideEntity
        {
            Id = Id,
            NoteEntryId = NoteEntryId,
            FilmCountMin = FilmCountMin,
            FilmCountMax = FilmCountMax,
            Time = Time,
            Step = Step,
            Details = Details,
            TemperatureMin = TemperatureMin,
            TemperatureMax = TemperatureMax,
        };
    }

    public NoteEntryOverrideEntity ToEntity(string noteEntryId)
    {
        return new NoteEntryOverrideEntity
        {
            Id = Id,
            NoteEntryId = noteEntryId,
            FilmCountMin = FilmCountMin,
            FilmCountMax = FilmCountMax,
            Time = Time,
            Step = Step,
            Details = Details,
            TemperatureMin = TemperatureMin,
            TemperatureMax = TemperatureMax,
        };
    }
}
