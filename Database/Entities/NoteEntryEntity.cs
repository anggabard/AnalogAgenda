using Database.DTOs;

namespace Database.Entities;

public class NoteEntryEntity : BaseEntity
{
    public string NoteId { get; set; } = default!;
    public required double Time { get; set; }
    public required string Step { get; set; }
    public string Details { get; set; } = string.Empty;
    public int Index { get; set; }
    public double TemperatureMin { get; set; }
    public double? TemperatureMax { get; set; }

    // Navigation properties
    public NoteEntity Note { get; set; } = default!;
    public ICollection<NoteEntryRuleEntity> Rules { get; set; } = [];
    public ICollection<NoteEntryOverrideEntity> Overrides { get; set; } = [];

    protected override int IdLength() => 8;

    public void Update(NoteEntryDto dto)
    {
        Time = dto.Time;
        Step = dto.Step;
        Details = dto.Details;
        Index = dto.Index;
        TemperatureMin = dto.TemperatureMin;
        TemperatureMax = dto.TemperatureMax;
    }

    public NoteEntryDto ToDTO()
    {
        return new NoteEntryDto
        {
            Id = Id,
            NoteId = NoteId,
            Time = Time,
            Step = Step,
            Details = Details,
            Index = Index,
            TemperatureMin = TemperatureMin,
            TemperatureMax = TemperatureMax,
            Rules = [.. Rules.Select(r => r.ToDTO())],
            Overrides = [.. Overrides.Select(o => o.ToDTO())],
        };
    }
}
