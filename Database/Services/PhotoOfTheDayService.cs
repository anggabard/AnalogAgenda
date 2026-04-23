using Database.Data;
using Database.Entities;
using Database.Helpers;
using Database.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Database.Services;

/// <summary>
/// Picks the photo of the (12h) period from unrestricted photos using a UTC half-day bucket and a
/// deterministic index. All replicas agree without DB persistence or in-process static cache.
/// </summary>
public sealed class PhotoOfTheDayService(AnalogAgendaDbContext context) : IPhotoOfTheDayService
{
    private readonly AnalogAgendaDbContext _context = context;

    public async Task<PhotoEntity?> GetCurrentOrRefreshAsync(CancellationToken cancellationToken = default)
    {
        var halfDayIndex = PhotoOfTheDayPeriodHelper.GetUtcHalfDayIndex(DateTime.UtcNow);
        return await PickForHalfDayAsync(halfDayIndex, cancellationToken).ConfigureAwait(false);
    }

    private async Task<PhotoEntity?> PickForHalfDayAsync(long utcHalfDayIndex, CancellationToken cancellationToken)
    {
        var unrestricted = _context.Photos.AsNoTracking().Where(photo => !photo.Restricted);
        var count = await unrestricted.CountAsync(cancellationToken).ConfigureAwait(false);
        if (count == 0)
        {
            return null;
        }

        var index = PhotoOfTheDayPeriodHelper.GetDeterministicIndex(utcHalfDayIndex, count);
        return await unrestricted
            .OrderBy(photo => photo.Id)
            .Skip(index)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
