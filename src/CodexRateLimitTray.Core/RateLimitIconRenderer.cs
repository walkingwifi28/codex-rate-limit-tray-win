using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace CodexRateLimitTray.Core;

public sealed record RingGeometry(double OuterDiameter, double InnerDiameter);
public sealed record IconPalette(Color BackgroundColor, Color TextColor, Color InnerRingColor);

public enum IconTheme
{
    Light,
    Dark,
}

public static class RateLimitIconRenderer
{
    private const int SupersamplingScale = 4;

    public static readonly Color OuterRingColor = ColorTranslator.FromHtml("#339CFF");
    public static readonly Color WeekProgressNeedleColor = ColorTranslator.FromHtml("#FF0000");
    public static readonly Color GraphBackgroundColor = ColorTranslator.FromHtml("#D9D9D9");
    public static readonly IconPalette LightTheme = new(
        ColorTranslator.FromHtml("#FFFFFF"),
        ColorTranslator.FromHtml("#1A1C1F"),
        ColorTranslator.FromHtml("#1A1C1F"));

    public static readonly IconPalette DarkTheme = new(
        ColorTranslator.FromHtml("#181818"),
        ColorTranslator.FromHtml("#FFFFFF"),
        ColorTranslator.FromHtml("#FFFFFF"));

    public static RingGeometry CalculateGeometry(double outerDiameter)
    {
        return new RingGeometry(outerDiameter, outerDiameter * 200d / 314d);
    }

    [SupportedOSPlatform("windows")]
    public static Bitmap RenderBitmap(
        UsageState state,
        int size,
        IconTheme theme = IconTheme.Light,
        Color? unusedCircleColor = null,
        DateTimeOffset? now = null)
    {
        var renderSize = size * SupersamplingScale;
        using var supersampled = RenderBitmapAtSize(state, renderSize, theme, unusedCircleColor ?? Color.Transparent, now);

        var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CompositingMode = CompositingMode.SourceCopy;
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.SmoothingMode = SmoothingMode.HighQuality;
        graphics.Clear(Color.Transparent);
        graphics.DrawImage(supersampled, new Rectangle(0, 0, size, size));
        return bitmap;
    }

    [SupportedOSPlatform("windows")]
    private static Bitmap RenderBitmapAtSize(
        UsageState state,
        int size,
        IconTheme theme,
        Color unusedCircleColor,
        DateTimeOffset? now)
    {
        var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.SmoothingMode = SmoothingMode.HighQuality;
        graphics.Clear(Color.Transparent);

        var outerUsed = state.HasError ? 0 : state.Week.UsedPercent;
        var innerUsed = state.HasError ? 0 : state.FiveHour.UsedPercent;
        DrawRings(
            graphics,
            size,
            outerUsed,
            innerUsed,
            state.HasError ? null : state.Week.ResetAt,
            now,
            PaletteFor(theme),
            unusedCircleColor);
        return bitmap;
    }

    [SupportedOSPlatform("windows")]
    public static Icon RenderIcon(
        UsageState state,
        int size = 32,
        IconTheme theme = IconTheme.Light,
        DateTimeOffset? now = null)
    {
        using var bitmap = RenderBitmap(state, size, theme, now: now ?? DateTimeOffset.Now);
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
    private static void DrawRings(
        Graphics graphics,
        int canvasSize,
        double outerUsedPercent,
        double innerUsedPercent,
        DateTimeOffset? weekResetAt,
        DateTimeOffset? now,
        IconPalette palette,
        Color unusedCircleColor)
    {
        var geometry = CalculateGeometry(canvasSize);

        var outerRect = CenteredRect(canvasSize, (float)geometry.OuterDiameter);
        var innerRect = CenteredRect(canvasSize, (float)geometry.InnerDiameter);

        DrawPieDisc(graphics, outerRect, OuterRingColor, outerUsedPercent, unusedCircleColor);
        DrawPieDisc(graphics, innerRect, palette.InnerRingColor, innerUsedPercent, unusedCircleColor);

        if (weekResetAt.HasValue && now.HasValue)
        {
            DrawWeekProgressNeedle(graphics, canvasSize, outerRect, weekResetAt.Value, now.Value);
        }
    }

    [SupportedOSPlatform("windows")]
    private static void DrawPieDisc(Graphics graphics, RectangleF rect, Color usedColor, double usedPercent, Color unusedCircleColor)
    {
        using var usedBrush = new SolidBrush(usedColor);
        using var transparentPath = new GraphicsPath();
        transparentPath.AddEllipse(rect);

        graphics.SetClip(transparentPath);
        graphics.Clear(unusedCircleColor);
        graphics.ResetClip();

        var sweep = (float)(Math.Clamp(usedPercent, 0d, 100d) / 100d * 360d);
        if (sweep > 0)
        {
            graphics.FillPie(usedBrush, rect, -90, sweep);
        }
    }

    [SupportedOSPlatform("windows")]
    private static void DrawWeekProgressNeedle(
        Graphics graphics,
        int canvasSize,
        RectangleF outerRect,
        DateTimeOffset weekResetAt,
        DateTimeOffset now)
    {
        const double weekWindowDays = 7d;
        var weekStartAt = weekResetAt.AddDays(-weekWindowDays);
        var elapsed = now - weekStartAt;
        var total = weekResetAt - weekStartAt;
        var progress = Math.Clamp(elapsed.TotalMilliseconds / total.TotalMilliseconds, 0d, 1d);
        var angleRadians = (-90d + (progress * 360d)) * Math.PI / 180d;
        var center = new PointF(outerRect.Left + (outerRect.Width / 2f), outerRect.Top + (outerRect.Height / 2f));
        var outerRadius = outerRect.Width / 2f;
        var end = new PointF(
            center.X + ((float)Math.Cos(angleRadians) * outerRadius),
            center.Y + ((float)Math.Sin(angleRadians) * outerRadius));

        using var pen = new Pen(WeekProgressNeedleColor, Math.Max(12f, canvasSize * 0.026f))
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        graphics.DrawLine(pen, center, end);
    }

    private static RectangleF CenteredRect(int canvasSize, float diameter)
    {
        var offset = (canvasSize - diameter) / 2f;
        return new RectangleF(offset, offset, diameter, diameter);
    }

    public static IconPalette PaletteFor(IconTheme theme)
    {
        return theme == IconTheme.Dark ? DarkTheme : LightTheme;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
