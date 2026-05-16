using CodexRateLimitTray.Core;

namespace CodexRateLimitTray;

internal sealed class UsagePopupForm : Form
{
    private const int PopupWidth = 260;
    private const int HorizontalPadding = 10;
    private const int LabelWidth = PopupWidth - (HorizontalPadding * 2);

    private readonly Label _title = new();
    private readonly PictureBox _graph = new();
    private readonly Label _fiveHour = new();
    private readonly Label _week = new();

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

        ConfigureLabel(_fiveHour, 192);
        ConfigureLabel(_week, 220);

        Controls.AddRange(new Control[] { _title, _graph, _fiveHour, _week });
        Deactivate += (_, _) => Hide();
    }

    public void UpdateState(UsageState state, IconTheme theme)
    {
        ApplyTheme(theme);

        _graph.Image?.Dispose();
        _graph.Image = RateLimitIconRenderer.RenderBitmap(state, 124, theme, now: DateTimeOffset.Now);

        if (state.HasError)
        {
            _fiveHour.Text = "取得できません";
            _week.Text = state.ErrorMessage ?? "不明なエラー";
            return;
        }

        var lines = UsageDisplayFormatter.FormatUsageLines(state);
        _fiveHour.Text = lines.FiveHour;
        _week.Text = lines.Week;
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
            _fiveHour.Dispose();
            _week.Dispose();
        }

        base.Dispose(disposing);
    }

    private static void ConfigureLabel(Label label, int top)
    {
        label.AutoSize = false;
        label.TextAlign = ContentAlignment.MiddleCenter;
        label.Font = AppFonts.Create(10f);
        label.Location = new Point(HorizontalPadding, top);
        label.Size = new Size(LabelWidth, 22);
    }

    private void ApplyTheme(IconTheme theme)
    {
        var palette = RateLimitIconRenderer.PaletteFor(theme);

        BackColor = palette.BackgroundColor;
        _graph.BackColor = palette.BackgroundColor;
        _title.ForeColor = palette.TextColor;
        _fiveHour.ForeColor = palette.TextColor;
        _week.ForeColor = palette.TextColor;
    }
}
