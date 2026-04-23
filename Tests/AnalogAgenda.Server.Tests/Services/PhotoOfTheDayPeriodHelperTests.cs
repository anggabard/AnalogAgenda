using Database.Helpers;

namespace AnalogAgenda.Server.Tests.Services;

public class PhotoOfTheDayPeriodHelperTests
{
    [Fact]
    public void GetUtcHalfDayIndex_SameUtc12hWindow_ReturnsSameIndex()
    {
        var early = new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc);
        var late = new DateTime(2024, 6, 15, 11, 59, 59, DateTimeKind.Utc);

        Assert.Equal(
            PhotoOfTheDayPeriodHelper.GetUtcHalfDayIndex(early),
            PhotoOfTheDayPeriodHelper.GetUtcHalfDayIndex(late));
    }

    [Fact]
    public void GetUtcHalfDayIndex_AcrossUtcNoonBoundary_Changes()
    {
        var beforeNoon = new DateTime(2024, 6, 15, 11, 59, 0, DateTimeKind.Utc);
        var noon = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        Assert.NotEqual(
            PhotoOfTheDayPeriodHelper.GetUtcHalfDayIndex(beforeNoon),
            PhotoOfTheDayPeriodHelper.GetUtcHalfDayIndex(noon));
    }

    [Fact]
    public void GetDeterministicIndex_SameInputs_ReturnsSameValue()
    {
        const long bucket = 902_831;
        const int count = 7;

        Assert.Equal(
            PhotoOfTheDayPeriodHelper.GetDeterministicIndex(bucket, count),
            PhotoOfTheDayPeriodHelper.GetDeterministicIndex(bucket, count));
    }

    [Fact]
    public void GetDeterministicIndex_IsInRange()
    {
        const long bucket = 12_345;
        const int count = 100;

        var index = PhotoOfTheDayPeriodHelper.GetDeterministicIndex(bucket, count);
        Assert.InRange(index, 0, count - 1);
    }

    [Fact]
    public void GetDeterministicIndex_CountOne_ReturnsZero()
    {
        Assert.Equal(0, PhotoOfTheDayPeriodHelper.GetDeterministicIndex(999, 1));
    }

    /// <summary>
    /// Golden value for the current SHA-256 preimage (algorithm UTF-8 + LE half-day + LE genesis ToBinary).
    /// If this fails after an intentional change, update the expected value and document the migration.
    /// </summary>
    [Fact]
    public void GetDeterministicIndex_Golden902831Count7_StaysStable()
    {
        const int expected = 1;
        Assert.Equal(expected, PhotoOfTheDayPeriodHelper.GetDeterministicIndex(902_831, 7));
    }
}
