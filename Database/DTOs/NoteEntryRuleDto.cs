using Database.Entities;

namespace Database.DTOs;

public class NoteEntryRuleDto
{
    public string Id { get; set; } = string.Empty;
    public required string NoteEntryId { get; set; }
    public int FilmInterval { get; set; }
    public double TimeIncrement { get; set; }

    public NoteEntryRuleEntity ToEntity()
    {
        return new NoteEntryRuleEntity
        {
            Id = Id,
            NoteEntryId = NoteEntryId,
            FilmInterval = FilmInterval,
            TimeIncrement = TimeIncrement,
        };
    }

    public NoteEntryRuleEntity ToEntity(string noteEntryId)
    {
        return new NoteEntryRuleEntity
        {
            Id = Id,
            NoteEntryId = noteEntryId,
            FilmInterval = FilmInterval,
            TimeIncrement = TimeIncrement,
        };
    }
}
