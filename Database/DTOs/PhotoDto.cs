using Database.DTOs.Subclasses;

namespace Database.DTOs;

public class PhotoDto : HasImage
{
    public string Id { get; set; } = string.Empty;

    public required string FilmId { get; set; }

    public int Index { get; set; }

    public bool Restricted { get; set; }
}
