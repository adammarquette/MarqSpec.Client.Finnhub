using System.Text.Json;

namespace MarqSpec.Client.Finnhub;

/// <summary>The Finnhub websocket trade feed.</summary>
public interface IFinnhubQuoteStream : IDisposable
{
    /// <summary>The symbols currently subscribed.</summary>
    IReadOnlyCollection<string> Subscribed { get; }

    /// <summary>Subscribes to a symbol's trades.</summary>
    /// <param name="symbol">The Finnhub symbol.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <exception cref="FinnhubSubscriptionLimitException">The free-tier symbol cap would be exceeded.</exception>
    Task SubscribeAsync(string symbol, CancellationToken cancellationToken);

    /// <summary>Unsubscribes from a symbol.</summary>
    /// <param name="symbol">The Finnhub symbol.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    Task UnsubscribeAsync(string symbol, CancellationToken cancellationToken);

    /// <summary>Reads trades until the token is cancelled, reconnecting and re-subscribing across drops.</summary>
    /// <param name="cancellationToken">Stops the stream.</param>
    /// <returns>Trades as they arrive.</returns>
    IAsyncEnumerable<FinnhubTrade> ReadAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Streams Finnhub trades over a websocket, holding the subscription set so it survives a reconnect.
/// </summary>
/// <remarks>
/// <para>
/// <b>The cap is enforced here, at the call.</b> Finnhub's free tier allows a bounded number of simultaneous
/// symbol subscriptions and simply ignores the excess — so an over-cap subscribe would "succeed" and the symbol
/// would just never produce a tick, discovered later by its absence. Refusing the call turns a silent gap into a
/// loud, immediate failure naming the cap.
/// </para>
/// <para>
/// <b>The subscription set is the client's, not the socket's.</b> A dropped connection loses the server's view of
/// what we wanted; keeping it locally is what lets a reconnect restore it. A reconnect that comes back subscribed
/// to nothing looks perfectly healthy from the outside and delivers no data.
/// </para>
/// </remarks>
public sealed class FinnhubQuoteStream : IFinnhubQuoteStream
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly Func<IFinnhubWebSocket> _socketFactory;
    private readonly FinnhubOptions _options;
    private readonly HashSet<string> _subscribed = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _gate = new(1, 1);

    private IFinnhubWebSocket? _socket;
    private bool _disposed;

    /// <summary>Creates the stream.</summary>
    /// <param name="socketFactory">Builds a fresh transport — called again on every reconnect.</param>
    /// <param name="options">Finnhub configuration, including the token and the symbol cap.</param>
    /// <exception cref="InvalidOperationException">No API token is configured.</exception>
    public FinnhubQuoteStream(Func<IFinnhubWebSocket> socketFactory, FinnhubOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _socketFactory = socketFactory;
        _options = options;

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("Finnhub API token is not configured (FinnhubOptions.ApiKey).");
        }
    }

    /// <inheritdoc />
    public IReadOnlyCollection<string> Subscribed
    {
        get
        {
            lock (_subscribed)
            {
                return [.. _subscribed];
            }
        }
    }

    /// <inheritdoc />
    public async Task SubscribeAsync(string symbol, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        lock (_subscribed)
        {
            // Already subscribed is a no-op, not a cap consumer -- otherwise a caller re-subscribing on its own
            // reconnect logic would spend cap it already holds.
            if (!_subscribed.Contains(symbol) && _subscribed.Count >= _options.MaxSubscribedSymbols)
            {
                throw new FinnhubSubscriptionLimitException(_options.MaxSubscribedSymbols, symbol);
            }

            _subscribed.Add(symbol);
        }

        if (_socket is { IsOpen: true })
        {
            await SendSubscribeAsync(_socket, symbol, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task UnsubscribeAsync(string symbol, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        lock (_subscribed)
        {
            _subscribed.Remove(symbol);
        }

        if (_socket is { IsOpen: true })
        {
            await _socket.SendAsync(
                JsonSerializer.Serialize(new { type = "unsubscribe", symbol }, JsonOptions), cancellationToken);
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<FinnhubTrade> ReadAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            IFinnhubWebSocket socket = await ConnectAndResubscribeAsync(cancellationToken);

            while (!cancellationToken.IsCancellationRequested)
            {
                string? frame;
                try
                {
                    frame = await socket.ReceiveAsync(cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    yield break;
                }

                // A null frame is a closed socket. Break to the outer loop, which reconnects AND re-subscribes --
                // the whole reason the subscription set lives on this object.
                if (frame is null)
                {
                    break;
                }

                foreach (FinnhubTrade trade in ParseTrades(frame))
                {
                    yield return trade;
                }
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _socket?.Dispose();
        _gate.Dispose();
    }

    private static IEnumerable<FinnhubTrade> ParseTrades(string frame)
    {
        FinnhubTradeEnvelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<FinnhubTradeEnvelope>(frame, JsonOptions);
        }
        catch (JsonException)
        {
            // Finnhub interleaves "ping" and error frames with data. A frame we cannot read is not a reason to
            // tear down a working stream.
            yield break;
        }

        if (envelope?.Data is null || !string.Equals(envelope.Type, "trade", StringComparison.Ordinal))
        {
            yield break;
        }

        foreach (FinnhubTradePayload payload in envelope.Data)
        {
            if (string.IsNullOrWhiteSpace(payload.S))
            {
                continue;
            }

            yield return new FinnhubTrade(
                payload.S, payload.P, payload.V, DateTimeOffset.FromUnixTimeMilliseconds(payload.T));
        }
    }

    private async Task<IFinnhubWebSocket> ConnectAndResubscribeAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            _socket?.Dispose();
            _socket = _socketFactory();

            Uri endpoint = new($"{_options.WebsocketUrl}?token={Uri.EscapeDataString(_options.ApiKey!)}");
            await _socket.ConnectAsync(endpoint, cancellationToken);

            // THE reconnect obligation. The server knows nothing about what we wanted before the drop.
            string[] symbols;
            lock (_subscribed)
            {
                symbols = [.. _subscribed];
            }

            foreach (string symbol in symbols)
            {
                await SendSubscribeAsync(_socket, symbol, cancellationToken);
            }

            return _socket;
        }
        finally
        {
            _gate.Release();
        }
    }

    private static Task SendSubscribeAsync(IFinnhubWebSocket socket, string symbol, CancellationToken cancellationToken) =>
        socket.SendAsync(JsonSerializer.Serialize(new { type = "subscribe", symbol }, JsonOptions), cancellationToken);

    private sealed record FinnhubTradeEnvelope(string? Type, IReadOnlyList<FinnhubTradePayload>? Data);

    // Finnhub's wire shape: single-letter fields. Kept internal so the abbreviations never leave this file.
    private sealed record FinnhubTradePayload(string? S, decimal P, decimal V, long T);
}
