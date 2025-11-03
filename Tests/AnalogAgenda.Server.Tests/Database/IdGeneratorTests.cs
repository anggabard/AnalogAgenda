using Database.Helpers;

namespace AnalogAgenda.Server.Tests.Database;

public class IdGeneratorTests
{
    [Fact]
    public void Generate_ShouldReturnStringOfCorrectLength()
    {
        var result = IdGenerator.Get(16, "Test", "Input");
        Assert.Equal(16, result.Length);
    }

    [Fact]
    public void Generate_ShouldBeDeterministic()
    {
        var result1 = IdGenerator.Get(32, "abc", "123");
        var result2 = IdGenerator.Get(32, "abc", "123");

        Assert.Equal(result1, result2);
    }

    [Fact]
    public void Generate_ShouldReturnDifferentStrings_ForDifferentInputs()
    {
        var result1 = IdGenerator.Get(32, "abc", "123");
        var result2 = IdGenerator.Get(32, "abc", "124");

        Assert.NotEqual(result1, result2);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(8)]
    [InlineData(32)]
    [InlineData(64)]
    [InlineData(99)]
    public void Generate_ShouldHandleVariousValidLengths(int length)
    {
        var result = IdGenerator.Get(length, "some", "fixed", "input");
        Assert.Equal(length, result.Length);
    }

    [Fact]
    public void Generate_ShouldThrowException_WhenLengthIsZero()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            IdGenerator.Get(0, "invalid"));
    }

    [Fact]
    public void Generate_ShouldThrowException_WhenLengthIsTooLarge()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            IdGenerator.Get(100, "invalid"));
    }

    [Fact]
    public void Generate_ShouldUseAlphanumericCharactersOnly()
    {
        var result = IdGenerator.Get(99, "Ceapa", "AnalogAgenda");
        Assert.All(result.ToCharArray(), c =>
            Assert.True(char.IsLetterOrDigit(c), $"Character '{c}' is not alphanumeric."));
    }

}

