using System.Security.Cryptography;
using System.Text;

namespace Database.Helpers;

public static class IdGenerator
{
    private static readonly char[] AlphanumericChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();
    public static string Get(int length = 8, params string[] inputs)
    {
        if (length <= 0 || length >= 100)
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be between 1 and 99.");

        string combinedInput = string.Join("|", inputs);
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(combinedInput));

        var result = new StringBuilder();
        int counter = 0;

        while (result.Length < length)
        {
            byte[] extendedInput = hash.Concat(BitConverter.GetBytes(counter)).ToArray();
            byte[] extendedHash = SHA256.HashData(extendedInput);

            foreach (byte b in extendedHash)
            {
                char c = AlphanumericChars[b % AlphanumericChars.Length];
                result.Append(c);

                if (result.Length == length)
                    break;
            }

            counter++;
        }

        return result.ToString();
    }
}
