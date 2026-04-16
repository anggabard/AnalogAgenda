namespace Database.DTOs;

public class CollectionPublicVerifyDto
{
    public string Password { get; set; } = string.Empty;
}

public class CollectionPublicCommentDto
{
    public string Id { get; set; } = string.Empty;

    public string AuthorName { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}

public class CollectionPublicCommentPostDto
{
    public string AuthorName { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;
}

/// <summary>Anonymous GET: either password required or full payload.</summary>
public class PublicCollectionPageDto
{
    public bool RequiresPassword { get; set; }

    public string? Id { get; set; }

    public string? Name { get; set; }

    public DateOnly? FromDate { get; set; }

    public DateOnly? ToDate { get; set; }

    public string? Location { get; set; }

    public string? Description { get; set; }

    public string? FeaturedImageUrl { get; set; }

    public List<PhotoDto> Photos { get; set; } = [];

    public List<CollectionPublicCommentDto> Comments { get; set; } = [];
}
