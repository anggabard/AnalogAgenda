using Database.Helpers;

namespace AnalogAgenda.Server.Tests.Database.Helpers;

public class DateFormattingHelperTests
{
    [Fact]
    public void FormatExposureDateRange_NoDates_UsesFallbackDate()
    {
        // Arrange
        var fallbackDate = new DateOnly(2025, 11, 13);
        var dates = new List<DateOnly>();

        // Act
        var result = DateFormattingHelper.FormatExposureDateRange(dates, fallbackDate);

        // Assert
        Assert.Equal("13 NOV 2025", result);
    }

    [Fact]
    public void FormatExposureDateRange_NoDates_NoFallback_ReturnsEmpty()
    {
        // Arrange
        var dates = new List<DateOnly>();

        // Act
        var result = DateFormattingHelper.FormatExposureDateRange(dates, null);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void FormatExposureDateRange_SingleDate_ReturnsFormattedDate()
    {
        // Arrange
        var dates = new List<DateOnly> { new DateOnly(2025, 11, 13) };

        // Act
        var result = DateFormattingHelper.FormatExposureDateRange(dates, null);

        // Assert
        Assert.Equal("13 NOV 2025", result);
    }

    [Fact]
    public void FormatExposureDateRange_AllSameDay_ReturnsFormattedDate()
    {
        // Arrange
        var dates = new List<DateOnly>
        {
            new DateOnly(2025, 11, 13),
            new DateOnly(2025, 11, 13),
            new DateOnly(2025, 11, 13),
        };

        // Act
        var result = DateFormattingHelper.FormatExposureDateRange(dates, null);

        // Assert
        Assert.Equal("13 NOV 2025", result);
    }

    [Fact]
    public void FormatExposureDateRange_SameMonth_ReturnsMonthYear()
    {
        // Arrange
        var dates = new List<DateOnly>
        {
            new DateOnly(2025, 11, 13),
            new DateOnly(2025, 11, 15),
            new DateOnly(2025, 11, 20),
        };

        // Act
        var result = DateFormattingHelper.FormatExposureDateRange(dates, null);

        // Assert
        Assert.Equal("NOV 2025", result);
    }

    [Fact]
    public void FormatExposureDateRange_ConsecutiveMonths_ReturnsMonthRange()
    {
        // Arrange
        var dates = new List<DateOnly>
        {
            new DateOnly(2025, 10, 15),
            new DateOnly(2025, 11, 10),
            new DateOnly(2025, 12, 5),
        };

        // Act
        var result = DateFormattingHelper.FormatExposureDateRange(dates, null);

        // Assert
        Assert.Equal("OCT-DEC 2025", result);
    }

    [Fact]
    public void FormatExposureDateRange_NonConsecutiveMonths_ReturnsYear()
    {
        // Arrange
        var dates = new List<DateOnly>
        {
            new DateOnly(2025, 10, 15),
            new DateOnly(2025, 12, 5), // Missing November
        };

        // Act
        var result = DateFormattingHelper.FormatExposureDateRange(dates, null);

        // Assert
        Assert.Equal("2025", result);
    }

    [Fact]
    public void FormatExposureDateRange_ConsecutiveYears_ReturnsYearRange()
    {
        // Arrange
        var dates = new List<DateOnly>
        {
            new DateOnly(2020, 1, 15),
            new DateOnly(2021, 6, 10),
            new DateOnly(2022, 12, 5),
        };

        // Act
        var result = DateFormattingHelper.FormatExposureDateRange(dates, null);

        // Assert
        Assert.Equal("2020-2022", result);
    }

    [Fact]
    public void FormatExposureDateRange_NonConsecutiveYears_ReturnsYearRange()
    {
        // Arrange
        var dates = new List<DateOnly>
        {
            new DateOnly(2020, 1, 15),
            new DateOnly(2022, 6, 10),
            new DateOnly(2025, 12, 5), // Missing 2021, 2023, 2024
        };

        // Act
        var result = DateFormattingHelper.FormatExposureDateRange(dates, null);

        // Assert
        Assert.Equal("2020-2025", result);
    }

    [Fact]
    public void FormatExposureDateRange_UnsortedDates_SortsCorrectly()
    {
        // Arrange
        var dates = new List<DateOnly>
        {
            new DateOnly(2025, 12, 5),
            new DateOnly(2025, 10, 15),
            new DateOnly(2025, 11, 10),
        };

        // Act
        var result = DateFormattingHelper.FormatExposureDateRange(dates, null);

        // Assert
        Assert.Equal("OCT-DEC 2025", result);
    }

    [Fact]
    public void FormatExposureDateRange_SingleMonthMultipleDays_ReturnsMonthYear()
    {
        // Arrange
        var dates = new List<DateOnly>
        {
            new DateOnly(2025, 11, 1),
            new DateOnly(2025, 11, 15),
            new DateOnly(2025, 11, 30),
        };

        // Act
        var result = DateFormattingHelper.FormatExposureDateRange(dates, null);

        // Assert
        Assert.Equal("NOV 2025", result);
    }
}

