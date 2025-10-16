using Database.Entities;

namespace Database.DTOs;

public class UsedFilmThumbnailDto
{
    public string RowKey { get; set; } = string.Empty;

    public required string FilmName { get; set; }

    public string ImageId { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;

    public string ImageBase64 { get; set; } = string.Empty;

    public UsedFilmThumbnailEntity ToEntity()
    {
        return new UsedFilmThumbnailEntity
        {
            RowKey = RowKey,
            FilmName = FilmName,
            ImageId = string.IsNullOrEmpty(ImageId) ? Guid.Empty : Guid.Parse(ImageId)
        };
    }
}

