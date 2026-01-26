namespace Database.DTOs;

public class UserSettingsDto
{
    public string UserId { get; set; } = string.Empty;

    public bool IsSubscribed { get; set; }

    public string? CurrentFilmId { get; set; }

    public bool TableView { get; set; }

    public int EntitiesPerPage { get; set; }
}

