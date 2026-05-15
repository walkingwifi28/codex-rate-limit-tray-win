using System.Globalization;

namespace CodexRateLimitTray.Core;

public enum UsageErrorKind
{
    None,
    Authentication,
    Network,
    Server,
    InvalidResponse,
    AuthFile
}

public sealed record UsageWindow(double UsedPercent, DateTimeOffset ResetAt)
{
    public double RemainingPercent => Math.Clamp(100d - Math.Clamp(UsedPercent, 0d, 100d), 0d, 100d);

    public string RemainingText => Math.Round(RemainingPercent, MidpointRounding.AwayFromZero).ToString("0", CultureInfo.InvariantCulture);

    public string ResetText => ResetAt.ToString("HH:mm", CultureInfo.InvariantCulture);

    public string WeekResetText => ResetAt.ToString("MM/dd HH:mm", CultureInfo.InvariantCulture);
}

public sealed record UsageState(
    UsageWindow FiveHour,
    UsageWindow Week,
    UsageErrorKind ErrorKind,
    string? ErrorMessage)
{
    public bool HasError => ErrorKind != UsageErrorKind.None;

    public static UsageState Success(UsageWindow fiveHour, UsageWindow week)
    {
        return new UsageState(fiveHour, week, UsageErrorKind.None, null);
    }

    public static UsageState Error(UsageErrorKind kind, string message)
    {
        return new UsageState(new UsageWindow(0, DateTimeOffset.Now), new UsageWindow(0, DateTimeOffset.Now), kind, message);
    }
}
