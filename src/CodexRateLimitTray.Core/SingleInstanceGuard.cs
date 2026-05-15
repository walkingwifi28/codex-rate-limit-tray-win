namespace CodexRateLimitTray.Core;

public sealed class SingleInstanceGuard : IDisposable
{
    public const string DefaultMutexName = @"Local\WalkingWiFi.CodexRateLimitTray";

    private readonly Mutex? _mutex;

    private SingleInstanceGuard(Mutex? mutex, bool hasHandle)
    {
        _mutex = mutex;
        HasHandle = hasHandle;
    }

    public bool HasHandle { get; }

    public static SingleInstanceGuard TryAcquire(string mutexName = DefaultMutexName)
    {
        var mutex = new Mutex(initiallyOwned: true, name: mutexName, createdNew: out var createdNew);
        if (createdNew)
        {
            return new SingleInstanceGuard(mutex, hasHandle: true);
        }

        mutex.Dispose();
        return new SingleInstanceGuard(null, hasHandle: false);
    }

    public void Dispose()
    {
        if (_mutex is null)
        {
            return;
        }

        if (HasHandle)
        {
            _mutex.ReleaseMutex();
        }

        _mutex.Dispose();
    }
}
