namespace Database.Entities;

/// <summary>Visitor comment on a public collection page.</summary>
public class CollectionPublicCommentEntity : BaseEntity
{
    public string CollectionId { get; set; } = string.Empty;

    public required string AuthorName { get; set; }

    public required string Body { get; set; }

    public CollectionEntity Collection { get; set; } = default!;

    protected override int IdLength() => 16;
}
