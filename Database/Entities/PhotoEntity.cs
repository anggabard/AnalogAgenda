using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Helpers;

namespace Database.Entities;

public class PhotoEntity : BaseEntity, IImageEntity
{
    public PhotoEntity() : base(TableName.Photos) { }

    public required string FilmRowId { get; set; }

    public int Index { get; set; }

    public Guid ImageId { get; set; }

    protected override int RowKeyLenght() => 8;

    public PhotoDto ToDTO(string accountName)
    {
        return new PhotoDto()
        {
            RowKey = RowKey,
            FilmRowId = FilmRowId,
            Index = Index,
            ImageUrl = ImageId == Guid.Empty ? string.Empty : BlobUrlHelper.GetUrlFromImageImageInfo(accountName, ContainerName.photos.ToString(), ImageId)
        };
    }
}
