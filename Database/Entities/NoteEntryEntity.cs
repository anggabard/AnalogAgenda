using Database.DBObjects.Enums;
using Database.DTOs;

namespace Database.Entities;

public class NoteEntryEntity : BaseEntity
{
    public NoteEntryEntity() : base(TableName.NotesEntries) { }

    public string NoteRowKey { get; set; } = default!;
    public required double Time { get; set; }
    public required string Step { get; set; }
    public string Details { get; set; } = string.Empty;
    public int Index { get; set; }
    public double TemperatureMin { get; set; }
    public double? TemperatureMax { get; set; }

    protected override int RowKeyLenght() => 8;

    public NoteEntryDto ToDTO()
    {
        return new NoteEntryDto
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
}
