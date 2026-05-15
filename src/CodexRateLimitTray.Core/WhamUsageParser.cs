using System.Text.Json;

namespace CodexRateLimitTray.Core;

public static class WhamUsageParser
{
    public static UsageState Parse(string json, TimeZoneInfo localTimeZone)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var rateLimit = document.RootElement.GetProperty("rate_limit");
            var primary = ReadWindow(rateLimit.GetProperty("primary_window"), localTimeZone);
            var secondary = ReadWindow(rateLimit.GetProperty("secondary_window"), localTimeZone);

            return UsageState.Success(primary, secondary);
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            return UsageState.Error(UsageErrorKind.InvalidResponse, "レスポンスが不正です");
        }
    }

    private static UsageWindow ReadWindow(JsonElement element, TimeZoneInfo localTimeZone)
    {
        var usedPercent = element.GetProperty("used_percent").GetDouble();
        var resetUnix = element.GetProperty("reset_at").GetInt64();
        var resetAt = TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeSeconds(resetUnix), localTimeZone);
        return new UsageWindow(usedPercent, resetAt);
    }
}
