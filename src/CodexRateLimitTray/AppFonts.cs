using CodexRateLimitTray.Core;

namespace CodexRateLimitTray;

internal static class AppFonts
{
    public static Font Create(float size, FontStyle style = FontStyle.Regular)
    {
        return Create(AppTypography.FontFamilyName, size, style);
    }

    public static Font CreateAligned(float size, FontStyle style = FontStyle.Regular)
    {
        return Create(AppTypography.AlignedFontFamilyName, size, style);
    }

    private static Font Create(string familyName, float size, FontStyle style)
    {
        try
        {
            return new Font(familyName, size, style, GraphicsUnit.Point);
        }
        catch (ArgumentException)
        {
            return new Font(FontFamily.GenericSansSerif, size, style, GraphicsUnit.Point);
        }
    }
}
