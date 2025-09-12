using Database.Entities;

namespace Database.DTOs;

public class NoteEntryDto
{
    public string RowKey { get; set; } = string.Empty;
    public required string NoteRowKey { get; set; }
    public double Time { get; set; }
    public required string Process { get; set; }
    public required string Film { get; set; }
    public string Details { get; set; } = string.Empty;

    public NoteEntryEntity ToEntity()
    {
        return new NoteEntryEntity
        {
            RowKey = RowKey,
            NoteRowKey = NoteRowKey,
            Time = Time,
            Process = Process,
            Film = Film,
            Details = Details,
        };
    }

    public NoteEntryEntity ToEntity(string noteRowKey)
    {
        return new NoteEntryEntity
        {
            RowKey = RowKey,
            NoteRowKey = noteRowKey,
            Time = Time,
            Process = Process,
            Film = Film,
            Details = Details,
        };
    }
}
