namespace Database.Entities;

public class ExposureDateEntity : BaseEntity
{
    public required string FilmId { get; set; }

    public required DateOnly Date { get; set; }

    public string Description { get; set; } = string.Empty;

    // Navigation property
    public FilmEntity Film { get; set; } = default!;

    protected override int IdLength() => 8;
}

