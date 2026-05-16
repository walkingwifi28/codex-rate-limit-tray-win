using CodexRateLimitTray.Core;
using System.Drawing;

namespace CodexRateLimitTray.Tests;

public sealed class IconRendererTests
{
    [Fact]
    public void Ring_geometry_uses_required_outer_to_inner_diameter_ratio()
    {
        var geometry = RateLimitIconRenderer.CalculateGeometry(314);

        Assert.Equal(314, geometry.OuterDiameter);
        Assert.Equal(200, geometry.InnerDiameter);
        Assert.Equal(314d / 200d, geometry.OuterDiameter / geometry.InnerDiameter, precision: 6);
    }

    [Fact]
    public void Renderer_draws_filled_pie_discs_instead_of_ring_strokes()
    {
        var state = UsageState.Success(
            new UsageWindow(100, DateTimeOffset.UnixEpoch),
            new UsageWindow(25, DateTimeOffset.UnixEpoch));

        using var bitmap = RateLimitIconRenderer.RenderBitmap(state, 314);

        AssertColorNear(RateLimitIconRenderer.LightTheme.InnerRingColor, bitmap.GetPixel(157, 157));
        AssertColorNear(RateLimitIconRenderer.OuterRingColor, bitmap.GetPixel(210, 60));
        Assert.Equal(0, bitmap.GetPixel(0, 0).A);
    }

    [Fact]
    public void Renderer_uses_light_theme_palette()
    {
        var state = UsageState.Success(
            new UsageWindow(100, DateTimeOffset.UnixEpoch),
            new UsageWindow(100, DateTimeOffset.UnixEpoch));

        using var bitmap = RateLimitIconRenderer.RenderBitmap(state, 314, IconTheme.Light);

        Assert.Equal(ColorTranslator.FromHtml("#FFFFFF"), RateLimitIconRenderer.LightTheme.BackgroundColor);
        Assert.Equal(ColorTranslator.FromHtml("#1A1C1F"), RateLimitIconRenderer.LightTheme.TextColor);
        AssertColorNear(ColorTranslator.FromHtml("#1A1C1F"), bitmap.GetPixel(157, 157));
        AssertColorNear(ColorTranslator.FromHtml("#339CFF"), bitmap.GetPixel(210, 60));
        Assert.Equal(0, bitmap.GetPixel(0, 0).A);
    }

    [Fact]
    public void Renderer_uses_dark_theme_palette()
    {
        var state = UsageState.Success(
            new UsageWindow(100, DateTimeOffset.UnixEpoch),
            new UsageWindow(100, DateTimeOffset.UnixEpoch));

        using var bitmap = RateLimitIconRenderer.RenderBitmap(state, 314, IconTheme.Dark);

        Assert.Equal(ColorTranslator.FromHtml("#181818"), RateLimitIconRenderer.DarkTheme.BackgroundColor);
        Assert.Equal(ColorTranslator.FromHtml("#FFFFFF"), RateLimitIconRenderer.DarkTheme.TextColor);
        AssertColorNear(ColorTranslator.FromHtml("#FFFFFF"), bitmap.GetPixel(157, 157));
        AssertColorNear(ColorTranslator.FromHtml("#339CFF"), bitmap.GetPixel(210, 60));
        Assert.Equal(0, bitmap.GetPixel(0, 0).A);
    }

    [Fact]
    public void Renderer_leaves_unused_circle_area_transparent()
    {
        var state = UsageState.Success(
            new UsageWindow(0, DateTimeOffset.UnixEpoch),
            new UsageWindow(100, DateTimeOffset.UnixEpoch));

        using var bitmap = RateLimitIconRenderer.RenderBitmap(state, 314, IconTheme.Light);

        Assert.Equal(0, bitmap.GetPixel(157, 157).A);
    }

    [Fact]
    public void Renderer_antialiases_popup_sized_pie_edges()
    {
        var state = UsageState.Success(
            new UsageWindow(33, DateTimeOffset.UnixEpoch),
            new UsageWindow(66, DateTimeOffset.UnixEpoch));

        using var bitmap = RateLimitIconRenderer.RenderBitmap(state, 124, IconTheme.Light);

        var partiallyTransparentPixels = 0;
        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                var alpha = bitmap.GetPixel(x, y).A;
                if (alpha is > 0 and < 255)
                {
                    partiallyTransparentPixels++;
                }
            }
        }

        Assert.InRange(partiallyTransparentPixels, 500, int.MaxValue);
    }

    [Fact]
    public void Renderer_draws_bright_red_week_progress_needle_from_center_to_outer_disc()
    {
        var resetAt = new DateTimeOffset(2026, 5, 18, 0, 0, 0, TimeSpan.Zero);
        var halfwayToReset = resetAt.AddDays(-3.5);
        var state = UsageState.Success(
            new UsageWindow(0, resetAt),
            new UsageWindow(0, resetAt));

        using var bitmap = RateLimitIconRenderer.RenderBitmap(state, 314, IconTheme.Light, now: halfwayToReset);

        Assert.Equal(ColorTranslator.FromHtml("#FF0000"), RateLimitIconRenderer.WeekProgressNeedleColor);
        AssertRedNeedlePixel(bitmap.GetPixel(157, 157));
        AssertRedNeedlePixel(bitmap.GetPixel(157, 300));
    }

    [Fact]
    public void RenderIcon_draws_week_progress_needle_for_tray_icon()
    {
        var resetAt = new DateTimeOffset(2026, 5, 18, 0, 0, 0, TimeSpan.Zero);
        var halfwayToReset = resetAt.AddDays(-3.5);
        var state = UsageState.Success(
            new UsageWindow(0, resetAt),
            new UsageWindow(0, resetAt));

        using var icon = RateLimitIconRenderer.RenderIcon(state, 32, IconTheme.Light, halfwayToReset);
        using var bitmap = icon.ToBitmap();

        Assert.InRange(CountRedDominantPixels(bitmap), 8, int.MaxValue);
    }

    [Fact]
    public void RenderIcon_draws_thick_week_progress_needle_for_tray_icon()
    {
        var resetAt = new DateTimeOffset(2026, 5, 18, 0, 0, 0, TimeSpan.Zero);
        var halfwayToReset = resetAt.AddDays(-3.5);
        var state = UsageState.Success(
            new UsageWindow(0, resetAt),
            new UsageWindow(0, resetAt));

        using var icon = RateLimitIconRenderer.RenderIcon(state, 32, IconTheme.Light, halfwayToReset);
        using var bitmap = icon.ToBitmap();

        Assert.InRange(CountRedDominantPixelsInRow(bitmap, 24), 3, int.MaxValue);
    }

    private static void AssertColorNear(Color expected, Color actual)
    {
        Assert.InRange(Math.Abs(expected.R - actual.R), 0, 4);
        Assert.InRange(Math.Abs(expected.G - actual.G), 0, 4);
        Assert.InRange(Math.Abs(expected.B - actual.B), 0, 4);
        Assert.InRange(actual.A, 250, 255);
    }

    private static void AssertRedNeedlePixel(Color actual)
    {
        Assert.InRange(actual.R, 220, 255);
        Assert.InRange(actual.G, 0, 90);
        Assert.InRange(actual.B, 0, 90);
        Assert.InRange(actual.A, 240, 255);
    }

    private static int CountRedDominantPixels(Bitmap bitmap)
    {
        var count = 0;
        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                if (pixel.A > 0 && pixel.R > pixel.G + 50 && pixel.R > pixel.B + 50)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static int CountRedDominantPixelsInRow(Bitmap bitmap, int y)
    {
        var count = 0;
        for (var x = 0; x < bitmap.Width; x++)
        {
            var pixel = bitmap.GetPixel(x, y);
            if (pixel.A > 0 && pixel.R > pixel.G + 50 && pixel.R > pixel.B + 50)
            {
                count++;
            }
        }

        return count;
    }
}
