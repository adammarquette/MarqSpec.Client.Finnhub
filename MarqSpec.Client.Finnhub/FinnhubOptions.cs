namespace MarqSpec.Client.Finnhub;

/// <summary>
/// Configuration for the Finnhub clients — the news REST surface, the market-data REST surface, and the websocket
/// trade feed. The API token is supplied by the caller and sourced from its own configuration / environment; it is
/// never hard-coded here.
/// </summary>
public sealed class FinnhubOptions
{
    /// <summary>The Finnhub API token. Required; sent as the <c>X-Finnhub-Token</c> header, never in the URL.</summary>
    public string? ApiKey { get; set; }

    /// <summary>The API base address. Overridable for testing; defaults to Finnhub's v1 REST base.</summary>
    public string BaseUrl { get; set; } = "https://finnhub.io/api/v1";

    /// <summary>The websocket endpoint. Overridable for testing; defaults to Finnhub's v1 stream.</summary>
    public string WebsocketUrl { get; set; } = "wss://ws.finnhub.io";

    /// <summary>
    /// How many symbols may be subscribed at once.
    /// </summary>
    /// <remarks>
    /// Finnhub's free tier caps simultaneous subscriptions and <b>silently ignores the excess</b> — an over-cap
    /// subscribe is accepted and then never delivers a tick. The client refuses past this instead, so the mistake
    /// surfaces at the call rather than as data that quietly never arrives.
    /// </remarks>
    public int MaxSubscribedSymbols { get; set; } = 50;
}
