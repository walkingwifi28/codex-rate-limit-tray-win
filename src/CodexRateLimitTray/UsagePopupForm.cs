using CodexRateLimitTray.Core;
using System.Drawing.Drawing2D;
using System.Globalization;

namespace CodexRateLimitTray;

internal sealed class UsagePopupForm : Form
{
    private const int PopupWidth = 281;
    private const int HorizontalPadding = 10;
    private const int LabelWidth = PopupWidth - (HorizontalPadding * 2);
    private const int PinButtonSize = 20;
    private const int TitleWidth = PopupWidth - (HorizontalPadding * 3) - PinButtonSize;
    private const int PercentColumnIndex = 3;
    private static readonly int[] UsageColumnLefts = [12, 58, 70, 104, 156, 214];
    private static readonly int[] UsageColumnWidths = [46, 12, 34, 52, 58, 57];

    private readonly Label _title = new();
    private readonly PinIconButton _pinButton = new();
    private readonly PictureBox _graph = new();
    private readonly Label[][] _usageRows =
    [
        CreateUsageRow(),
        CreateUsageRow()
    ];
    private readonly Label _errorLine1 = new();
    private readonly Label _errorLine2 = new();
    private bool _isDisposing;

    internal bool IsPinned => _pinButton.IsPinned;

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
        _title.Size = new Size(TitleWidth, 22);

        _pinButton.Name = "PinButton";
        _pinButton.AccessibleName = "ピン止め";
        _pinButton.Location = new Point(ClientSize.Width - _pinButton.Width - 10, 10);
        _pinButton.Click += (_, _) => _pinButton.IsPinned = !_pinButton.IsPinned;

        _graph.Size = new Size(140, 140);
        _graph.SizeMode = PictureBoxSizeMode.CenterImage;
        _graph.Location = new Point((ClientSize.Width - _graph.Width) / 2, 40);

        ConfigureUsageRow(_usageRows[0], 192);
        ConfigureUsageRow(_usageRows[1], 220);
        ConfigureFullLine(_errorLine1, 192);
        ConfigureFullLine(_errorLine2, 220);
        _errorLine1.Visible = false;
        _errorLine2.Visible = false;

        Controls.AddRange(new Control[] { _title, _pinButton, _graph });
        Controls.AddRange(_usageRows.SelectMany(row => row).Cast<Control>().ToArray());
        Controls.AddRange(new Control[] { _errorLine1, _errorLine2 });
        Deactivate += (_, _) =>
        {
            if (!IsPinned)
            {
                Hide();
            }
        };
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
            _isDisposing = true;
            _graph.Image?.Dispose();
            _title.Dispose();
            _pinButton.Dispose();
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

    protected override void SetVisibleCore(bool value)
    {
        if (!value && IsPinned && !_isDisposing)
        {
            return;
        }

        base.SetVisibleCore(value);
    }

    private static Label[] CreateUsageRow()
    {
        return
        [
            new Label(),
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
            label.Font = AppFonts.Create(10f);
            label.Location = new Point(UsageColumnLefts[i], top);
            label.Size = new Size(UsageColumnWidths[i], 22);
        }

        row[PercentColumnIndex].TextAlign = ContentAlignment.MiddleRight;
    }

    private static void ConfigureFullLine(Label label, int top)
    {
        label.AutoSize = false;
        label.TextAlign = ContentAlignment.MiddleLeft;
        label.Font = AppFonts.Create(10f);
        label.Location = new Point(HorizontalPadding, top);
        label.Size = new Size(LabelWidth, 22);
    }

    private static void SetUsageRow(Label[] row, string label, string remainingText, string resetDateText, string resetTimeText)
    {
        row[0].Text = label;
        row[1].Text = ":";
        row[2].Text = "残り";
        row[3].Text = $"{remainingText}%";
        row[4].Text = resetDateText;
        row[5].Text = resetTimeText;
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
        _pinButton.BackColor = palette.BackgroundColor;
        _pinButton.ForeColor = palette.TextColor;
        _pinButton.FlatAppearance.MouseDownBackColor = palette.BackgroundColor;
        _pinButton.FlatAppearance.MouseOverBackColor = palette.BackgroundColor;
        foreach (var label in _usageRows.SelectMany(row => row))
        {
            label.ForeColor = palette.TextColor;
        }

        _errorLine1.ForeColor = palette.TextColor;
        _errorLine2.ForeColor = palette.TextColor;
    }

    private sealed class PinIconButton : Button
    {
        private static readonly PointF[] PinnedHead =
        [
            new(444.241f, 119.211f),
            new(394.039f, 69.017f),
            new(326.28f, 1.25f),
            new(294.904f, 32.625f),
            new(307.453f, 72.778f),
            new(156.862f, 190.74f),
            new(84.7f, 165.894f),
            new(40.196f, 206.515f),
            new(173.475f, 339.784f),
            new(306.744f, 473.061f),
            new(347.355f, 428.55f),
            new(322.518f, 356.388f),
            new(440.471f, 205.797f),
            new(480.624f, 218.354f),
            new(512f, 186.978f)
        ];

        private static readonly PointF[] PinnedNeedle =
        [
            new(18.828f, 454.277f),
            new(0f, 510.75f),
            new(56.464f, 491.914f),
            new(179.451f, 368.937f),
            new(141.805f, 331.291f)
        ];

        private static readonly PointF[] UnpinnedOuter =
        [
            new(432.697f, 128.286f),
            new(383.723f, 79.304f),
            new(304.403f, 0f),
            new(251.117f, 53.286f),
            new(262.794f, 90.635f),
            new(155.149f, 171.122f),
            new(83.47f, 146.428f),
            new(3.622f, 221.02f),
            new(128.934f, 346.323f),
            new(4.253f, 471.013f),
            new(0f, 512f),
            new(40.978f, 507.747f),
            new(165.668f, 383.066f),
            new(290.98f, 508.378f),
            new(365.564f, 428.53f),
            new(340.887f, 356.851f),
            new(421.357f, 249.206f),
            new(458.714f, 260.874f),
            new(512f, 207.588f)
        ];

        private static readonly PointF[] UnpinnedInner =
        [
            new(448.298f, 218.539f),
            new(407.285f, 205.721f),
            new(299.182f, 350.326f),
            new(322.986f, 419.464f),
            new(290.072f, 454.703f),
            new(173.68f, 338.319f),
            new(57.297f, 221.928f),
            new(92.537f, 189.014f),
            new(161.675f, 212.817f),
            new(306.279f, 104.706f),
            new(293.46f, 63.702f),
            new(304.403f, 52.76f),
            new(357.343f, 105.691f),
            new(406.307f, 154.665f),
            new(459.24f, 207.588f)
        ];

        private bool _isPinned;

        public PinIconButton()
        {
            Size = new Size(PinButtonSize, PinButtonSize);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            TabStop = false;
            Cursor = Cursors.Hand;
            AccessibleDescription = "ピン止め無効";
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        }

        public bool IsPinned
        {
            get => _isPinned;
            set
            {
                if (_isPinned == value)
                {
                    return;
                }

                _isPinned = value;
                AccessibleDescription = value ? "ピン止め有効" : "ピン止め無効";
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
            e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using var path = CreateIconPath();
            using var transform = CreateIconTransform();
            path.Transform(transform);

            using var brush = new SolidBrush(ForeColor);
            e.Graphics.FillPath(brush, path);

            if (Focused)
            {
                ControlPaint.DrawFocusRectangle(e.Graphics, ClientRectangle);
            }
        }

        private GraphicsPath CreateIconPath()
        {
            var path = new GraphicsPath(FillMode.Alternate);
            if (IsPinned)
            {
                path.AddPolygon(PinnedHead);
                path.AddPolygon(PinnedNeedle);
                return path;
            }

            path.AddPolygon(UnpinnedOuter);
            path.AddPolygon(UnpinnedInner);
            return path;
        }

        private Matrix CreateIconTransform()
        {
            const float sourceSize = 512f;
            const float padding = 3f;
            var targetSize = Math.Min(ClientSize.Width, ClientSize.Height) - (padding * 2f);
            var scale = targetSize / sourceSize;
            var offsetX = (ClientSize.Width - (sourceSize * scale)) / 2f;
            var offsetY = (ClientSize.Height - (sourceSize * scale)) / 2f;
            var transform = new Matrix();
            transform.Translate(offsetX, offsetY);
            transform.Scale(scale, scale);
            return transform;
        }
    }
}
