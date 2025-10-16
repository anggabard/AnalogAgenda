using Database.Entities;

namespace Database.DTOs;

public class UsedDevKitThumbnailDto
{
    public string RowKey { get; set; } = string.Empty;

    public required string DevKitName { get; set; }

    public string ImageId { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;

    public string ImageBase64 { get; set; } = string.Empty;

    public UsedDevKitThumbnailEntity ToEntity()
    {
        return new UsedDevKitThumbnailEntity
        {
            RowKey = RowKey,
            DevKitName = DevKitName,
            ImageId = string.IsNullOrEmpty(ImageId) ? Guid.Empty : Guid.Parse(ImageId)
        };
    }
}

