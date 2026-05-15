using CodexRateLimitTray.Core;

namespace CodexRateLimitTray.Tests;

public sealed class SingleInstanceGuardTests
{
    [Fact]
    public void Second_guard_with_same_name_cannot_acquire_lock()
    {
        var name = $"Local\\WalkingWiFi.CodexRateLimitTray.Tests.{Guid.NewGuid():N}";

        using var first = SingleInstanceGuard.TryAcquire(name);
        using var second = SingleInstanceGuard.TryAcquire(name);

        Assert.True(first.HasHandle);
        Assert.False(second.HasHandle);
    }
}
