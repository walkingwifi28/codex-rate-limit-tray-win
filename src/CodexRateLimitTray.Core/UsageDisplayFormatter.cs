namespace CodexRateLimitTray.Core;

public sealed record UsageDisplayLines(string Week);

public static class UsageDisplayFormatter
{
    public const string Title = "Codex レート制限";
    private const int LabelWidth = 4;
    private const int RemainingWidth = 3;
    private const int ResetWidth = 11;

    public static UsageDisplayLines FormatUsageLines(UsageState state)
    {
        return new UsageDisplayLines(FormatLine("週", state.Week.RemainingText, state.Week.WeekResetText));
    }

    public static string FormatTooltipText(UsageState state)
    {
        return $"Codexレート制限 : {state.Week.RemainingText}%";
    }

    private static string FormatLine(string label, string remainingText, string resetText)
    {
        return $"{label,-LabelWidth}: 残り{remainingText,RemainingWidth}% {resetText,ResetWidth}";
    }
}
