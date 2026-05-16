using CodexRateLimitTray.Core;
using Microsoft.Win32;

namespace CodexRateLimitTray;

internal static class WindowsThemeReader
{
    private const string PersonalizeKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AppsUseLightThemeValueName = "AppsUseLightTheme";

    public static IconTheme GetIconTheme()
    {
        using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKeyPath);
        return key?.GetValue(AppsUseLightThemeValueName) is int appsUseLightTheme && appsUseLightTheme == 0
            ? IconTheme.Dark
            : IconTheme.Light;
    }
}
