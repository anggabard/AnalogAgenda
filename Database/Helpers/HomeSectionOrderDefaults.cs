namespace Database.Helpers;

/// <summary>Canonical home dashboard section ids (JSON order array persisted on <see cref="Entities.UserSettingsEntity"/>).</summary>
public static class HomeSectionOrderDefaults
{
    public static readonly string[] ValidSectionIds =
        ["filmCheck", "currentFilm", "settings", "wackyIdeas", "photoOfTheDay"];

    public static bool IsValidOrder(IReadOnlyList<string>? order)
    {
        if (order is null || order.Count != ValidSectionIds.Length)
            return false;

        return new HashSet<string>(order, StringComparer.Ordinal).SetEquals(ValidSectionIds);
    }
}
