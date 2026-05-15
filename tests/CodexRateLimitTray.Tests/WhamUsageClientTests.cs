using System.Net;
using CodexRateLimitTray.Core;

namespace CodexRateLimitTray.Tests;

public sealed class WhamUsageClientTests
{
    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, UsageErrorKind.Authentication)]
    [InlineData(HttpStatusCode.Forbidden, UsageErrorKind.Authentication)]
    [InlineData(HttpStatusCode.InternalServerError, UsageErrorKind.Server)]
    public async Task Classifies_http_errors(HttpStatusCode statusCode, UsageErrorKind expected)
    {
        using var http = new HttpClient(new StubHandler(new HttpResponseMessage(statusCode)));
        var client = new WhamUsageClient(http, TimeZoneInfo.Utc);

        var state = await client.GetUsageAsync("token", CancellationToken.None);

        Assert.True(state.HasError);
        Assert.Equal(expected, state.ErrorKind);
    }

    [Fact]
    public async Task Sends_bearer_token_to_wham_usage_endpoint()
    {
        HttpRequestMessage? captured = null;
        using var http = new HttpClient(new StubHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"rate_limit":{"primary_window":{"used_percent":1,"reset_at":1},"secondary_window":{"used_percent":2,"reset_at":2}}}""")
        }, request => captured = request));
        var client = new WhamUsageClient(http, TimeZoneInfo.Utc);

        await client.GetUsageAsync("abc", CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal("https://chatgpt.com/backend-api/wham/usage", captured!.RequestUri!.ToString());
        Assert.Equal("Bearer", captured.Headers.Authorization!.Scheme);
        Assert.Equal("abc", captured.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task Timeout_is_reported_as_network_error()
    {
        using var http = new HttpClient(new ThrowingHandler(new TimeoutException()));
        var client = new WhamUsageClient(http, TimeZoneInfo.Utc);

        var state = await client.GetUsageAsync("token", CancellationToken.None);

        Assert.True(state.HasError);
        Assert.Equal(UsageErrorKind.Network, state.ErrorKind);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;
        private readonly Action<HttpRequestMessage>? _onRequest;

        public StubHandler(HttpResponseMessage response, Action<HttpRequestMessage>? onRequest = null)
        {
            _response = response;
            _onRequest = onRequest;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _onRequest?.Invoke(request);
            return Task.FromResult(_response);
        }
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        private readonly Exception _exception;

        public ThrowingHandler(Exception exception)
        {
            _exception = exception;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromException<HttpResponseMessage>(_exception);
        }
    }
}
