using Database.DTOs;

namespace Database.Entities;

public class NoteEntryRuleEntity : BaseEntity
{
    public string NoteEntryId { get; set; } = default!;
    public int FilmInterval { get; set; }
    public double TimeIncrement { get; set; }

    // Navigation property
    public NoteEntryEntity NoteEntry { get; set; } = default!;

    protected override int IdLength() => 8;
}
