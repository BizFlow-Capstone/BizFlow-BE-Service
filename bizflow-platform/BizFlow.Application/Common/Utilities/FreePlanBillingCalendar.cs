namespace BizFlow.Application.Common.Utilities
{
    
    public static class FreePlanBillingCalendar
    {
        public static TimeZoneInfo ResolveTimeZone(string? timeZoneId)
        {
            if (string.IsNullOrWhiteSpace(timeZoneId))
                return TimeZoneInfo.Utc;

            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim());
            }
            catch
            {
                return TimeZoneInfo.Utc;
            }
        }

        public static DateTime GetCurrentMonthStartUtc(DateTime utcNow, string? timeZoneId)
        {
            var tz = ResolveTimeZone(timeZoneId);
            var utc = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
            var local = TimeZoneInfo.ConvertTimeFromUtc(utc, tz);
            var monthStartLocal = new DateTime(local.Year, local.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
            return TimeZoneInfo.ConvertTimeToUtc(monthStartLocal, tz);
        }

        public static DateTime GetNextMonthStartUtc(DateTime utcNow, string? timeZoneId)
        {
            var tz = ResolveTimeZone(timeZoneId);
            var utc = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
            var local = TimeZoneInfo.ConvertTimeFromUtc(utc, tz);
            var nextMonthFirstLocal = new DateTime(local.Year, local.Month, 1, 0, 0, 0, DateTimeKind.Unspecified).AddMonths(1);
            return TimeZoneInfo.ConvertTimeToUtc(nextMonthFirstLocal, tz);
        }
    }
}
