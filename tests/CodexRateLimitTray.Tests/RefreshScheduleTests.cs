using CodexRateLimitTray.Core;

namespace CodexRateLimitTray.Tests;

public sealed class RefreshScheduleTests
{
    [Fact]
    public void Automatic_refresh_interval_is_thirty_seconds()
    {
        Assert.Equal(TimeSpan.FromSeconds(30), RefreshSchedule.AutomaticRefreshInterval);
    }

    [Fact]
    public void Popup_visible_refresh_interval_is_five_seconds()
    {
        Assert.Equal(TimeSpan.FromSeconds(5), RefreshSchedule.PopupVisibleRefreshInterval);
    }

    [Theory]
    [InlineData(false, 30)]
    [InlineData(true, 5)]
    public void Selects_refresh_interval_from_popup_visibility(bool isPopupVisible, int expectedSeconds)
    {
        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), RefreshSchedule.ForPopupVisibility(isPopupVisible));
    }
}
