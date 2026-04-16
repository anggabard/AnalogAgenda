namespace Database.Entities;

public class PhotoEntity : BaseEntity, IImageEntity
{
    public required string FilmId { get; set; }

    public int Index { get; set; }

    public Guid ImageId { get; set; }

    public bool Restricted { get; set; }

    // Navigation property
    public FilmEntity Film { get; set; } = default!;

    public ICollection<CollectionEntity> Collections { get; set; } = [];

    public ICollection<CollectionPhotoEntity> CollectionPhotoLinks { get; set; } = [];

    protected override int IdLength() => 16;
}
