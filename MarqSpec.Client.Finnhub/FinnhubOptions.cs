namespace MarqSpec.Client.Finnhub;

/// <summary>
/// Configuration for the Finnhub news client. The API token is supplied by the caller and sourced from its own
/// configuration / environment — it is never hard-coded here.
/// </summary>
public sealed class FinnhubOptions
{
    /// <summary>The Finnhub API token. Required; sent as the <c>X-Finnhub-Token</c> header, never in the URL.</summary>
    public string? ApiKey { get; set; }

    /// <summary>The API base address. Overridable for testing; defaults to Finnhub's v1 REST base.</summary>
    public string BaseUrl { get; set; } = "https://finnhub.io/api/v1";
}
