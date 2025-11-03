using Database.Entities;

namespace Database.DTOs;

public class NoteEntryDto
{
    public string Id { get; set; } = string.Empty;
    public required string NoteId { get; set; }
    public double Time { get; set; }
    public required string Process { get; set; }
    public required string Film { get; set; }
    public string Details { get; set; } = string.Empty;

    public NoteEntryEntity ToEntity()
    {
        return new NoteEntryEntity
        {
            Id = Id,
            NoteId = NoteId,
            Time = Time,
            Process = Process,
            Film = Film,
            Details = Details,
        };
    }

    public NoteEntryEntity ToEntity(string noteId)
    {
        return new NoteEntryEntity
        {
            Id = Id,
            NoteId = noteId,
            Time = Time,
            Process = Process,
            Film = Film,
            Details = Details,
        };
    }
}
