namespace Database.Entities;

public class IdeaEntity : BaseEntity
{
    public required string Title { get; set; }

    public string Description { get; set; } = string.Empty;

    public string Outcome { get; set; } = string.Empty;

    public ICollection<IdeaSessionEntity> IdeaSessions { get; set; } = [];

    protected override int IdLength() => 3;
}
