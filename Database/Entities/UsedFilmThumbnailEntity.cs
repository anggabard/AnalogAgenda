using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Helpers;

namespace Database.Entities;

public class UsedFilmThumbnailEntity : BaseEntity
{
    public UsedFilmThumbnailEntity() : base(TableName.UsedFilmThumbnails) { }

    public required string FilmName { get; set; }

    public required Guid ImageId { get; set; }

    protected override int RowKeyLenght() => 6;

    public UsedFilmThumbnailDto ToDTO(string accountName)
    {
        return new UsedFilmThumbnailDto()
        {
            RowKey = RowKey,
            FilmName = FilmName,
            ImageId = ImageId.ToString(),
            ImageUrl = ImageId == Guid.Empty ? string.Empty : BlobUrlHelper.GetUrlFromImageImageInfo(accountName, ContainerName.films.ToString(), ImageId)
        };
    }
}

