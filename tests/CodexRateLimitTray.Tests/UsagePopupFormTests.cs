using CodexRateLimitTray;
using CodexRateLimitTray.Core;
using System.Drawing;
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

    [Fact]
    public void Popup_labels_use_primary_ui_font_without_centered_usage_text()
    {
        using var form = new UsagePopupForm();

        var labels = LabelsIn(form).ToArray();
        var usageLabels = labels.Where(label => label.Text != UsageDisplayFormatter.Title).ToArray();

        Assert.All(labels, label =>
        {
            Assert.Contains(label.Font.Name, new[] { "BIZ UDPGothic", "BIZ UDPゴシック" });
        });

        Assert.All(usageLabels, label =>
        {
            Assert.NotEqual(ContentAlignment.MiddleCenter, label.TextAlign);
        });
    }

    [Fact]
    public void Usage_parts_are_laid_out_in_matching_columns()
    {
        using var form = new UsagePopupForm();
        var state = UsageState.Success(
            new UsageWindow(6, new DateTimeOffset(2026, 5, 17, 18, 48, 0, TimeSpan.Zero)),
            new UsageWindow(1, new DateTimeOffset(2026, 5, 24, 13, 48, 0, TimeSpan.Zero)));

        form.UpdateState(state, IconTheme.Dark);

        var labels = LabelsIn(form)
            .Where(label => label.Text != UsageDisplayFormatter.Title)
            .Where(label => label.Top is 192 or 220)
            .Where(label => label.Left is 12 or 58 or 69 or 146 or 204)
            .ToArray();

        Assert.Contains(labels, label => label.Text == "5時間");
        Assert.Contains(labels, label => label.Text == "週");
        Assert.Contains(labels, label => label.Text == "残り 94%");
        Assert.Contains(labels, label => label.Text == "残り 99%");
        Assert.Contains(labels, label => label.Text == "05/24");
        Assert.Contains(labels, label => label.Text == "13:48");
        AssertColumnAligned(labels.Where(label => label.Text is "残り 94%" or "残り 99%"), 2);
        AssertColumnAligned(labels.Where(label => label.Text is "" or "05/24"), 2);
        AssertColumnAligned(labels.Where(label => label.Text is "18:48" or "13:48"), 2);
        AssertColumnLeft(labels, ":", 58);
        AssertColumnLeft(labels.Where(label => label.Text is "残り 94%" or "残り 99%"), 69);
        AssertColumnLeft(labels.Where(label => label.Text is "" or "05/24"), 146);
        AssertColumnLeft(labels.Where(label => label.Text is "18:48" or "13:48"), 204);
    }

    [Fact]
    public void Usage_labels_are_wide_enough_for_primary_ui_font_text()
    {
        using var form = new UsagePopupForm();
        var state = UsageState.Success(
            new UsageWindow(6, new DateTimeOffset(2026, 5, 17, 18, 48, 0, TimeSpan.Zero)),
            new UsageWindow(1, new DateTimeOffset(2026, 5, 24, 13, 48, 0, TimeSpan.Zero)));

        form.UpdateState(state, IconTheme.Dark);

        var labels = LabelsIn(form)
            .Where(label => !string.IsNullOrEmpty(label.Text))
            .Where(label => label.Top is 192 or 220)
            .Where(label => label.Left is 12 or 58 or 69 or 146 or 204)
            .ToArray();

        Assert.All(labels, label =>
        {
            var measuredWidth = TextRenderer.MeasureText(label.Text, label.Font).Width;

            Assert.True(
                measuredWidth <= label.Width,
                $"'{label.Text}' needs {measuredWidth}px but label width is {label.Width}px.");
        });
    }

    private static IEnumerable<Label> LabelsIn(Control control)
    {
        foreach (Control child in control.Controls)
        {
            if (child is Label label)
            {
                yield return label;
            }

            foreach (var nested in LabelsIn(child))
            {
                yield return nested;
            }
        }
    }

    private static void AssertColumnAligned(IEnumerable<Label> labels, string text, int expectedCount)
    {
        var matching = labels.Where(label => label.Text == text).ToArray();

        Assert.Equal(expectedCount, matching.Length);
        Assert.Single(matching.Select(label => label.Left).Distinct());
    }

    private static void AssertColumnAligned(IEnumerable<Label> labels, int expectedCount)
    {
        var matching = labels.ToArray();

        Assert.Equal(expectedCount, matching.Length);
        Assert.Single(matching.Select(label => label.Left).Distinct());
    }

    private static void AssertColumnLeft(IEnumerable<Label> labels, string text, int expectedLeft)
    {
        var matching = labels.Where(label => label.Text == text).ToArray();

        Assert.NotEmpty(matching);
        Assert.All(matching, label => Assert.Equal(expectedLeft, label.Left));
    }

    private static void AssertColumnLeft(IEnumerable<Label> labels, int expectedLeft)
    {
        var matching = labels.ToArray();

        Assert.NotEmpty(matching);
        Assert.All(matching, label => Assert.Equal(expectedLeft, label.Left));
    }
}
