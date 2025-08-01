namespace Database.Helpers;

public static class EnumHelpers
{
    public static T ToEnum<T>(this string value) where T : struct
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidCastException($"Value cannot be null");
        }

        if (Enum.TryParse(value, ignoreCase: true, out T result))
        {
            return result;
        }

        throw new InvalidCastException($"{value} could not be casted");
    }
}
