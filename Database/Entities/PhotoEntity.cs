using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Helpers;

namespace Database.Entities;

public class PhotoEntity : BaseEntity, IImageEntity
{
    public required string FilmId { get; set; }

    public int Index { get; set; }

    public Guid ImageId { get; set; }

    // Navigation property
    public FilmEntity Film { get; set; } = default!;

    protected override int IdLength() => 16;

    public PhotoDto ToDTO(string accountName)
    {
        return new PhotoDto()
        {
            Id = Id,
            FilmId = FilmId,
            Index = Index,
            ImageUrl = ImageId == Guid.Empty ? string.Empty : BlobUrlHelper.GetUrlFromImageImageInfo(accountName, ContainerName.photos.ToString(), ImageId)
        };
    }
}
