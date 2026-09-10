using CodexRateLimitTray;
using CodexRateLimitTray.Core;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Windows.Forms;

namespace CodexRateLimitTray.Tests;

public sealed class UsagePopupFormTests
{
    private static readonly UsageState LoadedState = UsageState.Success(
        new UsageWindow(6, new DateTimeOffset(2026, 5, 17, 18, 48, 0, TimeSpan.Zero)),
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
    public void Usage_rows_are_laid_out_in_matching_columns_and_fit_inside_popup()
    {
        using var form = new UsagePopupForm();

        form.UpdateState(LoadedState, IconTheme.Dark);

        var labels = LabelsIn(form)
            .Where(label => label.Top is 192 or 220)
            .Where(label => label.Left is 12 or 58 or 70 or 104 or 156 or 214)
            .ToArray();

        Assert.Equal(12, labels.Length);
        Assert.Equal(
            new[] { "5時間", ":", "残り", "94%", "", "18:48" },
            labels.Where(label => label.Top == 192).OrderBy(label => label.Left).Select(label => label.Text));
        Assert.Equal(
            new[] { "週", ":", "残り", "99%", "05/24", "13:48" },
            labels.Where(label => label.Top == 220).OrderBy(label => label.Left).Select(label => label.Text));

        Assert.Equal(new Size(281, 260), form.ClientSize);
        Assert.Equal(22, labels[0].Height);
        Assert.All(labels, label =>
        {
            Assert.Contains(label.Left, new[] { 12, 58, 70, 104, 156, 214 });
            Assert.True(label.Right <= form.ClientSize.Width - 10);
            Assert.True(label.Bottom <= form.ClientSize.Height);
        });
        Assert.All(labels.Where(label => label.Text is "94%" or "99%"), label =>
        {
            Assert.Equal(ContentAlignment.MiddleRight, label.TextAlign);
        });
    }

    [Fact]
    public void Usage_labels_are_wide_enough_for_primary_ui_font_text()
    {
        using var form = new UsagePopupForm();
        var state = UsageState.Success(
            new UsageWindow(1, new DateTimeOffset(2026, 5, 24, 13, 48, 0, TimeSpan.Zero)),
            null);

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
            new UsageWindow(0, new DateTimeOffset(2026, 5, 24, 13, 48, 0, TimeSpan.Zero)),
            null);

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

        form.UpdateState(LoadedState, IconTheme.Dark);

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

    [Fact]
    public void Week_row_is_blank_without_week_state_and_does_not_show_error()
    {
        using var form = new UsagePopupForm();
        var state = UsageState.Success(
            new UsageWindow(6, new DateTimeOffset(2026, 5, 17, 18, 48, 0, TimeSpan.Zero)),
            null);

        form.UpdateState(state, IconTheme.Dark);

        var weekLabels = LabelsAtTop(form, 220);

        Assert.Equal(6, weekLabels.Length);
        Assert.All(weekLabels, label => Assert.Equal(string.Empty, label.Text));
        Assert.DoesNotContain(LabelsIn(form), label => label.Text == "取得できません");
        Assert.DoesNotContain(LabelsIn(form), label => label.Text == "不明なエラー");
    }

    [Fact]
    public void Popup_formats_maximum_percent_and_time_invariantly()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");

            using var form = new UsagePopupForm();
            var state = UsageState.Success(
                new UsageWindow(-10, new DateTimeOffset(2026, 5, 17, 23, 59, 0, TimeSpan.Zero)),
                new UsageWindow(-10, new DateTimeOffset(2026, 5, 24, 23, 59, 0, TimeSpan.Zero)));

            form.UpdateState(state, IconTheme.Dark);

            Assert.Equal(
                new[] { "5時間", ":", "残り", "100%", "", "23:59" },
                LabelsAtTop(form, 192).Select(label => label.Text));
            Assert.Equal(
                new[] { "週", ":", "残り", "100%", "05/24", "23:59" },
                LabelsAtTop(form, 220).Select(label => label.Text));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
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

    private static Label[] LabelsAtTop(Control control, int top)
    {
        return LabelsIn(control)
            .Where(label => label.Top == top)
            .Where(label => label.Left is 12 or 58 or 70 or 104 or 156 or 214)
            .OrderBy(label => label.Left)
            .ToArray();
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

}
