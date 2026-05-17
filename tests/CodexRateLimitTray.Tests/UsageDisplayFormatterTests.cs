using CodexRateLimitTray.Core;

namespace CodexRateLimitTray.Tests;

public sealed class UsageDisplayFormatterTests
{
    [Fact]
    public void Title_is_codex_rate_limit()
    {
        Assert.Equal("Codex レート制限", UsageDisplayFormatter.Title);
    }

    [Fact]
    public void Formats_window_lines_with_aligned_columns()
    {
        var state = UsageState.Success(
            new UsageWindow(25, new DateTimeOffset(2026, 5, 16, 9, 5, 0, TimeSpan.Zero)),
            new UsageWindow(80, new DateTimeOffset(2026, 5, 18, 13, 45, 0, TimeSpan.Zero)));

        var lines = UsageDisplayFormatter.FormatUsageLines(state);

        Assert.Equal("5時間 : 残り 75%       09:05", lines.FiveHour);
        Assert.Equal("週   : 残り 20% 05/18 13:45", lines.Week);
    }

    [Fact]
    public void Pads_remaining_percent_to_three_digits()
    {
        var state = UsageState.Success(
            new UsageWindow(1, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)),
            new UsageWindow(0, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)));

        var lines = UsageDisplayFormatter.FormatUsageLines(state);

        Assert.Equal("5時間 : 残り 99%       00:00", lines.FiveHour);
        Assert.Equal("週   : 残り100% 01/01 00:00", lines.Week);
    }

    [Fact]
    public void Formats_tooltip_text_as_single_rate_limit_summary()
    {
        var state = UsageState.Success(
            new UsageWindow(20, new DateTimeOffset(2026, 5, 17, 18, 48, 0, TimeSpan.Zero)),
            new UsageWindow(3, new DateTimeOffset(2026, 5, 24, 13, 48, 0, TimeSpan.Zero)));

        var text = UsageDisplayFormatter.FormatTooltipText(state);

        Assert.Equal("Codexレート制限 : 80% / 97%", text);
    }
}
