using Database.DBObjects.Enums;
using Database.DTOs;

namespace Database.Entities;

public class NoteEntity : BaseEntity, IImageEntity
{
    public required string Name { get; set; }

    public string SideNote { get; set; } = string.Empty;

    public Guid ImageId { get; set; }

    // Navigation property
    public ICollection<NoteEntryEntity> Entries { get; set; } = new List<NoteEntryEntity>();

    protected override int IdLength() => 4;

    public void Update(NoteDto dto)
    {
        Name = dto.Name;
        SideNote = dto.SideNote;
        
        // ImageId is handled in the controller (uploaded to blob storage)
    }
}
