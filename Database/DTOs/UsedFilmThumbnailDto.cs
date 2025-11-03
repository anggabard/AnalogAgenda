using Database.Entities;

namespace Database.DTOs;

public class UsedFilmThumbnailDto
{
    public string Id { get; set; } = string.Empty;

    public required string FilmName { get; set; }

    public string ImageId { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;

    public string ImageBase64 { get; set; } = string.Empty;

    public UsedFilmThumbnailEntity ToEntity()
    {
        return new UsedFilmThumbnailEntity
        {
            Id = Id,
            FilmName = FilmName,
            ImageId = string.IsNullOrEmpty(ImageId) ? Guid.Empty : Guid.Parse(ImageId)
        };
    }
}

