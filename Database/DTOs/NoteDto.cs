using Database.DTOs.Subclasses;
using Database.Entities;
using Database.Helpers;

namespace Database.DTOs;

public class NoteDto : HasImage
{
    public string Id { get; set; } = string.Empty;
    public required string Name { get; set; }
    public string SideNote { get; set; } = string.Empty;
    public List<NoteEntryDto> Entries { get; set; } = [];

    public (NoteEntity, List<NoteEntryEntity>) ToEntity()
    {
        var entity = new NoteEntity
        {
            Id = Id,
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
            Id = Id,
            Name = Name,
            SideNote = SideNote,
            ImageId = string.IsNullOrEmpty(ImageUrl) ? Guid.Empty : BlobUrlHelper.GetImageInfoFromUrl(ImageUrl).ImageId
        };
    }

    public List<NoteEntryEntity> ToNoteEntryEntities(string noteId)
    {
        return [.. Entries.Select(entry => entry.ToEntity(noteId))];
    }
}
