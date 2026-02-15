namespace Database.Entities;

public class IdeaEntity : BaseEntity
{
    public required string Title { get; set; }

    public string Description { get; set; } = string.Empty;

    protected override int IdLength() => 3;
}
