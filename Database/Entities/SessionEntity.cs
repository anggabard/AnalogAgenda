using System.ComponentModel.DataAnnotations.Schema;
using Database.DBObjects.Enums;
using Database.DTOs;

namespace Database.Entities;

public class SessionEntity : BaseEntity, IImageEntity
{
    public DateTime SessionDate { get; set; }

    public required string Location { get; set; }

    public required string Participants { get; set; } // JSON array as string (keeping for now)

    public Guid ImageId { get; set; }

    public string Description { get; set; } = string.Empty;

    /// <summary>Stable session index 1..N (unique). Database-generated (e.g. SQL Server IDENTITY seed 1).</summary>
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Index { get; set; }

    /// <summary>Optional user label; empty means UI shows "Session {Index}".</summary>
    public string? Name { get; set; }

    // Navigation properties - converting from JSON arrays to proper relationships
    public ICollection<DevKitEntity> UsedDevKits { get; set; } = [];
    public ICollection<FilmEntity> DevelopedFilms { get; set; } = [];
    public ICollection<IdeaSessionEntity> IdeaSessions { get; set; } = [];

    protected override int IdLength() => 10;

    public void Update(SessionDto dto)
    {
        SessionDate = dto.SessionDate.ToDateTime(TimeOnly.MinValue);
        Location = dto.Location;
        Participants = dto.Participants;
        Description = dto.Description;
        Name = string.IsNullOrWhiteSpace(dto.Name) ? null : dto.Name.Trim();

        // ImageId is handled in the controller (uploaded to blob storage)
    }
}
