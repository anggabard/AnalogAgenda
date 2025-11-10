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
    public ICollection<DevKitEntity> UsedDevKits { get; set; } = [];
    public ICollection<FilmEntity> DevelopedFilms { get; set; } = [];

    protected override int IdLength() => 10;

    public void Update(SessionDto dto)
    {
        SessionDate = dto.SessionDate.ToDateTime(TimeOnly.MinValue);
        Location = dto.Location;
        Participants = dto.Participants;
        Description = dto.Description;
        
        // ImageId is handled in the controller (uploaded to blob storage)
    }

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
