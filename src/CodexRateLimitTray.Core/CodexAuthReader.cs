using System.Text.Json;

namespace CodexRateLimitTray.Core;

public static class CodexAuthReader
{
    public static AuthReadResult ReadAccessToken()
    {
        return ReadAccessToken(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
    }

    public static AuthReadResult ReadAccessToken(string userProfilePath)
    {
        var authPath = Path.Combine(userProfilePath, ".codex", "auth.json");
        if (!File.Exists(authPath))
        {
            return AuthReadResult.Failure(AuthReadError.FileNotFound, ".codex/auth.json がありません");
        }

        try
        {
            using var stream = File.OpenRead(authPath);
            using var document = JsonDocument.Parse(stream);
            if (!document.RootElement.TryGetProperty("tokens", out var tokens) ||
                !tokens.TryGetProperty("access_token", out var tokenElement) ||
                tokenElement.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(tokenElement.GetString()))
            {
                return AuthReadResult.Failure(AuthReadError.TokenMissing, "access_token がありません");
            }

            return AuthReadResult.Success(tokenElement.GetString()!);
        }
        catch (JsonException)
        {
            return AuthReadResult.Failure(AuthReadError.InvalidJson, "auth.json が不正です");
        }
        catch (IOException ex)
        {
            return AuthReadResult.Failure(AuthReadError.IoError, ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return AuthReadResult.Failure(AuthReadError.IoError, ex.Message);
        }
    }
}
