using Database.DTOs.Subclasses;
using Database.Entities;
using Database.Helpers;

namespace Database.DTOs;

public class NoteDto : HasImage
{
    public string RowKey { get; set; } = string.Empty;
    public required string Name { get; set; }
    public string SideNote { get; set; } = string.Empty;
    public List<NoteEntryDto> Entries { get; set; } = [];
    
    public (NoteEntity, List<NoteEntryEntity>) ToEntity()
    {
        var entity = new NoteEntity {
            RowKey = RowKey,
            Name = Name,
            SideNote = SideNote,
            ImageId = string.IsNullOrEmpty(ImageUrl) ? Guid.Empty : BlobUrlHelper.GetImageInfoFromUrl(ImageUrl).ImageId
        };

        List<NoteEntryEntity> entries = [.. Entries.Select(entry => entry.ToEntity())];
        return (entity, entries);
    }

    public NoteEntity ToNoteEntity()
    {
        return new NoteEntity
        {
            RowKey = RowKey,
            Name = Name,
            SideNote = SideNote,
            ImageId = string.IsNullOrEmpty(ImageUrl) ? Guid.Empty : BlobUrlHelper.GetImageInfoFromUrl(ImageUrl).ImageId
        };
    }

    public List<NoteEntryEntity> ToNoteEntryEntities(string noteRowKey)
    {
        return [.. Entries.Select(entry => entry.ToEntity(noteRowKey))];
    }
}
