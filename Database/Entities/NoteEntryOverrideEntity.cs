using Database.DBObjects.Enums;
using Database.DTOs;

namespace Database.Entities;

public class NoteEntryOverrideEntity : BaseEntity
{
    public NoteEntryOverrideEntity() : base(TableName.NotesEntryOverrides) { }

    public string NoteEntryRowKey { get; set; } = default!;
    public int FilmCountMin { get; set; }
    public int FilmCountMax { get; set; }
    public double? Time { get; set; }
    public string? Step { get; set; }
    public string? Details { get; set; }
    public double? TemperatureMin { get; set; }
    public double? TemperatureMax { get; set; }

    protected override int RowKeyLenght() => 6;

    public NoteEntryOverrideDto ToDTO()
    {
        return new NoteEntryOverrideDto
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
}
