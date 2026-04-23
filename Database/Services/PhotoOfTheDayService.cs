using Database.Data;
using Database.Entities;
using Database.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Database.Services;

/// <summary>
/// Photo-of-the-day pick is scoped per request for DI; the 12h id and picked time are stored in static fields so they apply process-wide (no DB migrations).
/// In multi-replica deployments each instance has its own static state until restart.
/// </summary>
public sealed class PhotoOfTheDayService(AnalogAgendaDbContext context) : IPhotoOfTheDayService
{
    private static readonly TimeSpan PickTtl = TimeSpan.FromHours(12);
    private static readonly SemaphoreSlim Gate = new(1, 1);

    private static PhotoEntity? cachedPhoto;
    private static DateTime pickedAtUtc;

    private readonly AnalogAgendaDbContext _context = context;

    public async Task<PhotoEntity?> GetCurrentOrRefreshAsync(CancellationToken cancellationToken = default)
    {
        await Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var utcNow = DateTime.UtcNow;

            if (cachedPhoto != null && utcNow - pickedAtUtc < PickTtl)
            {
                return cachedPhoto;
            }

            var photoEntity = await PickRandomUnrestrictedPhotoAsync(cancellationToken).ConfigureAwait(false);
            if (photoEntity is null)
                return null;

            cachedPhoto = photoEntity;
            pickedAtUtc = utcNow;
            return photoEntity;
        }
        finally
        {
            Gate.Release();
        }
    }

    private async Task<PhotoEntity?> PickRandomUnrestrictedPhotoAsync(
        CancellationToken cancellationToken)
    {
        var unrestrictedPhotos = _context.Photos.AsNoTracking().Where(photo => !photo.Restricted);
        var count = await unrestrictedPhotos.CountAsync(cancellationToken).ConfigureAwait(false);
        if (count == 0)
            return null;

        var skip = Random.Shared.Next(0, count);
        var photoEntity = await unrestrictedPhotos
            .OrderBy(photo => photo.Id)
            .Skip(skip)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return photoEntity;
    }
}
