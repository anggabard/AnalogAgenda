using Database.DTOs.Subclasses;
using System.Text.Json;

namespace Database.DTOs;

public class SessionDto : HasImage
{
    public string Id { get; set; } = string.Empty;

    /// <summary>Database-generated; not set from client on create.</summary>
    public int Index { get; set; }

    /// <summary>Optional; empty or whitespace => default label uses Index.</summary>
    public string? Name { get; set; }

    /// <summary>Derived for API; never exposes "Session 0" (requires Index &gt;= 1).</summary>
    public string DisplayLabel =>
        string.IsNullOrWhiteSpace(Name)
            ? (Index >= 1 ? $"Session {Index}" : string.Empty)
            : Name.Trim();

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

    /// <summary>Wacky ideas linked to this session (read model when IdeaSessions are loaded).</summary>
    public List<SessionLinkedIdeaSummaryDto> ConnectedIdeas { get; set; } = [];

    /// <summary>Idea ids to sync on create/update (junction IdeaSessions). Invalid ids are ignored.</summary>
    public List<string> ConnectedIdeaIds { get; set; } = [];
}
