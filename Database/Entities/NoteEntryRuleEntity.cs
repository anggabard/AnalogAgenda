using Database.DBObjects.Enums;
using Database.DTOs;

namespace Database.Entities;

public class NoteEntryRuleEntity : BaseEntity
{
    public NoteEntryRuleEntity() : base(TableName.NotesEntryRules) { }

    public string NoteEntryRowKey { get; set; } = default!;
    public int FilmInterval { get; set; }
    public double TimeIncrement { get; set; }

    protected override int RowKeyLenght() => 6;

    public NoteEntryRuleDto ToDTO()
    {
        return new NoteEntryRuleDto
        {
            RowKey = RowKey,
            NoteEntryRowKey = NoteEntryRowKey,
            FilmInterval = FilmInterval,
            TimeIncrement = TimeIncrement,
        };
    }
}
