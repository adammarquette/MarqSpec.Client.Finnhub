using System.Net;
using System.Text;
using FluentAssertions;
using MarqSpec.Client.Finnhub;

namespace MarqSpec.Client.Finnhub.Tests;

/// <summary>
/// The Finnhub news client, driven entirely against a stubbed transport — no API token, no network. Two things it
/// must get right: the token authenticates via a header (never the URL, where it would leak), and a provider
/// error surfaces to the caller rather than being swallowed.
/// </summary>
public class FinnhubNewsClientTests
{
    private const string Token = "finnhub-token-not-a-secret";

    private static FinnhubNewsClient Client(StubHandler handler, string? baseUrl = null) =>
        new(new HttpClient(handler), new FinnhubOptions { ApiKey = Token, BaseUrl = baseUrl ?? "https://finnhub.test/api/v1" });

    private const string TwoArticles = """
        [
          {"category":"general","datetime":1596589501,"headline":"Fed holds rates","id":1,"related":"SPY,QQQ","source":"Reuters","summary":"Rates unchanged.","url":"https://ex.com/a"},
          {"category":"general","datetime":1596600000,"headline":"Crude draws","id":2,"related":"","source":"EIA","summary":"Larger draw.","url":"https://ex.com/b"}
        ]
        """;

    [Fact]
    public async Task GetMarketNewsAsync_ShouldReturnMappedArticles_WhenFinnhubResponds()
    {
        FinnhubNewsClient client = Client(new StubHandler { Body = TwoArticles });

        IReadOnlyList<FinnhubNewsArticle> articles = await client.GetMarketNewsAsync("general", CancellationToken.None);

        articles.Should().HaveCount(2);
        articles[0].Headline.Should().Be("Fed holds rates");
        articles[0].Datetime.Should().Be(1596589501);
        articles[0].Related.Should().Be("SPY,QQQ");
        articles[0].Url.Should().Be("https://ex.com/a");
    }

    [Fact]
    public async Task GetMarketNewsAsync_ShouldSendTheTokenAsAHeader_NeverInTheUrl()
    {
        StubHandler handler = new() { Body = "[]" };

        await Client(handler).GetMarketNewsAsync("general", CancellationToken.None);

        handler.LastRequest!.Headers.GetValues("X-Finnhub-Token").Should().ContainSingle().Which.Should().Be(Token);
        handler.LastRequest.RequestUri!.ToString().Should().NotContain(Token, "a token in the URL leaks into logs and proxies");
        handler.LastRequest.RequestUri.ToString().Should().Contain("category=general");
    }

    [Fact]
    public async Task GetMarketNewsAsync_ShouldReturnEmpty_WhenFinnhubReturnsNull()
    {
        // Finnhub can answer 200 with a literal `null` body; that is "no news", not a crash.
        FinnhubNewsClient client = Client(new StubHandler { Body = "null" });

        (await client.GetMarketNewsAsync("general", CancellationToken.None)).Should().BeEmpty();
    }

    [Fact]
    public async Task GetMarketNewsAsync_ShouldThrow_WhenFinnhubRateLimits()
    {
        FinnhubNewsClient client = Client(new StubHandler { Status = HttpStatusCode.TooManyRequests });

        // The client surfaces a 429 rather than swallowing it, so the consumer can degrade to another source.
        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetMarketNewsAsync("general", CancellationToken.None));
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenNoApiKeyIsConfigured()
    {
        Action act = () => _ = new FinnhubNewsClient(new HttpClient(new StubHandler()), new FinnhubOptions { ApiKey = " " });

        act.Should().Throw<InvalidOperationException>();
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;

        public string Body { get; set; } = "[]";

        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(Status)
            {
                Content = new StringContent(Body, Encoding.UTF8, "application/json"),
            });
        }
    }
}
