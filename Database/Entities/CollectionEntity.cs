using Database.DBObjects;
using Database.DBObjects.Enums;
using Database.DTOs;

namespace Database.Entities;

/// <summary>User-defined photo collection (not tied to a single film).</summary>
public class CollectionEntity : BaseEntity, IImageEntity
{
    public required string Name { get; set; }

    public DateOnly? FromDate { get; set; }

    public DateOnly? ToDate { get; set; }

    public string Location { get; set; } = string.Empty;

    /// <summary>Blob id for the card thumbnail (default or a frame from the collection).</summary>
    public Guid ImageId { get; set; } = Constants.DefaultCollectionImageId;

    public bool IsOpen { get; set; } = true;

    public EUsernameType Owner { get; set; }

    public ICollection<PhotoEntity> Photos { get; set; } = [];

    protected override int IdLength() => 8;

    public void Update(CollectionDto dto)
    {
        Name = dto.Name?.Trim() ?? string.Empty;
        FromDate = dto.FromDate;
        ToDate = dto.ToDate;
        Location = dto.Location ?? string.Empty;
        IsOpen = dto.IsOpen;
        // ImageId is set by the controller after validating against membership + placeholder rules.
    }
}
