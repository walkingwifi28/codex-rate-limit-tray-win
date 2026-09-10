using CodexRateLimitTray.Core;
using System.Globalization;

namespace CodexRateLimitTray.Tests;

public sealed class UsageDisplayFormatterTests
{
    [Fact]
    public void Title_is_codex_rate_limit()
    {
        Assert.Equal("Codex レート制限", UsageDisplayFormatter.Title);
    }

    [Fact]
    public void Formats_five_hour_and_week_lines_with_invariant_columns()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");

            var fiveHourReset = TimeZoneInfo.ConvertTime(
                DateTimeOffset.FromUnixTimeSeconds(1715781600),
                TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time"));
            var weekReset = TimeZoneInfo.ConvertTime(
                DateTimeOffset.FromUnixTimeSeconds(1716094800),
                TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time"));
        var state = UsageState.Success(
                new UsageWindow(25, fiveHourReset),
                new UsageWindow(80, weekReset));

            var lines = UsageDisplayFormatter.FormatUsageLines(state);

            Assert.Equal("5時間 : 残り 75%       23:00", lines.FiveHour);
            Assert.Equal("週   : 残り 20% 05/19 14:00", lines.Week);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void Pads_remaining_percent_to_three_digits()
    {
        var state = UsageState.Success(
            new UsageWindow(0, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)),
            new UsageWindow(100, new DateTimeOffset(2026, 5, 19, 14, 0, 0, TimeSpan.Zero)));

        var lines = UsageDisplayFormatter.FormatUsageLines(state);

        Assert.Equal("5時間 : 残り100%       00:00", lines.FiveHour);
        Assert.Equal("週   : 残り  0% 05/19 14:00", lines.Week);
    }

    [Fact]
    public void Formats_tooltip_text_with_both_rate_limits()
    {
        var state = UsageState.Success(
            new UsageWindow(25, new DateTimeOffset(2026, 5, 17, 23, 0, 0, TimeSpan.Zero)),
            new UsageWindow(80, new DateTimeOffset(2026, 5, 19, 14, 0, 0, TimeSpan.Zero)));

        var text = UsageDisplayFormatter.FormatTooltipText(state);

        Assert.Equal("Codexレート制限 : 75% / 20%", text);
    }

    [Fact]
    public void Leaves_week_line_empty_and_uses_dash_in_tooltip_when_week_is_missing()
    {
        var state = UsageState.Success(
            new UsageWindow(25, new DateTimeOffset(2026, 5, 17, 23, 0, 0, TimeSpan.Zero)),
            null);

        var lines = UsageDisplayFormatter.FormatUsageLines(state);

        Assert.Equal("5時間 : 残り 75%       23:00", lines.FiveHour);
        Assert.Equal(string.Empty, lines.Week);
        Assert.Equal("Codexレート制限 : 75% / -", UsageDisplayFormatter.FormatTooltipText(state));
    }
}
