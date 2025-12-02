namespace Database.DTOs;

public class NoteEntryRuleDto
{
    public string Id { get; set; } = string.Empty;
    public required string NoteEntryId { get; set; }
    public int FilmInterval { get; set; }
    public double TimeIncrement { get; set; }
}
