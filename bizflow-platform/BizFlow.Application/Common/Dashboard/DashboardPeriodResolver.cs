using BizFlow.Domain.Enums;

namespace BizFlow.Application.Common.Dashboard;

public static class DashboardPeriodResolver
{
    /// <summary>
    /// Calendar presets use <paramref name="referenceDate"/> (e.g. UTC today when query omits referenceDate):
    /// day = that calendar day; week = Mon–Sun week containing it; month = 1st–last of that month; year = Jan 1–Dec 31 of that year.
    /// <see cref="DashboardPeriod.Custom"/> uses <paramref name="customFrom"/> / <paramref name="customTo"/> only.
    /// </summary>
    public static (DateOnly From, DateOnly To) Resolve(
        string period,
        DateOnly referenceDate,
        DateOnly? customFrom,
        DateOnly? customTo)
    {
        return period switch
        {
            DashboardPeriod.Day => (referenceDate, referenceDate),
            DashboardPeriod.Week => GetWeekRangeMondayToSunday(referenceDate),
            DashboardPeriod.Month => GetCalendarMonthRange(referenceDate),
            DashboardPeriod.Year => GetCalendarYearRange(referenceDate),
            DashboardPeriod.Custom => (customFrom!.Value, customTo!.Value),
            _ => throw new ArgumentOutOfRangeException(nameof(period))
        };
    }

    public static bool TryParsePeriod(string? raw, out string period)
    {
        var normalized = DashboardPeriod.NormalizeOrNull(raw);
        if (normalized == null)
        {
            period = string.Empty;
            return false;
        }

        period = normalized;
        return true;
    }

    /// <summary>Monday–Sunday week containing <paramref name="date"/>.</summary>
    private static (DateOnly From, DateOnly To) GetWeekRangeMondayToSunday(DateOnly date)
    {
        var dow = date.DayOfWeek;
        var offsetFromMonday = ((int)dow + 6) % 7;
        var monday = date.AddDays(-offsetFromMonday);
        var sunday = monday.AddDays(6);
        return (monday, sunday);
    }

    private static (DateOnly From, DateOnly To) GetCalendarMonthRange(DateOnly date)
    {
        var first = new DateOnly(date.Year, date.Month, 1);
        var last = first.AddMonths(1).AddDays(-1);
        return (first, last);
    }

    private static (DateOnly From, DateOnly To) GetCalendarYearRange(DateOnly date)
    {
        var first = new DateOnly(date.Year, 1, 1);
        var last = new DateOnly(date.Year, 12, 31);
        return (first, last);
    }

    /// <summary>Inclusive UTC range for filtering <see cref="DateTime"/> (e.g. CompletedAt).</summary>
    public static (DateTime FromUtc, DateTime ToUtc) ToUtcInclusiveDateTimeRange(DateOnly from, DateOnly to)
    {
        var fromUtc = DateTime.SpecifyKind(from.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var toUtc = DateTime.SpecifyKind(to.ToDateTime(TimeOnly.MaxValue), DateTimeKind.Utc);
        return (fromUtc, toUtc);
    }
}
