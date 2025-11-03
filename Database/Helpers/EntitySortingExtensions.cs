using Database.Entities;

namespace Database.Helpers;

/// <summary>
/// Extension methods for common entity sorting patterns
/// </summary>
public static class EntitySortingExtensions
{
    /// <summary>
    /// Apply standard DevKit sorting: by purchase date (oldest first)
    /// </summary>
    public static IOrderedEnumerable<DevKitEntity> ApplyStandardSorting(this IEnumerable<DevKitEntity> devKits)
    {
        return devKits.OrderBy(k => k.PurchasedOn);
    }

    /// <summary>
    /// Apply standard DevKit sorting: by purchase date (oldest first) - IQueryable version
    /// </summary>
    public static IOrderedQueryable<DevKitEntity> ApplyStandardSorting(this IQueryable<DevKitEntity> devKits)
    {
        return devKits.OrderBy(k => k.PurchasedOn);
    }

    /// <summary>
    /// Apply standard Film sorting: by owner first, then by date (newest first)
    /// </summary>
    public static IOrderedEnumerable<FilmEntity> ApplyStandardSorting(this IEnumerable<FilmEntity> films)
    {
        return films.OrderBy(f => f.PurchasedBy).ThenByDescending(f => f.PurchasedOn);
    }

    /// <summary>
    /// Apply standard Film sorting: by owner first, then by date (newest first) - IQueryable version
    /// </summary>
    public static IOrderedQueryable<FilmEntity> ApplyStandardSorting(this IQueryable<FilmEntity> films)
    {
        return films.OrderBy(f => f.PurchasedBy).ThenByDescending(f => f.PurchasedOn);
    }

    /// <summary>
    /// Apply user-filtered Film sorting: by date (newest first)
    /// </summary>
    public static IOrderedEnumerable<FilmEntity> ApplyUserFilteredSorting(this IEnumerable<FilmEntity> films)
    {
        return films.OrderByDescending(f => f.PurchasedOn);
    }

    /// <summary>
    /// Apply standard Photo sorting: by index
    /// </summary>
    public static IOrderedEnumerable<PhotoEntity> ApplyStandardSorting(this IEnumerable<PhotoEntity> photos)
    {
        return photos.OrderBy(p => p.Index);
    }

    /// <summary>
    /// Apply standard Session sorting: by session date (newest first)
    /// </summary>
    public static IOrderedEnumerable<SessionEntity> ApplyStandardSorting(this IEnumerable<SessionEntity> sessions)
    {
        return sessions.OrderByDescending(s => s.SessionDate);
    }

    /// <summary>
    /// Apply standard Session sorting: by session date (newest first) - IQueryable version
    /// </summary>
    public static IOrderedQueryable<SessionEntity> ApplyStandardSorting(this IQueryable<SessionEntity> sessions)
    {
        return sessions.OrderByDescending(s => s.SessionDate);
    }
}
