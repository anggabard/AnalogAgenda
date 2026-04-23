using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
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
    /// Stable index from 0 to count - 1 for this half-day bucket (same inputs ⇒ same index on every machine and process).
    /// Uses SHA-256 over algorithm name, half-day index, and <see cref="Constants.AnalogAgendaGenesis"/> (see <see cref="Constants.HashAlgorithmName"/> / <see cref="Constants.HashAlgorithmInputLength"/>).
    /// </summary>
    public static int GetDeterministicIndex(long utcHalfDayIndex, int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(count, 0);

        var algoUtf8 = Encoding.UTF8.GetBytes(Constants.HashAlgorithmName.Name ?? "SHA256");
        var inputLength = algoUtf8.Length + sizeof(long) + sizeof(long);
        var input = new byte[inputLength];
        algoUtf8.CopyTo(input, 0);
        var payload = input.AsSpan(algoUtf8.Length);
        BinaryPrimitives.WriteInt64LittleEndian(payload, utcHalfDayIndex);
        BinaryPrimitives.WriteInt64LittleEndian(payload.Slice(sizeof(long)), Constants.AnalogAgendaGenesis.ToBinary());

        Span<byte> digest = stackalloc byte[Constants.HashAlgorithmInputLength];
        var written = SHA256.HashData(input, digest);
        if (written != Constants.HashAlgorithmInputLength)
        {
            throw new InvalidOperationException(
                $"Expected {Constants.HashAlgorithmInputLength}-byte digest for {Constants.HashAlgorithmName.Name}, got {written}.");
        }

        var h = BinaryPrimitives.ReadUInt32BigEndian(digest);
        return (int)(h % (uint)count);
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
