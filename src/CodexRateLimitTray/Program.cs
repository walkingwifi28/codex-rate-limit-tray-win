namespace CodexRateLimitTray;

static class Program
{
    [STAThread]
    static void Main()
    {
        using var instanceGuard = CodexRateLimitTray.Core.SingleInstanceGuard.TryAcquire();
        if (!instanceGuard.HasHandle)
        {
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayAppContext());
    }    
}
