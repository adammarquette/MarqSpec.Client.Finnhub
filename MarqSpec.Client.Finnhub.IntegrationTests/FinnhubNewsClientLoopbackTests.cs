using System.Net;
using System.Net.Sockets;
using System.Text;
using FluentAssertions;
using MarqSpec.Client.Finnhub;

namespace MarqSpec.Client.Finnhub.IntegrationTests;

/// <summary>
/// News REST against a loopback listener — no Finnhub credential, no public network.
/// Asserts what the listener recorded (R-8: token is a header, never a URL).
/// </summary>
public sealed class FinnhubNewsClientLoopbackTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetMarketNewsAsync_ShouldSendTokenAsHeader_NeverInTheUrl()
    {
        using HttpListener listener = StartListener(out string prefix);
        Task<RecordedRequest> serve = ServeOnceAsync(listener, "[]");

        FinnhubNewsClient client = new(
            new HttpClient(),
            new FinnhubOptions { ApiKey = "loopback-token-not-a-secret", BaseUrl = prefix.TrimEnd('/') });

        try
        {
            IReadOnlyList<FinnhubNewsArticle> articles =
                await client.GetMarketNewsAsync("general", CancellationToken.None);

            articles.Should().BeEmpty();

            RecordedRequest seen = await serve;
            seen.TokenHeader.Should().Be("loopback-token-not-a-secret");
            seen.RawUrl.Should().NotContain("loopback-token-not-a-secret");
            seen.RawUrl.Should().Contain("category=general");
        }
        finally
        {
            listener.Stop();
        }
    }

    private static HttpListener StartListener(out string prefix)
    {
        TcpListener probe = new(IPAddress.Loopback, 0);
        probe.Start();
        int port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();

        prefix = $"http://127.0.0.1:{port}/";
        HttpListener listener = new();
        listener.Prefixes.Add(prefix);
        listener.Start();
        return listener;
    }

    private static async Task<RecordedRequest> ServeOnceAsync(HttpListener listener, string body)
    {
        HttpListenerContext context = await listener.GetContextAsync();
        string? token = context.Request.Headers["X-Finnhub-Token"];
        string rawUrl = context.Request.RawUrl ?? string.Empty;

        byte[] bytes = Encoding.UTF8.GetBytes(body);
        context.Response.StatusCode = (int)HttpStatusCode.OK;
        context.Response.ContentType = "application/json";
        context.Response.ContentLength64 = bytes.Length;
        await context.Response.OutputStream.WriteAsync(bytes);
        context.Response.Close();

        return new RecordedRequest(token, rawUrl);
    }

    private sealed record RecordedRequest(string? TokenHeader, string RawUrl);
}
