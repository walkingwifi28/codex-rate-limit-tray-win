using CodexRateLimitTray.Core;
using Microsoft.Win32;

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
    private readonly SynchronizationContext? _uiContext;
    private UsageState _currentState = UsageState.Error(UsageErrorKind.Network, "未取得");
    private Icon? _currentIcon;
    private bool _isDisposed;

    public TrayAppContext()
    {
        _uiContext = SynchronizationContext.Current;
        _usageClient = new WhamUsageClient(_httpClient, TimeZoneInfo.Local);
        BuildMenu();

        _notifyIcon.Text = "Codex レート制限";
        _notifyIcon.ContextMenuStrip = _menu;
        _notifyIcon.MouseUp += OnNotifyIconMouseUp;
        _notifyIcon.Visible = true;
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;

        UpdateTrayVisual();

        _timer.Tick += async (_, _) => await RefreshAsync().ConfigureAwait(true);
        _timer.Start();
        _ = RefreshAsync();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _isDisposed = true;
            _timer.Dispose();
            _popup.Dispose();
            SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
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
        _popup.UpdateState(_currentState, WindowsThemeReader.GetIconTheme());
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
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category is UserPreferenceCategory.Color or UserPreferenceCategory.General or UserPreferenceCategory.VisualStyle)
        {
            RunOnUiThread(UpdateTrayVisual);
        }
    }

    private void RunOnUiThread(Action action)
    {
        if (_isDisposed)
        {
            return;
        }

        if (_uiContext is null)
        {
            action();
            return;
        }

        _uiContext.Post(_ =>
        {
            if (!_isDisposed)
            {
                action();
            }
        }, null);
    }

    private void UpdateTrayVisual()
    {
        var theme = WindowsThemeReader.GetIconTheme();
        var nextIcon = RateLimitIconRenderer.RenderIcon(_currentState, theme: theme);
        var previousIcon = _currentIcon;
        _currentIcon = nextIcon;
        _notifyIcon.Icon = nextIcon;
        previousIcon?.Dispose();
        _notifyIcon.Text = ToNotifyIconText(_currentState);
        _popup.UpdateState(_currentState, theme);
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
