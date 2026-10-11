namespace ECommerceStore.Core.Constants;

/// <summary>
/// Time zone helpers for Egypt standard time (UTC+2 / UTC+3 with DST).
/// Linux containers (Render, Docker) use "Africa/Cairo"; Windows uses "Egypt Standard Time".
/// </summary>
public static class EgyptTimeZone
{
    private static readonly TimeZoneInfo TimeZone = ResolveTimeZone();

    private static TimeZoneInfo ResolveTimeZone()
    {
        try
        {
            // Standard IANA identifier on Linux/macOS
            return TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo");
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                // Windows identifier
                return TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                // Fallback fixed +2 / +3 offset if neither is installed
                return TimeZoneInfo.CreateCustomTimeZone("Egypt Standard Time", TimeSpan.FromHours(3), "Egypt Standard Time", "Egypt Standard Time");
            }
        }
    }

    public static DateTime ToEgyptTime(this DateTime utcDateTime)
    {
        var utc = utcDateTime.Kind == DateTimeKind.Utc 
            ? utcDateTime 
            : DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZone);
    }
}
