using System.ComponentModel.DataAnnotations.Schema;

namespace Database.Entities;

/// <summary>Join row for <see cref="CollectionEntity"/> ↔ <see cref="PhotoEntity"/> with 1-based order in the collection.</summary>
public class CollectionPhotoEntity
{
    public string CollectionsId { get; set; } = string.Empty;

    public string PhotosId { get; set; } = string.Empty;

    /// <summary>1-based position within the collection (SQL column <c>Index</c>).</summary>
    [Column("Index")]
    public int CollectionIndex { get; set; }

    /// <summary>Optional denormalized film id (not used for ordering).</summary>
    public string? FilmId { get; set; }

    public CollectionEntity Collection { get; set; } = null!;

    public PhotoEntity Photo { get; set; } = null!;
}
