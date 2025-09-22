namespace Database.Helpers;

public static class EnumHelpers
{
    public static T ToEnum<T>(this string value) where T : struct
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidCastException($"Value cannot be null");
        }

        // First, try parsing the original value
        if (Enum.TryParse(value, ignoreCase: true, out T result))
        {
            return result;
        }

        // If that fails, try removing spaces and parsing again
        var valueWithoutSpaces = value.Replace(" ", "");
        if (Enum.TryParse(valueWithoutSpaces, ignoreCase: true, out result))
        {
            return result;
        }

        throw new InvalidCastException($"{value} could not be casted");
    }

    public static string ToDisplayString(this Database.DBObjects.Enums.EFilmType filmType)
    {
        return filmType switch
        {
            Database.DBObjects.Enums.EFilmType.ColorNegative => "Color Negative",
            Database.DBObjects.Enums.EFilmType.ColorPositive => "Color Positive",
            Database.DBObjects.Enums.EFilmType.BlackAndWhite => "Black and White",
            _ => filmType.ToString()
        };
    }
}
