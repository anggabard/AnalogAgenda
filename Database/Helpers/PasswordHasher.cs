using Database.DBObjects;
using System.Security.Cryptography;

namespace Database.Helpers;

public static class PasswordHasher
{
    private static readonly int iterations = (DateTime.UtcNow - DBObjects.Constants.AnalogAgendaGenesis).Days / 30 * 1_000 + 100_000;

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
                           DBObjects.Constants.HashAlgorithmName,
                           DBObjects.Constants.HashAlgorithmInputLength);

        return CryptographicOperations.FixedTimeEquals(hashPart, testHash);
    }

    public static string HashPassword(string plain)
    {
        var salt = RandomNumberGenerator.GetBytes(16);

        var hash = Rfc2898DeriveBytes.Pbkdf2(
                       plain,
                       salt,
                       iterations,
                       DBObjects.Constants.HashAlgorithmName,
                       DBObjects.Constants.HashAlgorithmInputLength);

        return $"{iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }
}
