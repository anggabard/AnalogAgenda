using Database.DBObjects;

namespace Database.Helpers;

/// <summary>
/// UTC 12-hour windows and a deterministic index so all API replicas pick the same photo without shared mutable state.
/// </summary>
public static class PhotoOfTheDayPeriodHelper
{
    private static readonly DateTime UnixEpochUtc = DateTime.UnixEpoch;

    /// <summary>
    /// Zero-based index of the current UTC half-day since Unix epoch (each window is 12 hours).
    /// </summary>
    public static long GetUtcHalfDayIndex(DateTime utcNow)
    {
        utcNow = NormalizeToUtc(utcNow);
        var elapsed = utcNow - UnixEpochUtc;
        return (long)Math.Floor(elapsed.TotalHours / 12.0);
    }

    /// <summary>
    /// Stable index from 0 to count - 1 for this half-day bucket (same inputs ⇒ same index on every machine).
    /// </summary>
    public static int GetDeterministicIndex(long utcHalfDayIndex, int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(count, 0);
        unchecked
        {
            var h = (uint)HashCode.Combine(utcHalfDayIndex, Constants.AnalogAgendaGenesis);
            return (int)(h % (uint)count);
        }
    }

    private static DateTime NormalizeToUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };
    }
}
