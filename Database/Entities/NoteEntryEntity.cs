using Database.DBObjects.Enums;

namespace Database.Entities;

public class NoteEntryEntity : BaseEntity
{
    public NoteEntryEntity() : base(TableName.NotesEntries) { }

    public string NoteRowKey { get; set; } = default!;
    public required int Time { get; set; }
    public required string Process { get; set; }
    public required string Film { get; set; }
    public string Details { get; set; } = string.Empty;

    protected override int RowKeyLenght() => 8;
}
