namespace CodexRateLimitTray.Core;

public sealed record UsageDisplayLines(string FiveHour, string Week);

public static class UsageDisplayFormatter
{
    public const string Title = "Codex レート制限";

    public static UsageDisplayLines FormatUsageLines(UsageState state)
    {
        return new UsageDisplayLines(
            $"5時間: 残り{state.FiveHour.RemainingText}% {state.FiveHour.ResetText}",
            $"週: 残り{state.Week.RemainingText}% {state.Week.WeekResetText}");
    }
}
