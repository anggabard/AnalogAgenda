using Database.DTOs.Subclasses;
using Database.Entities;
using Database.Helpers;

namespace Database.DTOs;

public class PhotoDto : HasImage
{
    public string RowKey { get; set; } = string.Empty;

    public required string FilmRowId { get; set; }

    public int Index { get; set; }

    public PhotoEntity ToEntity()
    {
        return new PhotoEntity
        {
            RowKey = RowKey,
            FilmRowId = FilmRowId,
            Index = Index,
            ImageId = Guid.Empty // ImageId should be set by the controller, not derived from URL
        };
    }
}
