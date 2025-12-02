using Database.DBObjects.Enums;

namespace Database.Entities;

public class PhotoEntity : BaseEntity, IImageEntity
{
    public required string FilmId { get; set; }

    public int Index { get; set; }

    public Guid ImageId { get; set; }

    // Navigation property
    public FilmEntity Film { get; set; } = default!;

    protected override int IdLength() => 16;
}
