using Database.Helpers;

namespace AnalogAgenda.Server.Tests;

public sealed class HomeSectionOrderDefaultsTests
{
    private static readonly string[] Valid =
        ["filmCheck", "currentFilm", "settings", "wackyIdeas", "photoOfTheDay"];

    [Fact]
    public void IsValidOrder_AcceptsPermutedOrder()
    {
        var permuted = new[] { "photoOfTheDay", "wackyIdeas", "settings", "currentFilm", "filmCheck" };
        Assert.True(HomeSectionOrderDefaults.IsValidOrder(permuted));
    }

    [Fact]
    public void IsValidOrder_RejectsWrongLength()
    {
        Assert.False(HomeSectionOrderDefaults.IsValidOrder(Valid[..4]));
    }

    [Fact]
    public void IsValidOrder_RejectsDuplicate()
    {
        var dup = new[] { "filmCheck", "filmCheck", "settings", "wackyIdeas", "photoOfTheDay" };
        Assert.False(HomeSectionOrderDefaults.IsValidOrder(dup));
    }

    [Fact]
    public void IsValidOrder_RejectsUnknownId()
    {
        var bad = new[] { "filmCheck", "currentFilm", "settings", "wackyIdeas", "unknown" };
        Assert.False(HomeSectionOrderDefaults.IsValidOrder(bad));
    }

    [Fact]
    public void IsValidOrder_NullIsInvalid()
    {
        Assert.False(HomeSectionOrderDefaults.IsValidOrder(null));
    }
}
