using System.Globalization;

namespace CodexRateLimitTray.Core;

public sealed record UsageDisplayLines(string FiveHour, string Week);

public static class UsageDisplayFormatter
{
    public const string Title = "Codex レート制限";
    private const int LabelWidth = 4;
    private const int RemainingWidth = 3;
    private const int ResetWidth = 11;

    public static UsageDisplayLines FormatUsageLines(UsageState state)
    {
        var fiveHour = FormatLine("5時間", state.FiveHour.RemainingText, state.FiveHour.ResetText);
        var week = state.Week is null
            ? string.Empty
            : FormatLine("週", state.Week.RemainingText, state.Week.WeekResetText);

        return new UsageDisplayLines(fiveHour, week);
    }

    public static string FormatTooltipText(UsageState state)
    {
        var weekRemaining = state.Week is null
            ? "-"
            : $"{state.Week.RemainingText}%";
        return string.Format(
            CultureInfo.InvariantCulture,
            "Codexレート制限 : {0}% / {1}",
            state.FiveHour.RemainingText,
            weekRemaining);
    }

    private static string FormatLine(string label, string remainingText, string resetText)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "{0,-" + LabelWidth + "}: 残り{1," + RemainingWidth + "}% {2," + ResetWidth + "}",
            label,
            remainingText,
            resetText);
    }
}
