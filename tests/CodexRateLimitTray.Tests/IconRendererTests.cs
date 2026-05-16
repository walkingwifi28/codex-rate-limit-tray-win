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

    private static void AssertColorNear(Color expected, Color actual)
    {
        Assert.InRange(Math.Abs(expected.R - actual.R), 0, 4);
        Assert.InRange(Math.Abs(expected.G - actual.G), 0, 4);
        Assert.InRange(Math.Abs(expected.B - actual.B), 0, 4);
        Assert.InRange(actual.A, 250, 255);
    }
}
