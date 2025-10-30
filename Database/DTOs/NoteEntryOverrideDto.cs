using Database.Entities;

namespace Database.DTOs;

public class NoteEntryOverrideDto
{
    public string RowKey { get; set; } = string.Empty;
    public required string NoteEntryRowKey { get; set; }
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
            RowKey = RowKey,
            NoteEntryRowKey = NoteEntryRowKey,
            FilmCountMin = FilmCountMin,
            FilmCountMax = FilmCountMax,
            Time = Time,
            Step = Step,
            Details = Details,
            TemperatureMin = TemperatureMin,
            TemperatureMax = TemperatureMax,
        };
    }

    public NoteEntryOverrideEntity ToEntity(string noteEntryRowKey)
    {
        return new NoteEntryOverrideEntity
        {
            RowKey = RowKey,
            NoteEntryRowKey = noteEntryRowKey,
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
