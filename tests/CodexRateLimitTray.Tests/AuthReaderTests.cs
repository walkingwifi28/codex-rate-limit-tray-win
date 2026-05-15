using CodexRateLimitTray.Core;

namespace CodexRateLimitTray.Tests;

public sealed class AuthReaderTests
{
    [Fact]
    public void Reads_access_token_from_codex_auth_json()
    {
        using var directory = new TemporaryDirectory();
        var authPath = Path.Combine(directory.Path, ".codex", "auth.json");
        Directory.CreateDirectory(Path.GetDirectoryName(authPath)!);
        File.WriteAllText(authPath, """{"tokens":{"access_token":"abc.123"}}""");

        var result = CodexAuthReader.ReadAccessToken(directory.Path);

        Assert.True(result.IsSuccess);
        Assert.Equal("abc.123", result.Token);
    }

    [Fact]
    public void Missing_auth_file_returns_error_reason()
    {
        using var directory = new TemporaryDirectory();

        var result = CodexAuthReader.ReadAccessToken(directory.Path);

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthReadError.FileNotFound, result.Error);
        Assert.Contains("auth.json", result.Message);
    }

    [Fact]
    public void Invalid_json_returns_error_reason()
    {
        using var directory = new TemporaryDirectory();
        var authPath = Path.Combine(directory.Path, ".codex", "auth.json");
        Directory.CreateDirectory(Path.GetDirectoryName(authPath)!);
        File.WriteAllText(authPath, "{bad json");

        var result = CodexAuthReader.ReadAccessToken(directory.Path);

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthReadError.InvalidJson, result.Error);
    }

    [Fact]
    public void Missing_token_returns_error_reason()
    {
        using var directory = new TemporaryDirectory();
        var authPath = Path.Combine(directory.Path, ".codex", "auth.json");
        Directory.CreateDirectory(Path.GetDirectoryName(authPath)!);
        File.WriteAllText(authPath, """{"tokens":{}}""");

        var result = CodexAuthReader.ReadAccessToken(directory.Path);

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthReadError.TokenMissing, result.Error);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        public TemporaryDirectory()
        {
            Directory.CreateDirectory(Path);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
