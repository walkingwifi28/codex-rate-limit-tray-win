namespace CodexRateLimitTray.Core;

public static class RefreshSchedule
{
    public static TimeSpan AutomaticRefreshInterval => TimeSpan.FromSeconds(30);

    public static TimeSpan PopupVisibleRefreshInterval => TimeSpan.FromSeconds(5);

    public static TimeSpan ForPopupVisibility(bool isPopupVisible)
    {
        return isPopupVisible ? PopupVisibleRefreshInterval : AutomaticRefreshInterval;
    }
}
