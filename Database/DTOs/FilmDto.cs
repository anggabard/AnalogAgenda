namespace Database.DTOs;

public class FilmDto
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public required string Brand { get; set; }

    public required string Iso { get; set; }

    public required string Type { get; set; }

    public int NumberOfExposures { get; set; } = 36;

    public double Cost { get; set; }

    public required string PurchasedBy { get; set; }

    public DateOnly PurchasedOn { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool Developed { get; set; }

    public string? DevelopedInSessionId { get; set; }

    public string? DevelopedWithDevKitId { get; set; }

    public string FormattedExposureDate { get; set; } = string.Empty;

    public int PhotoCount { get; set; } = 0;
}
