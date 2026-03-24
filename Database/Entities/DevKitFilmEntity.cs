namespace Database.Entities;

/// <summary>
/// Junction: film developed with a dev kit. Assignment modals read only this table; Film.DevelopedWithDevKitId is updated on save to match.
/// </summary>
public class DevKitFilmEntity
{
    public string DevKitId { get; set; } = string.Empty;

    public string FilmId { get; set; } = string.Empty;

    public DevKitEntity? DevKit { get; set; }

    public FilmEntity? Film { get; set; }
}
