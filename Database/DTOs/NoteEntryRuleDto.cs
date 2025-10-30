using Database.Entities;

namespace Database.DTOs;

public class NoteEntryRuleDto
{
    public string RowKey { get; set; } = string.Empty;
    public required string NoteEntryRowKey { get; set; }
    public int FilmInterval { get; set; }
    public double TimeIncrement { get; set; }

    public NoteEntryRuleEntity ToEntity()
    {
        return new NoteEntryRuleEntity
        {
            RowKey = RowKey,
            NoteEntryRowKey = NoteEntryRowKey,
            FilmInterval = FilmInterval,
            TimeIncrement = TimeIncrement,
        };
    }

    public NoteEntryRuleEntity ToEntity(string noteEntryRowKey)
    {
        return new NoteEntryRuleEntity
        {
            RowKey = RowKey,
            NoteEntryRowKey = noteEntryRowKey,
            FilmInterval = FilmInterval,
            TimeIncrement = TimeIncrement,
        };
    }
}
