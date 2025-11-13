namespace Database.Helpers;

public static class DateFormattingHelper
{
    private static readonly string[] MonthAbbreviations = 
    {
        "JAN", "FEB", "MAR", "APR", "MAY", "JUN",
        "JUL", "AUG", "SEP", "OCT", "NOV", "DEC"
    };

    /// <summary>
    /// Formats a list of exposure dates into a display string based on the date range rules.
    /// </summary>
    /// <param name="dates">List of exposure dates (will be sorted internally)</param>
    /// <param name="fallbackDate">Fallback date to use if dates list is empty (typically purchase date)</param>
    /// <returns>Formatted date string (e.g., "13 NOV 2025", "NOV 2025", "OCT-DEC 2025", "2025", "2020-2025")</returns>
    public static string FormatExposureDateRange(List<DateOnly> dates, DateOnly? fallbackDate)
    {
        // If no dates provided, use fallback date
        if (dates == null || dates.Count == 0)
        {
            if (fallbackDate.HasValue)
            {
                return FormatSingleDate(fallbackDate.Value);
            }
            return string.Empty;
        }

        // Sort dates to ensure consistent ordering
        var sortedDates = dates.OrderBy(d => d).ToList();

        // Check if all dates are the same day
        if (sortedDates.All(d => d == sortedDates[0]))
        {
            return FormatSingleDate(sortedDates[0]);
        }

        // Check if all dates are in the same month and year
        var firstDate = sortedDates[0];
        if (sortedDates.All(d => d.Year == firstDate.Year && d.Month == firstDate.Month))
        {
            return FormatMonthYear(firstDate);
        }

        // Check if all dates are in the same year
        if (sortedDates.All(d => d.Year == firstDate.Year))
        {
            // Check if months are consecutive
            var months = sortedDates.Select(d => d.Month).Distinct().OrderBy(m => m).ToList();
            if (AreMonthsConsecutive(months))
            {
                var firstMonth = months[0];
                var lastMonth = months[^1];
                return $"{MonthAbbreviations[firstMonth - 1]}-{MonthAbbreviations[lastMonth - 1]} {firstDate.Year}";
            }
            else
            {
                // Non-consecutive months, same year - just return year
                return firstDate.Year.ToString();
            }
        }

        // Multiple years - check if consecutive
        var years = sortedDates.Select(d => d.Year).Distinct().OrderBy(y => y).ToList();
        if (AreYearsConsecutive(years))
        {
            var firstYear = years[0];
            var lastYear = years[years.Count - 1];
            return $"{firstYear}-{lastYear}";
        }
        else
        {
            // Non-consecutive years - return first and last year
            var firstYear = years[0];
            var lastYear = years[years.Count - 1];
            return $"{firstYear}-{lastYear}";
        }
    }

    private static string FormatSingleDate(DateOnly date)
    {
        return $"{date.Day} {MonthAbbreviations[date.Month - 1]} {date.Year}";
    }

    private static string FormatMonthYear(DateOnly date)
    {
        return $"{MonthAbbreviations[date.Month - 1]} {date.Year}";
    }

    private static bool AreMonthsConsecutive(List<int> months)
    {
        if (months.Count <= 1) return true;

        for (int i = 1; i < months.Count; i++)
        {
            if (months[i] != months[i - 1] + 1)
            {
                return false;
            }
        }
        return true;
    }

    private static bool AreYearsConsecutive(List<int> years)
    {
        if (years.Count <= 1) return true;

        for (int i = 1; i < years.Count; i++)
        {
            if (years[i] != years[i - 1] + 1)
            {
                return false;
            }
        }
        return true;
    }
}

