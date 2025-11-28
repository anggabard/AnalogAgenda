using Database.DBObjects.Enums;
using Database.DTOs;

namespace Database.Entities;

public class UsedDevKitThumbnailEntity : BaseEntity, IImageEntity
{
    public required string DevKitName { get; set; }

    public required Guid ImageId { get; set; }

    protected override int IdLength() => 6;
}

