using Database.Entities;

namespace Database.Services.Interfaces;

public interface IPhotoOfTheDayService
{
    /// <summary>
    /// Returns the current pick as a <see cref="PhotoEntity"/> if still valid (&lt; 12h and row exists unrestricted), otherwise repicks.
    /// Picked-at is tracked only in this service (in-memory), not persisted.
    /// </summary>
    Task<PhotoEntity?> GetCurrentOrRefreshAsync(CancellationToken cancellationToken = default);
}
