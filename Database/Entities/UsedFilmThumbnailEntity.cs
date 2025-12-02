using Database.DBObjects.Enums;
using Database.DTOs;

namespace Database.Entities;

public class UsedFilmThumbnailEntity : BaseEntity, IImageEntity
{
    public required string FilmName { get; set; }

    public required Guid ImageId { get; set; }

    protected override int IdLength() => 6;
}

