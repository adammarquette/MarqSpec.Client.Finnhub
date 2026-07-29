using System.Net.Http.Json;
using System.Text.Json;

namespace MarqSpec.Client.Finnhub;

/// <summary>The Finnhub news REST surface.</summary>
public interface IFinnhubNewsClient
{
    /// <summary>Fetches the latest market news for a category (e.g. <c>general</c>).</summary>
    /// <param name="category">The Finnhub news category.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The articles Finnhub returned, newest first.</returns>
    Task<IReadOnlyList<FinnhubNewsArticle>> GetMarketNewsAsync(string category, CancellationToken cancellationToken);
}

/// <summary>
/// A typed, async client for Finnhub's news REST API. Data-only: it fetches news and nothing else. Errors are the
/// caller's to handle — a non-success status or transport fault surfaces as an exception rather than being
/// swallowed, so a consumer can degrade to another source. Rate-limit responses (HTTP 429) surface the same way.
/// </summary>
/// <remarks>
/// The API token authenticates via the <c>X-Finnhub-Token</c> header, never a query-string parameter — a token
/// in a URL leaks into logs and proxies.
/// </remarks>
public sealed class FinnhubNewsClient : IFinnhubNewsClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly FinnhubOptions _options;

    /// <summary>Creates the client.</summary>
    /// <param name="httpClient">The HTTP client (injected, so timeouts / handlers are the host's to configure).</param>
    /// <param name="options">The Finnhub configuration, including the API token.</param>
    /// <exception cref="InvalidOperationException">No API token is configured.</exception>
    public FinnhubNewsClient(HttpClient httpClient, FinnhubOptions options)
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
    public async Task<IReadOnlyList<FinnhubNewsArticle>> GetMarketNewsAsync(string category, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);

        Uri endpoint = new(new Uri(_options.BaseUrl.TrimEnd('/') + "/"), $"news?category={Uri.EscapeDataString(category)}");

        using HttpRequestMessage request = new(HttpMethod.Get, endpoint);
        request.Headers.Add("X-Finnhub-Token", _options.ApiKey);

        using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        IReadOnlyList<FinnhubNewsArticle>? articles =
            await response.Content.ReadFromJsonAsync<IReadOnlyList<FinnhubNewsArticle>>(JsonOptions, cancellationToken);

        return articles ?? [];
    }
}
