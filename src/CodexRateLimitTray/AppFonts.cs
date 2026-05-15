using CodexRateLimitTray.Core;

namespace CodexRateLimitTray;

internal static class AppFonts
{
    public static Font Create(float size, FontStyle style = FontStyle.Regular)
    {
        try
        {
            return new Font(AppTypography.FontFamilyName, size, style, GraphicsUnit.Point);
        }
        catch (ArgumentException)
        {
            return new Font(FontFamily.GenericSansSerif, size, style, GraphicsUnit.Point);
        }
    }
}
