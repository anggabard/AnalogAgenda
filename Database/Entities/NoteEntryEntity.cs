using Database.DBObjects.Enums;
using Database.Helpers;

namespace Database.Entities;

public class NoteEntryEntity : BaseEntity
{
    public NoteEntryEntity() : base(TableName.NotesEntries) { }

    public string NoteRowKey { get; set; } = default!;
    public required TimeSpan Time { get; set; }
    public required ENoteEntryType ProcessType { get; set; }
    public required string Film { get; set; }
    public string Details { get; set; } = string.Empty;

    protected override string GetId(params string[] inputs)
    {
        return IdGenerator.Get(8, NoteRowKey, Time.Ticks.ToString(), ProcessType.ToString(), Film, Details);
    }
}
