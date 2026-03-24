namespace Database.DTOs;

public class DevKitFilmAssignmentRowDto
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Brand { get; set; } = string.Empty;

    public string Iso { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string FormattedExposureDate { get; set; } = string.Empty;

    public bool IsSelected { get; set; }
}
