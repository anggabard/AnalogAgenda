using Database.Entities;

namespace Database.Services.Interfaces;

public interface IPhotoOfTheDayService
{
    /// <summary>
    /// Returns the unrestricted photo for the current UTC 12-hour window. The choice is deterministic from time + eligible set (stable order by id), so all replicas return the same photo.
    /// </summary>
    Task<PhotoEntity?> GetCurrentOrRefreshAsync(CancellationToken cancellationToken = default);
}
