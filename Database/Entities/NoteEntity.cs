using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Helpers;

namespace Database.Entities;

public class NoteEntity : BaseEntity, IImageEntity
{
    public NoteEntity() : base(TableName.Notes) { }

    public required string Name { get; set; }

    public string SideNote { get; set; } = string.Empty;

    public Guid ImageId { get; set; }

    protected override int RowKeyLenght() => 4;

    public NoteDto ToDTO(string accountName)
    {
        return new NoteDto()
        {
            RowKey = RowKey,
            Name = Name,
            SideNote = SideNote,
            ImageUrl = ImageId == Guid.Empty ? string.Empty : BlobUrlHelper.GetUrlFromImageImageInfo(accountName, ContainerName.notes.ToString(), ImageId)
        };
    }

    public NoteDto ToDTO(string accountName, List<NoteEntryEntity> noteEntries)
    {
        return new NoteDto() { 
            RowKey = RowKey,
            Name = Name,
            SideNote = SideNote,
            ImageUrl = ImageId == Guid.Empty ? string.Empty : BlobUrlHelper.GetUrlFromImageImageInfo(accountName, ContainerName.notes.ToString(), ImageId),
            Entries = [.. noteEntries.Select(entry => entry.ToDTO())]
        };
    }
}
