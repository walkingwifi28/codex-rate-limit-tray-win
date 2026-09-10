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
            var fiveHour = ReadWindow(rateLimit.GetProperty("primary_window"), localTimeZone);
            UsageWindow? week = null;

            if (rateLimit.TryGetProperty("secondary_window", out var secondaryWindow) &&
                secondaryWindow.ValueKind != JsonValueKind.Null)
            {
                week = ReadWindow(secondaryWindow, localTimeZone);
            }

            return UsageState.Success(fiveHour, week);
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException or FormatException or OverflowException or ArgumentOutOfRangeException)
        {
            return UsageState.Error(UsageErrorKind.InvalidResponse, "レスポンスが不正です");
        }
    }

    private static UsageWindow ReadWindow(JsonElement element, TimeZoneInfo localTimeZone)
    {
        var usedPercent = element.GetProperty("used_percent").GetDouble();
        if (!double.IsFinite(usedPercent))
        {
            throw new FormatException("used_percent must be finite");
        }

        var resetUnix = element.GetProperty("reset_at").GetInt64();
        var resetAt = TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeSeconds(resetUnix), localTimeZone);
        return new UsageWindow(usedPercent, resetAt);
    }
}
