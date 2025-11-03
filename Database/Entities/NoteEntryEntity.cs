using Database.DTOs;

namespace Database.Entities;

public class NoteEntryEntity : BaseEntity
{
    public string NoteId { get; set; } = default!;
    public required double Time { get; set; }
    public required string Process { get; set; }
    public required string Film { get; set; }
    public string Details { get; set; } = string.Empty;

    // Navigation property
    public NoteEntity Note { get; set; } = default!;

    protected override int IdLength() => 8;

    public NoteEntryDto ToDTO()
    {
        return new NoteEntryDto
        {
            Id = Id,
            NoteId = NoteId,
            Time = Time,
            Process = Process,
            Film = Film,
            Details = Details,
        };
    }
}
