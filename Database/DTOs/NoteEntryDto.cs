namespace Database.DTOs;

public class NoteEntryDto
{
    public string Id { get; set; } = string.Empty;
    public required string NoteId { get; set; }
    public double Time { get; set; }
    public required string Step { get; set; }
    public string Details { get; set; } = string.Empty;
    public int Index { get; set; }
    public double TemperatureMin { get; set; }
    public double? TemperatureMax { get; set; }
    public List<NoteEntryRuleDto> Rules { get; set; } = [];
    public List<NoteEntryOverrideDto> Overrides { get; set; } = [];
}
