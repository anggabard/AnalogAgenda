using Database.DTOs.Subclasses;

namespace Database.DTOs;

public class NoteDto : HasImage
{
    public string Id { get; set; } = string.Empty;
    public required string Name { get; set; }
    public string SideNote { get; set; } = string.Empty;
    public List<NoteEntryDto> Entries { get; set; } = [];
}
