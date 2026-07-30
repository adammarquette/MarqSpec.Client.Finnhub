using System.Collections.Concurrent;

namespace MarqSpec.Client.Finnhub.Tests;

/// <summary>
/// The websocket trade feed (gh#495). Two behaviours carry the weight, and both are silent when wrong: an
/// <b>over-cap subscribe</b> that Finnhub accepts and never delivers, and a <b>reconnect that comes back
/// subscribed to nothing</b> — a healthy-looking socket on a running process, delivering no data.
/// </summary>
public class FinnhubQuoteStreamTests
{
    private static FinnhubOptions Options(int cap = 50) =>
        new() { ApiKey = "token-not-a-secret", WebsocketUrl = "wss://stream.invalid", MaxSubscribedSymbols = cap };

    // --- The free-tier cap, refused at the call ---

    [Fact]
    public async Task SubscribeAsync_ShouldThrowNamingTheCap_WhenItWouldBeExceeded()
    {
        // Finnhub does NOT error on an over-cap subscribe: it accepts the frame and silently sends nothing for
        // that symbol. Passing it through would make a configuration mistake indistinguishable from a quiet
        // market, discovered only by noticing absent data.
        using FinnhubQuoteStream stream = new(() => new FakeSocket(), Options(cap: 2));
        await stream.SubscribeAsync("SPY", CancellationToken.None);
        await stream.SubscribeAsync("QQQ", CancellationToken.None);

        FinnhubSubscriptionLimitException error = await Assert.ThrowsAsync<FinnhubSubscriptionLimitException>(
            () => stream.SubscribeAsync("IWM", CancellationToken.None));

        Assert.Equal(2, error.Cap);
        Assert.Equal("IWM", error.Symbol);
        Assert.Contains("2", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SubscribeAsync_ShouldNotSpendCap_WhenTheSymbolIsAlreadySubscribed()
    {
        // Re-subscribing an existing symbol must be a no-op rather than a cap consumer -- a caller with its own
        // retry loop would otherwise exhaust the cap on symbols it already holds.
        using FinnhubQuoteStream stream = new(() => new FakeSocket(), Options(cap: 1));
        await stream.SubscribeAsync("SPY", CancellationToken.None);

        await stream.SubscribeAsync("SPY", CancellationToken.None);

        Assert.Single(stream.Subscribed);
    }

    [Fact]
    public async Task UnsubscribeAsync_ShouldReturnCap_SoTheSlotIsReusable()
    {
        using FinnhubQuoteStream stream = new(() => new FakeSocket(), Options(cap: 1));
        await stream.SubscribeAsync("SPY", CancellationToken.None);

        await stream.UnsubscribeAsync("SPY", CancellationToken.None);
        await stream.SubscribeAsync("QQQ", CancellationToken.None);

        Assert.Equal(["QQQ"], stream.Subscribed);
    }

    // --- The reconnect obligation ---

    [Fact]
    public async Task ReadAsync_ShouldResubscribeEverySymbol_WhenTheSocketDropsAndReconnects()
    {
        // THE guard. The server knows nothing about what we wanted before the drop, so the subscription set has to
        // live on the client and be replayed. A reconnect that comes back subscribed to nothing is invisible from
        // outside: the process is up, the socket is open, and no tick ever arrives.
        ConcurrentQueue<FakeSocket> sockets = new();
        FakeSocket First() => new(closeAfterFrames: 1, frames: [Trade("SPY", 500.25m)]);
        FakeSocket Second() => new(closeAfterFrames: 1, frames: [Trade("SPY", 500.50m)]);

        bool first = true;
        using FinnhubQuoteStream stream = new(
            () =>
            {
                FakeSocket socket = first ? First() : Second();
                first = false;
                sockets.Enqueue(socket);
                return socket;
            },
            Options());

        await stream.SubscribeAsync("SPY", CancellationToken.None);
        await stream.SubscribeAsync("QQQ", CancellationToken.None);

        using CancellationTokenSource stop = new();
        List<FinnhubTrade> seen = [];
        await foreach (FinnhubTrade trade in stream.ReadAsync(stop.Token))
        {
            seen.Add(trade);
            if (seen.Count == 2)
            {
                await stop.CancelAsync();
            }
        }

        // Two sockets were built -- the drop forced a reconnect -- and BOTH received a subscribe for BOTH symbols.
        FakeSocket[] built = [.. sockets];
        Assert.Equal(2, built.Length);
        Assert.All(built, socket =>
        {
            Assert.Contains(socket.Sent, frame => frame.Contains("\"symbol\":\"SPY\"", StringComparison.Ordinal));
            Assert.Contains(socket.Sent, frame => frame.Contains("\"symbol\":\"QQQ\"", StringComparison.Ordinal));
        });
    }

    [Fact]
    public async Task ReadAsync_ShouldKeepStreaming_WhenAFrameCannotBeParsed()
    {
        // Finnhub interleaves ping and error frames with data. A frame we cannot read must not tear down a working
        // stream -- that would turn a cosmetic wire event into an outage.
        using FinnhubQuoteStream stream = new(
            () => new FakeSocket(closeAfterFrames: 2, frames: ["{not json", Trade("SPY", 501m)]),
            Options());
        await stream.SubscribeAsync("SPY", CancellationToken.None);

        using CancellationTokenSource stop = new();
        List<FinnhubTrade> seen = [];
        await foreach (FinnhubTrade trade in stream.ReadAsync(stop.Token))
        {
            seen.Add(trade);
            await stop.CancelAsync();
        }

        Assert.Single(seen);
        Assert.Equal(501m, seen[0].Price);
    }

    [Fact]
    public void Constructor_ShouldRefuse_WhenNoTokenIsConfigured()
    {
        FinnhubOptions keyless = new() { ApiKey = null };

        Assert.Throws<InvalidOperationException>(() => new FinnhubQuoteStream(() => new FakeSocket(), keyless));
    }

    private static string Trade(string symbol, decimal price) =>
        $$"""{"type":"trade","data":[{"s":"{{symbol}}","p":{{price}},"v":10,"t":1767225600000}]}""";

    /// <summary>A websocket that yields a scripted set of frames, then reports closed.</summary>
    private sealed class FakeSocket : IFinnhubWebSocket
    {
        private readonly Queue<string> _frames;
        private readonly int _closeAfterFrames;
        private int _delivered;
        private bool _closed;

        public FakeSocket(int closeAfterFrames = 0, params string[] frames)
        {
            _frames = new Queue<string>(frames);
            _closeAfterFrames = closeAfterFrames;
        }

        public List<string> Sent { get; } = [];

        public bool IsOpen { get; private set; }

        public Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
        {
            IsOpen = true;
            return Task.CompletedTask;
        }

        public Task SendAsync(string text, CancellationToken cancellationToken)
        {
            Sent.Add(text);
            return Task.CompletedTask;
        }

        public async Task<string?> ReceiveAsync(CancellationToken cancellationToken)
        {
            if (_delivered < _closeAfterFrames && _frames.Count > 0)
            {
                _delivered++;
                return _frames.Dequeue();
            }

            // Signal the close EXACTLY once. That null is what drives the stream to reconnect -- returning it
            // repeatedly would spin new sockets forever, and never returning it would hang the test instead of
            // exercising the reconnect at all.
            if (!_closed)
            {
                _closed = true;
                IsOpen = false;
                return null;
            }

            // Afterwards idle until the caller cancels: a hot loop here would burn a core and turn a failing test
            // into a hanging one.
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return null;
        }

        public void Dispose() => IsOpen = false;
    }
}
