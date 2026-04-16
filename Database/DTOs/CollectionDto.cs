namespace Database.DTOs;

public class CollectionDto
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public DateOnly? FromDate { get; set; }

    public DateOnly? ToDate { get; set; }

    public string Location { get; set; } = string.Empty;

    /// <summary>Optional description (public page and edit form).</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Blob id for the card image (write on create/update).</summary>
    public string ImageId { get; set; } = string.Empty;

    public bool IsOpen { get; set; } = true;

    /// <summary>When set true, <see cref="PublicPassword"/> may be required to set the share password (hashed server-side).</summary>
    public bool IsPublic { get; set; }

    /// <summary>Plaintext share password when making the collection public (max 32 characters); never returned from GET APIs.</summary>
    public string? PublicPassword { get; set; }

    public string Owner { get; set; } = string.Empty;

    /// <summary>Photo ids belonging to this collection (write on create/update).</summary>
    public List<string> PhotoIds { get; set; } = [];

    public int PhotoCount { get; set; }

    /// <summary>Resolved URL for list cards (photos container).</summary>
    public string ImageUrl { get; set; } = string.Empty;
}

/// <summary>Minimal row for adding photos to an open collection.</summary>
public class CollectionOptionDto
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;
}
