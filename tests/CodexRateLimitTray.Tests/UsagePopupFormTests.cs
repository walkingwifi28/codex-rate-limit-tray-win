using CodexRateLimitTray;
using CodexRateLimitTray.Core;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace CodexRateLimitTray.Tests;

public sealed class UsagePopupFormTests
{
    private static readonly UsageState LoadedState = UsageState.Success(
        new UsageWindow(1, new DateTimeOffset(2026, 5, 24, 13, 48, 0, TimeSpan.Zero)));

    [Fact]
    public void Popup_removes_standard_title_bar()
    {
        using var form = new UsagePopupForm();

        Assert.Equal(FormBorderStyle.None, form.FormBorderStyle);
    }

    [Fact]
    public void Pin_button_starts_unpinned_at_top_right_and_uses_theme_text_color()
    {
        using var form = new UsagePopupForm();

        form.UpdateState(LoadedState, IconTheme.Dark);

        var pinButton = PinButtonIn(form);
        Assert.False(form.IsPinned);
        Assert.Equal(new Point(form.ClientSize.Width - pinButton.Width - 10, 10), pinButton.Location);
        Assert.Equal(RateLimitIconRenderer.DarkTheme.TextColor, pinButton.ForeColor);
        Assert.Equal("ピン止め", pinButton.AccessibleName);
        Assert.Contains("無効", pinButton.AccessibleDescription);
    }

    [Fact]
    public void Pin_button_uses_compact_icon_size()
    {
        using var form = new UsagePopupForm();

        var pinButton = PinButtonIn(form);

        Assert.Equal(new Size(20, 20), pinButton.Size);
    }

    [Fact]
    public void Pin_button_does_not_overlap_title_label()
    {
        using var form = new UsagePopupForm();

        var title = LabelsIn(form).Single(label => label.Text == UsageDisplayFormatter.Title);
        var pinButton = PinButtonIn(form);

        Assert.False(title.Bounds.IntersectsWith(pinButton.Bounds));
    }

    [Fact]
    public void Clicking_pin_button_toggles_pinned_state_and_accessible_description()
    {
        using var form = new UsagePopupForm();
        var pinButton = PinButtonIn(form);
        form.Show();

        pinButton.PerformClick();

        Assert.True(form.IsPinned);
        Assert.Contains("有効", pinButton.AccessibleDescription);

        pinButton.PerformClick();

        Assert.False(form.IsPinned);
        Assert.Contains("無効", pinButton.AccessibleDescription);
    }

    [Fact]
    public void Popup_ignores_deactivate_while_pinned()
    {
        using var form = new UsagePopupForm();
        var pinButton = PinButtonIn(form);
        form.Show();
        Assert.True(form.Visible);

        pinButton.PerformClick();
        InvokeDeactivate(form);

        Assert.True(form.Visible);

        pinButton.PerformClick();
        InvokeDeactivate(form);

        Assert.False(form.Visible);
    }

    [Fact]
    public void Popup_ignores_hide_requests_while_pinned()
    {
        using var form = new UsagePopupForm();
        var pinButton = PinButtonIn(form);
        form.Show();
        pinButton.PerformClick();

        form.Hide();

        Assert.True(form.Visible);
    }

    [Fact]
    public void Popup_stays_visible_when_state_refreshes_while_pinned()
    {
        using var form = new UsagePopupForm();
        var pinButton = PinButtonIn(form);
        form.Show();
        pinButton.PerformClick();

        form.UpdateState(LoadedState, IconTheme.Dark);

        Assert.True(form.IsPinned);
        Assert.True(form.Visible);
    }

    [Fact]
    public void Error_lines_fit_inside_popup()
    {
        using var form = new UsagePopupForm();

        form.UpdateState(UsageState.Error(UsageErrorKind.Network, "ネットワークエラー"), IconTheme.Dark);

        var errorLabels = LabelsIn(form)
            .Where(label => label.Text is "取得できません" or "ネットワークエラー")
            .ToArray();

        Assert.Equal(2, errorLabels.Length);
        Assert.All(errorLabels, label => Assert.InRange(label.Bottom, 0, form.ClientSize.Height));
    }

    [Fact]
    public void Popup_labels_use_primary_ui_font_without_centered_usage_text()
    {
        using var form = new UsagePopupForm();

        var labels = LabelsIn(form).ToArray();
        var usageLabels = labels.Where(label => label.Text != UsageDisplayFormatter.Title).ToArray();
        using var expectedFont = AppFonts.Create(10f);

        Assert.All(labels, label =>
        {
            Assert.Equal(expectedFont.FontFamily.Name, label.Font.FontFamily.Name);
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
            new UsageWindow(1, new DateTimeOffset(2026, 5, 24, 13, 48, 0, TimeSpan.Zero)));

        form.UpdateState(state, IconTheme.Dark);

        var labels = LabelsIn(form)
            .Where(label => label.Text != UsageDisplayFormatter.Title)
            .Where(label => label.Top is 192)
            .Where(label => label.Left is 12 or 58 or 70 or 104 or 156 or 214)
            .ToArray();

        Assert.Contains(labels, label => label.Text == "週");
        Assert.Contains(labels, label => label.Text == "99%");
        Assert.Contains(labels, label => label.Text == "05/24");
        Assert.Contains(labels, label => label.Text == "13:48");
        Assert.All(labels.Where(label => label.Text == "99%"), label =>
        {
            Assert.Equal(ContentAlignment.MiddleRight, label.TextAlign);
        });
        AssertColumnAligned(labels, "残り", 1);
        AssertColumnAligned(labels.Where(label => label.Text is "" or "05/24"), 1);
        AssertColumnAligned(labels.Where(label => label.Text == "13:48"), 1);
        AssertColumnLeft(labels, ":", 58);
        AssertColumnLeft(labels, "残り", 70);
        AssertColumnLeft(labels.Where(label => label.Text == "99%"), 104);
        AssertColumnLeft(labels.Where(label => label.Text is "" or "05/24"), 156);
        AssertColumnLeft(labels.Where(label => label.Text == "13:48"), 214);
    }

    [Fact]
    public void Usage_labels_are_wide_enough_for_primary_ui_font_text()
    {
        using var form = new UsagePopupForm();
        var state = UsageState.Success(
            new UsageWindow(1, new DateTimeOffset(2026, 5, 24, 13, 48, 0, TimeSpan.Zero)));

        form.UpdateState(state, IconTheme.Dark);

        var labels = LabelsIn(form)
            .Where(label => !string.IsNullOrEmpty(label.Text))
            .Where(label => label.Top is 192)
            .Where(label => label.Left is 12 or 58 or 70 or 104 or 156 or 214)
            .ToArray();

        Assert.All(labels, label =>
        {
            var measuredWidth = TextRenderer.MeasureText(label.Text, label.Font).Width;

            Assert.True(
                measuredWidth <= label.Width,
                $"'{label.Text}' needs {measuredWidth}px but label width is {label.Width}px.");
        });
    }

    [Fact]
    public void Usage_labels_are_wide_enough_for_full_remaining_percent()
    {
        using var form = new UsagePopupForm();
        var state = UsageState.Success(
            new UsageWindow(0, new DateTimeOffset(2026, 5, 24, 13, 48, 0, TimeSpan.Zero)));

        form.UpdateState(state, IconTheme.Dark);

        var fullPercentLabels = LabelsIn(form)
            .Where(label => label.Text == "100%")
            .ToArray();

        Assert.Single(fullPercentLabels);
        Assert.All(fullPercentLabels, label =>
        {
            var measuredWidth = TextRenderer.MeasureText(label.Text, label.Font).Width;

            Assert.True(
                measuredWidth <= label.Width,
                $"'{label.Text}' needs {measuredWidth}px but label width is {label.Width}px.");
        });
    }

    [Fact]
    public void Week_reset_time_column_fits_every_time_of_day_inside_popup()
    {
        using var form = new UsagePopupForm();
        var state = UsageState.Success(
            new UsageWindow(0, new DateTimeOffset(2026, 5, 24, 13, 48, 0, TimeSpan.Zero)));

        form.UpdateState(state, IconTheme.Dark);

        var timeLabel = LabelsIn(form).Single(label => label.Text == "13:48");

        Assert.True(
            timeLabel.Right <= form.ClientSize.Width - 10,
            $"Time column right edge is {timeLabel.Right}px but popup content ends at {form.ClientSize.Width - 10}px.");

        for (var hour = 0; hour < 24; hour++)
        {
            for (var minute = 0; minute < 60; minute++)
            {
                var text = $"{hour:00}:{minute:00}";
                var measuredWidth = TextRenderer.MeasureText(text, timeLabel.Font).Width;

                Assert.True(
                    measuredWidth <= timeLabel.Width,
                    $"'{text}' needs {measuredWidth}px but time column width is {timeLabel.Width}px.");
            }
        }
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

    private static Button PinButtonIn(Control control)
    {
        var button = control.Controls
            .Cast<Control>()
            .OfType<Button>()
            .SingleOrDefault(child => child.Name == "PinButton");

        Assert.NotNull(button);
        return button;
    }

    private static void InvokeDeactivate(Form form)
    {
        typeof(Form)
            .GetMethod("OnDeactivate", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(form, [EventArgs.Empty]);
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
