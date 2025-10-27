using Database.DTOs.Subclasses;
using Database.Entities;
using Database.Helpers;
using System.Text.Json;

namespace Database.DTOs;

public class SessionDto : HasImage
{
    public string RowKey { get; set; } = string.Empty;

    public DateOnly SessionDate { get; set; }

    public required string Location { get; set; }

    public required string Participants { get; set; } // JSON array as string

    public string Description { get; set; } = string.Empty;

    public string UsedSubstances { get; set; } = string.Empty; // JSON array of DevKit RowKeys

    public string DevelopedFilms { get; set; } = string.Empty; // JSON array of Film RowKeys

    // Helper properties for frontend
    public List<string> ParticipantsList
    {
        get => string.IsNullOrEmpty(Participants) ? [] : JsonSerializer.Deserialize<List<string>>(Participants) ?? [];
        set => Participants = JsonSerializer.Serialize(value);
    }

    public List<string> UsedSubstancesList
    {
        get => string.IsNullOrEmpty(UsedSubstances) ? [] : JsonSerializer.Deserialize<List<string>>(UsedSubstances) ?? [];
        set => UsedSubstances = JsonSerializer.Serialize(value);
    }

    public List<string> DevelopedFilmsList
    {
        get => string.IsNullOrEmpty(DevelopedFilms) ? [] : JsonSerializer.Deserialize<List<string>>(DevelopedFilms) ?? [];
        set => DevelopedFilms = JsonSerializer.Serialize(value);
    }

    // Dictionary mapping DevKit RowKey to list of Film RowKeys developed with that DevKit
    public Dictionary<string, List<string>> FilmToDevKitMapping { get; set; } = [];

    public SessionEntity ToEntity()
    {
        return new SessionEntity
        {
            RowKey = RowKey,
            SessionDate = SessionDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            Location = Location,
            Participants = Participants,
            ImageId = string.IsNullOrEmpty(ImageUrl) ? Guid.Empty : BlobUrlHelper.GetImageInfoFromUrl(ImageUrl).ImageId,
            Description = Description,
            UsedSubstances = UsedSubstances,
            DevelopedFilms = DevelopedFilms
        };
    }
}
