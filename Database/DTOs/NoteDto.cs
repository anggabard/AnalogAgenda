using Database.Entities;

namespace Database.DTOs;

public class NoteDto
{
    public string RowKey { get; set; } = string.Empty;
    public required string Name { get; set; }
    public List<NoteEntryDto> Entries { get; set; } = [];
    
    public (NoteEntity, List<NoteEntryEntity>) ToEntity()
    {
        var entity = new NoteEntity { Name = Name, RowKey = RowKey };
        List<NoteEntryEntity> entries = [.. Entries.Select(entry => entry.ToEntity())];

        return (entity, entries);
    }

    public NoteEntity ToNoteEntity()
    {
        return new NoteEntity { Name = Name, RowKey = RowKey };
    }

    public List<NoteEntryEntity> ToNoteEntryEntities(string noteRowKey)
    {
        return [.. Entries.Select(entry => entry.ToEntity(noteRowKey))];
    }
}
