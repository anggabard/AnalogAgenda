namespace Database.Entities;

/// <summary>
/// Junction: dev kit used on a session. Assignment modals read only this table; SessionDevKit is updated on save to match.
/// </summary>
public class DevKitSessionEntity
{
    public string DevKitId { get; set; } = string.Empty;

    public string SessionId { get; set; } = string.Empty;

    public DevKitEntity? DevKit { get; set; }

    public SessionEntity? Session { get; set; }
}
