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

        AssertColorNear(RateLimitIconRenderer.InnerRingColor, bitmap.GetPixel(157, 157));
        AssertColorNear(RateLimitIconRenderer.OuterRingColor, bitmap.GetPixel(210, 60));
        AssertColorNear(RateLimitIconRenderer.BackgroundRingColor, bitmap.GetPixel(25, 157));
        Assert.Equal(0, bitmap.GetPixel(0, 0).A);
    }

    private static void AssertColorNear(Color expected, Color actual)
    {
        Assert.InRange(Math.Abs(expected.R - actual.R), 0, 4);
        Assert.InRange(Math.Abs(expected.G - actual.G), 0, 4);
        Assert.InRange(Math.Abs(expected.B - actual.B), 0, 4);
        Assert.InRange(actual.A, 250, 255);
    }
}
