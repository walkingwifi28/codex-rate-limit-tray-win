using System.Net;
using System.Net.Http.Headers;

namespace CodexRateLimitTray.Core;

public sealed class WhamUsageClient
{
    public static readonly Uri UsageUri = new("https://chatgpt.com/backend-api/wham/usage");

    private readonly HttpClient _httpClient;
    private readonly TimeZoneInfo _localTimeZone;

    public WhamUsageClient(HttpClient httpClient, TimeZoneInfo localTimeZone)
    {
        _httpClient = httpClient;
        _localTimeZone = localTimeZone;
    }

    public async Task<UsageState> GetUsageAsync(string accessToken, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, UsageUri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return UsageState.Error(ClassifyStatusCode(response.StatusCode), ToStatusMessage(response.StatusCode));
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return WhamUsageParser.Parse(json, _localTimeZone);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or TimeoutException)
        {
            return UsageState.Error(UsageErrorKind.Network, "ネットワークエラー");
        }
    }

    private static UsageErrorKind ClassifyStatusCode(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => UsageErrorKind.Authentication,
            _ => (int)statusCode >= 500 ? UsageErrorKind.Server : UsageErrorKind.Network
        };
    }

    private static string ToStatusMessage(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "認証できません",
            _ when (int)statusCode >= 500 => "サーバーエラー",
            _ => $"HTTP {(int)statusCode}"
        };
    }
}
