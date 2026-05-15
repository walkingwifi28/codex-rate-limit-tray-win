using CodexRateLimitTray.Core;

namespace CodexRateLimitTray.Tests;

public sealed class RefreshScheduleTests
{
    [Fact]
    public void Automatic_refresh_interval_is_thirty_seconds()
    {
        Assert.Equal(TimeSpan.FromSeconds(30), RefreshSchedule.AutomaticRefreshInterval);
    }
}
