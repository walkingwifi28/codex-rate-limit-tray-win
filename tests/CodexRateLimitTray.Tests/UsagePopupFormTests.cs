using CodexRateLimitTray;
using System.Windows.Forms;

namespace CodexRateLimitTray.Tests;

public sealed class UsagePopupFormTests
{
    [Fact]
    public void Popup_removes_standard_title_bar()
    {
        using var form = new UsagePopupForm();

        Assert.Equal(FormBorderStyle.None, form.FormBorderStyle);
    }
}
