using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Helpers;

namespace Database.Entities;

public class NoteEntity : BaseEntity, IImageEntity
{
    public required string Name { get; set; }

    public string SideNote { get; set; } = string.Empty;

    public Guid ImageId { get; set; }

    // Navigation property
    public ICollection<NoteEntryEntity> Entries { get; set; } = new List<NoteEntryEntity>();

    protected override int IdLength() => 4;

    public NoteDto ToDTO(string accountName)
    {
        return new NoteDto()
        {
            Id = Id,
            Name = Name,
            SideNote = SideNote,
            ImageUrl = ImageId == Guid.Empty ? string.Empty : BlobUrlHelper.GetUrlFromImageImageInfo(accountName, ContainerName.notes.ToString(), ImageId)
        };
    }

    public NoteDto ToDTO(string accountName, List<NoteEntryEntity> noteEntries)
    {
        return new NoteDto()
        {
            Id = Id,
            Name = Name,
            SideNote = SideNote,
            ImageUrl = ImageId == Guid.Empty ? string.Empty : BlobUrlHelper.GetUrlFromImageImageInfo(accountName, ContainerName.notes.ToString(), ImageId),
            Entries = [.. noteEntries.Select(entry => entry.ToDTO())]
        };
    }
}
