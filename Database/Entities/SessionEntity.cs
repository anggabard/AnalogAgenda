using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Helpers;

namespace Database.Entities;

public class SessionEntity : BaseEntity, IImageEntity
{
    public DateTime SessionDate { get; set; }

    public required string Location { get; set; }

    public required string Participants { get; set; } // JSON array as string (keeping for now)

    public Guid ImageId { get; set; }

    public string Description { get; set; } = string.Empty;

    // Navigation properties - converting from JSON arrays to proper relationships
    public ICollection<DevKitEntity> UsedDevKits { get; set; } = new List<DevKitEntity>();
    public ICollection<FilmEntity> DevelopedFilms { get; set; } = new List<FilmEntity>();

    protected override int IdLength() => 10;

    public SessionDto ToDTO(string accountName)
    {
        return new SessionDto()
        {
            Id = Id,
            SessionDate = DateOnly.FromDateTime(SessionDate),
            Location = Location,
            Participants = Participants,
            ImageUrl = ImageId == Guid.Empty ? string.Empty : BlobUrlHelper.GetUrlFromImageImageInfo(accountName, ContainerName.sessions.ToString(), ImageId),
            Description = Description,
            UsedSubstances = string.Join(",", UsedDevKits.Select(d => d.Id)),
            DevelopedFilms = string.Join(",", DevelopedFilms.Select(f => f.Id))
        };
    }
}
