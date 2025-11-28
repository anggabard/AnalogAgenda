using Database.DTOs.Subclasses;
using System.Text.Json;

namespace Database.DTOs;

public class SessionDto : HasImage
{
    public string Id { get; set; } = string.Empty;

    public DateOnly SessionDate { get; set; }

    public required string Location { get; set; }

    public required string Participants { get; set; } // JSON array as string

    public string Description { get; set; } = string.Empty;

    public string UsedSubstances { get; set; } = string.Empty; // Comma-separated DevKit Ids

    public string DevelopedFilms { get; set; } = string.Empty; // Comma-separated Film Ids

    // Helper properties for frontend
    public List<string> ParticipantsList
    {
        get => string.IsNullOrEmpty(Participants) ? [] : JsonSerializer.Deserialize<List<string>>(Participants) ?? [];
        set => Participants = JsonSerializer.Serialize(value);
    }

    public List<string> UsedSubstancesList
    {
        get => string.IsNullOrEmpty(UsedSubstances) ? [] : UsedSubstances.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        set => UsedSubstances = value == null || value.Count == 0 ? string.Empty : string.Join(",", value);
    }

    public List<string> DevelopedFilmsList
    {
        get => string.IsNullOrEmpty(DevelopedFilms) ? [] : DevelopedFilms.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        set => DevelopedFilms = value == null || value.Count == 0 ? string.Empty : string.Join(",", value);
    }

    // Dictionary mapping DevKit Id to list of Film Ids developed with that DevKit
    public Dictionary<string, List<string>> FilmToDevKitMapping { get; set; } = [];
}
