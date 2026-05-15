namespace CodexRateLimitTray.Core;

public enum AuthReadError
{
    None,
    FileNotFound,
    InvalidJson,
    TokenMissing,
    IoError
}

public sealed record AuthReadResult(bool IsSuccess, string? Token, AuthReadError Error, string Message)
{
    public static AuthReadResult Success(string token) => new(true, token, AuthReadError.None, "");

    public static AuthReadResult Failure(AuthReadError error, string message) => new(false, null, error, message);
}
