using CodexRateLimitTray.Core;
using System.Globalization;

namespace CodexRateLimitTray;

internal sealed class UsagePopupForm : Form
{
    private const int PopupWidth = 260;
    private const int HorizontalPadding = 10;
    private const int LabelWidth = PopupWidth - (HorizontalPadding * 2);
    private static readonly int[] UsageColumnLefts = [12, 60, 78, 150, 198];
    private static readonly int[] UsageColumnWidths = [46, 12, 70, 46, 48];

    private readonly Label _title = new();
    private readonly PictureBox _graph = new();
    private readonly Label[][] _usageRows =
    [
        CreateUsageRow(),
        CreateUsageRow()
    ];
    private readonly Label _errorLine1 = new();
    private readonly Label _errorLine2 = new();

    public UsagePopupForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        Padding = new Padding(12);
        ClientSize = new Size(PopupWidth, 260);

        _title.AutoSize = false;
        _title.Text = UsageDisplayFormatter.Title;
        _title.TextAlign = ContentAlignment.MiddleCenter;
        _title.Font = AppFonts.Create(10f, FontStyle.Bold);
        _title.Location = new Point(HorizontalPadding, 12);
        _title.Size = new Size(LabelWidth, 22);

        _graph.Size = new Size(140, 140);
        _graph.SizeMode = PictureBoxSizeMode.CenterImage;
        _graph.Location = new Point((ClientSize.Width - _graph.Width) / 2, 40);

        ConfigureUsageRow(_usageRows[0], 192);
        ConfigureUsageRow(_usageRows[1], 220);
        ConfigureFullLine(_errorLine1, 192);
        ConfigureFullLine(_errorLine2, 220);
        _errorLine1.Visible = false;
        _errorLine2.Visible = false;

        Controls.AddRange(new Control[] { _title, _graph });
        Controls.AddRange(_usageRows.SelectMany(row => row).Cast<Control>().ToArray());
        Controls.AddRange(new Control[] { _errorLine1, _errorLine2 });
        Deactivate += (_, _) => Hide();
    }

    public void UpdateState(UsageState state, IconTheme theme)
    {
        ApplyTheme(theme);

        _graph.Image?.Dispose();
        _graph.Image = RateLimitIconRenderer.RenderBitmap(state, 124, theme, now: DateTimeOffset.Now);

        if (state.HasError)
        {
            SetUsageRowsVisible(false);
            _errorLine1.Visible = true;
            _errorLine2.Visible = true;
            _errorLine1.Text = "取得できません";
            _errorLine2.Text = state.ErrorMessage ?? "不明なエラー";
            return;
        }

        _errorLine1.Visible = false;
        _errorLine2.Visible = false;
        SetUsageRowsVisible(true);
        SetUsageRow(_usageRows[0], "5時間", state.FiveHour.RemainingText, string.Empty, state.FiveHour.ResetText);
        SetUsageRow(_usageRows[1], "週", state.Week.RemainingText, state.Week.ResetAt.ToString("MM/dd", CultureInfo.InvariantCulture), state.Week.ResetText);
    }

    public void ShowNearCursor()
    {
        var workingArea = Screen.FromPoint(Cursor.Position).WorkingArea;
        var x = Math.Min(Cursor.Position.X, workingArea.Right - Width);
        var y = Math.Min(Cursor.Position.Y, workingArea.Bottom - Height);
        Location = new Point(Math.Max(workingArea.Left, x), Math.Max(workingArea.Top, y));
        Show();
        Activate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _graph.Image?.Dispose();
            _title.Dispose();
            _graph.Dispose();
            foreach (var label in _usageRows.SelectMany(row => row))
            {
                label.Dispose();
            }

            _errorLine1.Dispose();
            _errorLine2.Dispose();
        }

        base.Dispose(disposing);
    }

    private static Label[] CreateUsageRow()
    {
        return
        [
            new Label(),
            new Label(),
            new Label(),
            new Label(),
            new Label()
        ];
    }

    private static void ConfigureUsageRow(Label[] row, int top)
    {
        for (var i = 0; i < row.Length; i++)
        {
            var label = row[i];
            label.AutoSize = false;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.Font = AppFonts.CreateAligned(10f);
            label.Location = new Point(UsageColumnLefts[i], top);
            label.Size = new Size(UsageColumnWidths[i], 22);
        }
    }

    private static void ConfigureFullLine(Label label, int top)
    {
        label.AutoSize = false;
        label.TextAlign = ContentAlignment.MiddleLeft;
        label.Font = AppFonts.CreateAligned(10f);
        label.Location = new Point(HorizontalPadding, top);
        label.Size = new Size(LabelWidth, 22);
    }

    private static void SetUsageRow(Label[] row, string label, string remainingText, string resetDateText, string resetTimeText)
    {
        row[0].Text = label;
        row[1].Text = ":";
        row[2].Text = $"残り {remainingText}%";
        row[3].Text = resetDateText;
        row[4].Text = resetTimeText;
    }

    private void SetUsageRowsVisible(bool visible)
    {
        foreach (var label in _usageRows.SelectMany(row => row))
        {
            label.Visible = visible;
        }
    }

    private void ApplyTheme(IconTheme theme)
    {
        var palette = RateLimitIconRenderer.PaletteFor(theme);

        BackColor = palette.BackgroundColor;
        _graph.BackColor = palette.BackgroundColor;
        _title.ForeColor = palette.TextColor;
        foreach (var label in _usageRows.SelectMany(row => row))
        {
            label.ForeColor = palette.TextColor;
        }

        _errorLine1.ForeColor = palette.TextColor;
        _errorLine2.ForeColor = palette.TextColor;
    }
}
