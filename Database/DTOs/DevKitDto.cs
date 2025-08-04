namespace Database.DTOs;

public class DevKitDto
{
    public required string Name { get; set; }

    public required string Url { get; set; }

    public required string Type { get; set; }

    public required string PurchasedBy { get; set; }

    public DateOnly PurchasedOn { get; set; }

    public DateOnly MixedOn { get; set; }

    public int ValidForWeeks { get; set; }

    public int ValidForFilms { get; set; }

    public int FilmsDeveloped { get; set; }

    public string ImageAsBase64 { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}
