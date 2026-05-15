using CodexRateLimitTray.Core;

namespace CodexRateLimitTray;

internal sealed class TrayAppContext : ApplicationContext
{
    private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(20) };
    private readonly WhamUsageClient _usageClient;
    private readonly NotifyIcon _notifyIcon = new();
    private readonly ContextMenuStrip _menu = new();
    private readonly ToolStripMenuItem _startupItem = new("Windows 起動時に実行") { CheckOnClick = true };
    private readonly UsagePopupForm _popup = new();
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = (int)RefreshSchedule.AutomaticRefreshInterval.TotalMilliseconds };
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private UsageState _currentState = UsageState.Error(UsageErrorKind.Network, "未取得");
    private Icon? _currentIcon;

    public TrayAppContext()
    {
        _usageClient = new WhamUsageClient(_httpClient, TimeZoneInfo.Local);
        BuildMenu();

        _notifyIcon.Text = "Codex レート制限";
        _notifyIcon.ContextMenuStrip = _menu;
        _notifyIcon.MouseUp += OnNotifyIconMouseUp;
        _notifyIcon.Visible = true;

        UpdateTrayVisual();

        _timer.Tick += async (_, _) => await RefreshAsync().ConfigureAwait(true);
        _timer.Start();
        _ = RefreshAsync();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
            _popup.Dispose();
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _menu.Dispose();
            _currentIcon?.Dispose();
            _refreshLock.Dispose();
            _httpClient.Dispose();
        }

        base.Dispose(disposing);
    }

    private void BuildMenu()
    {
        _menu.Font = AppFonts.Create(8.5f);

        var refreshItem = new ToolStripMenuItem("更新");
        refreshItem.Click += async (_, _) => await RefreshAsync().ConfigureAwait(true);

        _startupItem.Checked = StartupRegistration.IsEnabled();
        _startupItem.CheckedChanged += (_, _) => StartupRegistration.SetEnabled(_startupItem.Checked);

        var exitItem = new ToolStripMenuItem("終了");
        exitItem.Click += (_, _) => ExitThread();

        _menu.Items.Add(refreshItem);
        _menu.Items.Add(_startupItem);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(exitItem);
    }

    private async void OnNotifyIconMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        await RefreshAsync().ConfigureAwait(true);
        _popup.UpdateState(_currentState);
        _popup.ShowNearCursor();
    }

    private async Task RefreshAsync()
    {
        if (!await _refreshLock.WaitAsync(0).ConfigureAwait(true))
        {
            return;
        }

        try
        {
            var auth = CodexAuthReader.ReadAccessToken();
            _currentState = auth.IsSuccess
                ? await _usageClient.GetUsageAsync(auth.Token!, CancellationToken.None).ConfigureAwait(true)
                : UsageState.Error(UsageErrorKind.AuthFile, auth.Message);
            UpdateTrayVisual();
            _popup.UpdateState(_currentState);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private void UpdateTrayVisual()
    {
        var nextIcon = RateLimitIconRenderer.RenderIcon(_currentState);
        var previousIcon = _currentIcon;
        _currentIcon = nextIcon;
        _notifyIcon.Icon = nextIcon;
        previousIcon?.Dispose();
        _notifyIcon.Text = ToNotifyIconText(_currentState);
    }

    private static string ToNotifyIconText(UsageState state)
    {
        var lines = state.HasError ? null : UsageDisplayFormatter.FormatUsageLines(state);
        var text = state.HasError
            ? $"Codex レート制限{Environment.NewLine}取得できません{Environment.NewLine}{state.ErrorMessage}"
            : $"Codex レート制限{Environment.NewLine}{lines!.FiveHour}{Environment.NewLine}{lines.Week}";

        return text.Length <= 63 ? text : text[..63];
    }
}
