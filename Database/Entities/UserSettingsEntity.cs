namespace Database.Entities;

public class UserSettingsEntity : BaseEntity
{
    public required string UserId { get; set; }

    public bool IsSubscribed { get; set; } = false;

    public string? CurrentFilmId { get; set; }

    public bool TableView { get; set; } = false;

    public int EntitiesPerPage { get; set; } = 5;

    // Navigation properties
    public UserEntity? User { get; set; }
    public FilmEntity? CurrentFilm { get; set; }

    protected override int IdLength() => 10;
}

