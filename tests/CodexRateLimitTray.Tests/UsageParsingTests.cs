using CodexRateLimitTray.Core;

namespace CodexRateLimitTray.Tests;

public sealed class UsageParsingTests
{
    [Fact]
    public void Parses_primary_as_five_hour_and_secondary_as_week_window()
    {
        const string json = """
        {
          "rate_limit": {
            "primary_window": { "used_percent": 25.5, "reset_at": 1715781600 },
            "secondary_window": { "used_percent": 80, "reset_at": 1716094800 }
          }
        }
        """;

        var state = WhamUsageParser.Parse(json, TimeZoneInfo.Utc);

        Assert.False(state.HasError);
        Assert.Equal(25.5, state.FiveHour.UsedPercent);
        Assert.Equal(74.5, state.FiveHour.RemainingPercent);
        Assert.Equal(80, state.Week.UsedPercent);
        Assert.Equal(20, state.Week.RemainingPercent);
        Assert.Equal(new DateTimeOffset(2024, 5, 15, 14, 0, 0, TimeSpan.Zero), state.FiveHour.ResetAt);
        Assert.Equal(new DateTimeOffset(2024, 5, 19, 5, 0, 0, TimeSpan.Zero), state.Week.ResetAt);
    }

    [Fact]
    public void Formats_reset_times_with_local_time_patterns()
    {
        var tokyo = TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time");
        var fiveHour = new UsageWindow(10, TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeSeconds(1715781600), tokyo));
        var week = new UsageWindow(65, TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeSeconds(1716094800), tokyo));
        var state = UsageState.Success(fiveHour, week);

        Assert.Equal("23:00", state.FiveHour.ResetText);
        Assert.Equal("05/19 14:00", state.Week.WeekResetText);
    }

    [Theory]
    [InlineData(-5, 100)]
    [InlineData(0, 100)]
    [InlineData(42.4, 57.6)]
    [InlineData(120, 0)]
    public void Remaining_percent_is_clamped_to_zero_through_one_hundred(double used, double expectedRemaining)
    {
        var window = new UsageWindow(used, DateTimeOffset.UnixEpoch);

        Assert.Equal(expectedRemaining, window.RemainingPercent, precision: 6);
    }
}
