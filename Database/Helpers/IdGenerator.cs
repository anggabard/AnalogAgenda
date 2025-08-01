namespace Database.Helpers;

public class IdGenerator
{
    private const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    public static string Get(int length = 8, params string[] inputs)
    {
        var random = new Random(GetSeed(inputs));

        return new string(Enumerable.Range(0, length)
            .Select(_ => chars[random.Next(chars.Length)])
            .ToArray());
    }

    private static int GetSeed(string[] inputs)
    {
        var seed = string.Empty;
        foreach (var input in inputs) 
        {
            seed += input;
        }

        return seed.GetHashCode();
    }
}
