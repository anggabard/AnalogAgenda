using Database.DBObjects.Enums;
using Database.DTOs;

namespace Database.Entities;

public class NoteEntryEntity : BaseEntity
{
    public NoteEntryEntity() : base(TableName.NotesEntries) { }

    public string NoteRowKey { get; set; } = default!;
    public required double Time { get; set; }
    public required string Process { get; set; }
    public required string Film { get; set; }
    public string Details { get; set; } = string.Empty;

    protected override int RowKeyLenght() => 8;

    public NoteEntryDto ToDTO()
    {
        return new NoteEntryDto
        {
            RowKey = RowKey,
            NoteRowKey = NoteRowKey,
            Time = Time,
            Process = Process,
            Film = Film,
            Details = Details,
        };
    }
}
