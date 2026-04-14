using Database.DTOs.Subclasses;

namespace Database.DTOs;

public class PhotoDto : HasImage
{
    public string Id { get; set; } = string.Empty;

    /// <summary>Blob id in the photos container (for picking collection card image).</summary>
    public string ImageId { get; set; } = string.Empty;

    public required string FilmId { get; set; }

    public int Index { get; set; }

    public bool Restricted { get; set; }
}
