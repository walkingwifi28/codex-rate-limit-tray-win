using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace CodexRateLimitTray.Core;

public sealed record RingGeometry(double OuterDiameter, double InnerDiameter);

public static class RateLimitIconRenderer
{
    public static readonly Color OuterRingColor = ColorTranslator.FromHtml("#299E64");
    public static readonly Color InnerRingColor = ColorTranslator.FromHtml("#92BDA7");
    public static readonly Color BackgroundRingColor = ColorTranslator.FromHtml("#D9D9D9");

    public static RingGeometry CalculateGeometry(double outerDiameter)
    {
        return new RingGeometry(outerDiameter, outerDiameter * 200d / 314d);
    }

    [SupportedOSPlatform("windows")]
    public static Bitmap RenderBitmap(UsageState state, int size)
    {
        var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);

        var outerUsed = state.HasError ? 0 : state.Week.UsedPercent;
        var innerUsed = state.HasError ? 0 : state.FiveHour.UsedPercent;
        DrawRings(graphics, size, outerUsed, innerUsed);
        return bitmap;
    }

    [SupportedOSPlatform("windows")]
    public static Icon RenderIcon(UsageState state, int size = 32)
    {
        using var bitmap = RenderBitmap(state, size);
        var handle = bitmap.GetHicon();
        try
        {
            using var icon = Icon.FromHandle(handle);
            return (Icon)icon.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    [SupportedOSPlatform("windows")]
    private static void DrawRings(Graphics graphics, int canvasSize, double outerUsedPercent, double innerUsedPercent)
    {
        var geometry = CalculateGeometry(canvasSize);

        var outerRect = CenteredRect(canvasSize, (float)geometry.OuterDiameter);
        var innerRect = CenteredRect(canvasSize, (float)geometry.InnerDiameter);

        DrawPieDisc(graphics, outerRect, OuterRingColor, outerUsedPercent);
        DrawPieDisc(graphics, innerRect, InnerRingColor, innerUsedPercent);
    }

    [SupportedOSPlatform("windows")]
    private static void DrawPieDisc(Graphics graphics, RectangleF rect, Color usedColor, double usedPercent)
    {
        using var backgroundBrush = new SolidBrush(BackgroundRingColor);
        using var usedBrush = new SolidBrush(usedColor);

        graphics.FillEllipse(backgroundBrush, rect);

        var sweep = (float)(Math.Clamp(usedPercent, 0d, 100d) / 100d * 360d);
        if (sweep > 0)
        {
            graphics.FillPie(usedBrush, rect, -90, sweep);
        }
    }

    private static RectangleF CenteredRect(int canvasSize, float diameter)
    {
        var offset = (canvasSize - diameter) / 2f;
        return new RectangleF(offset, offset, diameter, diameter);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
