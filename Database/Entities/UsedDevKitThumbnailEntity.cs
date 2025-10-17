using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Helpers;

namespace Database.Entities;

public class UsedDevKitThumbnailEntity : BaseEntity, IImageEntity
{
    public UsedDevKitThumbnailEntity() : base(TableName.UsedDevKitThumbnails) { }

    public required string DevKitName { get; set; }

    public required Guid ImageId { get; set; }

    protected override int RowKeyLenght() => 6;

    public UsedDevKitThumbnailDto ToDTO(string accountName)
    {
        return new UsedDevKitThumbnailDto()
        {
            RowKey = RowKey,
            DevKitName = DevKitName,
            ImageId = ImageId.ToString(),
            ImageUrl = ImageId == Guid.Empty ? string.Empty : BlobUrlHelper.GetUrlFromImageImageInfo(accountName, ContainerName.devkits.ToString(), ImageId)
        };
    }
}

