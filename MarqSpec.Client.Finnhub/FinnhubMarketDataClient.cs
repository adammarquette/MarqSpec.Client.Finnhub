using System.Net;
using System.Text.Json;

namespace MarqSpec.Client.Finnhub;

/// <summary>A point-in-time quote snapshot.</summary>
/// <param name="Symbol">The Finnhub symbol.</param>
/// <param name="Current">The current price.</param>
/// <param name="High">The session high.</param>
/// <param name="Low">The session low.</param>
/// <param name="Open">The session open.</param>
/// <param name="PreviousClose">The previous session's close.</param>
/// <param name="TimestampUtc">When Finnhub computed the snapshot.</param>
public sealed record FinnhubQuote(
    string Symbol,
    decimal Current,
    decimal High,
    decimal Low,
    decimal Open,
    decimal PreviousClose,
    DateTimeOffset TimestampUtc);

/// <summary>The Finnhub market-data REST surface — the snapshot the websocket cannot give you.</summary>
public interface IFinnhubMarketDataClient
{
    /// <summary>Fetches the current quote snapshot for a symbol.</summary>
    /// <param name="symbol">The Finnhub symbol.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The snapshot.</returns>
    /// <exception cref="FinnhubRateLimitException">The free-tier rate limit was exceeded.</exception>
    Task<FinnhubQuote> GetQuoteAsync(string symbol, CancellationToken cancellationToken);
}

/// <summary>
/// A typed, async client for Finnhub's quote REST API. Data-only: quotes, nothing else — no account, no execution.
/// </summary>
/// <remarks>
/// <b>A rate limit is a distinct outcome, not a status code.</b> HTTP 429 surfaces as
/// <see cref="FinnhubRateLimitException"/> — retryable, and nothing to do with the request being wrong — while a
/// 4xx/5xx surfaces as the ordinary <see cref="HttpRequestException"/>. A consumer degrades to another source on
/// the first and stops on the second, and it should not have to string-match a message to tell them apart.
/// </remarks>
public sealed class FinnhubMarketDataClient : IFinnhubMarketDataClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly FinnhubOptions _options;

    /// <summary>Creates the client.</summary>
    /// <param name="httpClient">The HTTP client (injected, so timeouts / handlers are the host's to configure).</param>
    /// <param name="options">The Finnhub configuration, including the API token.</param>
    /// <exception cref="InvalidOperationException">No API token is configured.</exception>
    public FinnhubMarketDataClient(HttpClient httpClient, FinnhubOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _httpClient = httpClient;
        _options = options;

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("Finnhub API token is not configured (FinnhubOptions.ApiKey).");
        }
    }

    /// <inheritdoc />
    public async Task<FinnhubQuote> GetQuoteAsync(string symbol, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        Uri endpoint = new(
            new Uri(_options.BaseUrl.TrimEnd('/') + "/"), $"quote?symbol={Uri.EscapeDataString(symbol)}");

        using HttpRequestMessage request = new(HttpMethod.Get, endpoint);
        request.Headers.Add("X-Finnhub-Token", _options.ApiKey);

        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            throw new FinnhubRateLimitException(response.Headers.RetryAfter?.Delta);
        }

        response.EnsureSuccessStatusCode();

        await using Stream body = await response.Content.ReadAsStreamAsync(cancellationToken);
        QuotePayload? payload = await JsonSerializer.DeserializeAsync<QuotePayload>(
            body, JsonOptions, cancellationToken);

        return payload is null
            ? throw new HttpRequestException($"Finnhub returned no quote body for '{symbol}'.")
            : new FinnhubQuote(
                symbol, payload.C, payload.H, payload.L, payload.O, payload.Pc,
                DateTimeOffset.FromUnixTimeSeconds(payload.T));
    }

    // Finnhub's wire shape: single-letter fields. Kept private so the abbreviations never leave this file.
    private sealed record QuotePayload(decimal C, decimal H, decimal L, decimal O, decimal Pc, long T);
}
