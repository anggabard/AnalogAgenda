using System.Security.Cryptography;

namespace Database.Helpers;

public static class PasswordHasher
{
    private static readonly HashAlgorithmName hashAlgorithmName = HashAlgorithmName.SHA256;
    private const ushort inputLength = 32;
    private static readonly DateTime startDate = new(2025, 6, 27, 14, 57, 18, 226, 758, DateTimeKind.Utc);
    private static readonly int iterations = (DateTime.UtcNow - startDate).Days / 30 * 1_000 + 100_000;

    public static bool VerifyPassword(string plain, string stored)
    {
        var parts = stored.Split('.', 3);
        if (parts.Length != 3) return false;

        var iterationsPart = int.Parse(parts[0]);
        var saltPart = Convert.FromBase64String(parts[1]);
        var hashPart = Convert.FromBase64String(parts[2]);

        var testHash = Rfc2898DeriveBytes.Pbkdf2(
                           plain,
                           saltPart,
                           iterationsPart,
                           hashAlgorithmName,
                           inputLength);

        return CryptographicOperations.FixedTimeEquals(hashPart, testHash);
    }

    public static string HashPassword(string plain)
    {
        var salt = RandomNumberGenerator.GetBytes(16);

        var hash = Rfc2898DeriveBytes.Pbkdf2(
                       plain,
                       salt,
                       iterations,
                       hashAlgorithmName,
                       inputLength);

        return $"{iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }
}
