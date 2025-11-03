using Database.DTOs.Subclasses;

namespace Database.DTOs;

public class MergedNoteDto : HasImage
{
    public string CompositeId { get; set; } = string.Empty;
    public required string Name { get; set; }
    public string SideNote { get; set; } = string.Empty;
    public List<MergedNoteEntryDto> Entries { get; set; } = [];
}

public class MergedNoteEntryDto
{
    public string RowKey { get; set; } = string.Empty;
    public string NoteRowKey { get; set; } = string.Empty;
    public double Time { get; set; }
    public required string Step { get; set; }
    public string Details { get; set; } = string.Empty;
    public int Index { get; set; }
    public double TemperatureMin { get; set; }
    public double? TemperatureMax { get; set; }
    public required string Substance { get; set; } // Note name
    public double StartTime { get; set; } // Accumulated start time
}
