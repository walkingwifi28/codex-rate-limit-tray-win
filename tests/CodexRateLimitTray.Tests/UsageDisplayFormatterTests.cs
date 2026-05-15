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
    public void Formats_window_lines_with_remaining_label_and_colon()
    {
        var state = UsageState.Success(
            new UsageWindow(25, new DateTimeOffset(2026, 5, 16, 9, 5, 0, TimeSpan.Zero)),
            new UsageWindow(80, new DateTimeOffset(2026, 5, 18, 13, 45, 0, TimeSpan.Zero)));

        var lines = UsageDisplayFormatter.FormatUsageLines(state);

        Assert.Equal("5時間: 残り75% 09:05", lines.FiveHour);
        Assert.Equal("週: 残り20% 05/18 13:45", lines.Week);
    }
}
