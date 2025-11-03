using Database.Helpers;

namespace AnalogAgenda.Server.Tests.Database;

public class PasswordHasherTests
{
    [Fact]
    public void HashPassword_Returns_Three_DotSeparated_Parts_in_Correct_Format()
    {
        // arrange
        const string pw = "P@ssw0rd!";

        // act
        var hashed = PasswordHasher.HashPassword(pw);

        // assert
        Assert.False(string.IsNullOrWhiteSpace(hashed));

        var parts = hashed.Split('.', 3);
        Assert.Equal(3, parts.Length);

        // part[0]  → iterations
        Assert.True(int.TryParse(parts[0], out var iters));
        Assert.True(iters >= 100_000);      // our default

        // part[1] & part[2] → valid Base-64
        Assert.NotNull(Convert.FromBase64String(parts[1]));
        Assert.NotNull(Convert.FromBase64String(parts[2]));
    }

    [Fact]
    public void HashPassword_Same_Plaintext_Produces_Different_Strings_Due_To_Salt()
    {
        var pw = "same-pw";
        var h1 = PasswordHasher.HashPassword(pw);
        var h2 = PasswordHasher.HashPassword(pw);

        Assert.NotEqual(h1, h2);
    }

    [Fact]
    public void VerifyPassword_With_Correct_Password_Returns_True()
    {
        const string pw = "CorrectHorseBatteryStaple";
        var stored = PasswordHasher.HashPassword(pw);

        Assert.True(PasswordHasher.VerifyPassword(pw, stored));
    }

    [Fact]
    public void VerifyPassword_With_Wrong_Password_Returns_False()
    {
        var stored = PasswordHasher.HashPassword("secret");

        Assert.False(PasswordHasher.VerifyPassword("WRONG-pw", stored));
    }

    [Fact]
    public void VerifyPassword_With_Too_Few_Parts_Returns_False()
    {
        Assert.False(PasswordHasher.VerifyPassword("x", "onlyOnePart"));
    }

    [Fact]
    public void VerifyPassword_With_Invalid_Base64_Throws_FormatException()
    {
        // bad base-64 in both salt & hash parts
        var malformed = "10000.not-base64.not-base64";

        Assert.Throws<FormatException>(
            () => PasswordHasher.VerifyPassword("x", malformed));
    }
}

