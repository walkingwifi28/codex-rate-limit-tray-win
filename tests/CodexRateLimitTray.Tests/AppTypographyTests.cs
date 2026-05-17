using CodexRateLimitTray.Core;

namespace CodexRateLimitTray.Tests;

public sealed class AppTypographyTests
{
    [Fact]
    public void Font_family_is_biz_up_gothic()
    {
        Assert.Equal("BIZ UDPGothic", AppTypography.FontFamilyName);
    }

    [Fact]
    public void Aligned_font_family_is_biz_ud_gothic()
    {
        Assert.Equal("BIZ UDGothic", AppTypography.AlignedFontFamilyName);
    }
}
