using Database.DBObjects.Enums;
using Database.DTOs;

namespace Database.Entities;

public class NoteEntity : BaseEntity
{
    public NoteEntity() : base(TableName.Notes) { }

    public required string Name { get; set; }

    protected override int RowKeyLenght() => 4;

    public NoteDto ToDTO()
    {
        return new NoteDto()
        {
            RowKey = RowKey,
            Name = Name
        };
    }

    public NoteDto ToDTO(List<NoteEntryEntity> noteEntries)
    {
        return new NoteDto() { 
            RowKey = RowKey,
            Name = Name,
            Entries = [.. noteEntries.Select(entry => entry.ToDTO())]
        };
    }
}
