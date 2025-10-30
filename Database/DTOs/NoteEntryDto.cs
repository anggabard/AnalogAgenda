using Database.Entities;

namespace Database.DTOs;

public class NoteEntryDto
{
    public string RowKey { get; set; } = string.Empty;
    public required string NoteRowKey { get; set; }
    public double Time { get; set; }
    public required string Step { get; set; }
    public string Details { get; set; } = string.Empty;
    public int Index { get; set; }
    public double TemperatureMin { get; set; }
    public double? TemperatureMax { get; set; }
    public List<NoteEntryRuleDto> Rules { get; set; } = [];
    public List<NoteEntryOverrideDto> Overrides { get; set; } = [];

    public NoteEntryEntity ToEntity()
    {
        return new NoteEntryEntity
        {
            RowKey = RowKey,
            NoteRowKey = NoteRowKey,
            Time = Time,
            Step = Step,
            Details = Details,
            Index = Index,
            TemperatureMin = TemperatureMin,
            TemperatureMax = TemperatureMax,
        };
    }

    public NoteEntryEntity ToEntity(string noteRowKey)
    {
        return new NoteEntryEntity
        {
            RowKey = RowKey,
            NoteRowKey = noteRowKey,
            Time = Time,
            Step = Step,
            Details = Details,
            Index = Index,
            TemperatureMin = TemperatureMin,
            TemperatureMax = TemperatureMax,
        };
    }
}
