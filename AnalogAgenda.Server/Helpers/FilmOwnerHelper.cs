using Database.DBObjects.Enums;
using Database.Entities;
using Database.Helpers;
using System.Security.Claims;

namespace AnalogAgenda.Server.Helpers;

public static class FilmOwnerHelper
{
    public static bool IsCurrentUserFilmOwner(ClaimsPrincipal? user, FilmEntity film)
    {
        var name = user?.Identity?.IsAuthenticated == true ? user.FindFirstValue(ClaimTypes.Name) : null;
        if (string.IsNullOrEmpty(name))
            return false;
        try
        {
            var currentUserEnum = name.ToEnum<EUsernameType>();
            return film.PurchasedBy == currentUserEnum;
        }
        catch
        {
            return false;
        }
    }
}
