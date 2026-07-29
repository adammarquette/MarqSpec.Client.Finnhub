using System.Text.Json.Serialization;

namespace MarqSpec.Client.Finnhub;

/// <summary>
/// One news article as Finnhub's <c>/news</c> and <c>/company-news</c> endpoints return it — the raw provider
/// shape, before any normalization. A consumer maps this to its own domain type.
/// </summary>
/// <param name="Category">Finnhub's category tag (e.g. <c>general</c>, <c>forex</c>).</param>
/// <param name="Datetime">Publication time as a Unix epoch in <b>seconds</b>.</param>
/// <param name="Headline">The article headline.</param>
/// <param name="Id">Finnhub's numeric article id.</param>
/// <param name="Image">A URL to the article image, if any.</param>
/// <param name="Related">Comma-separated tickers Finnhub tagged (often empty for general news).</param>
/// <param name="Source">The publishing source (e.g. <c>Reuters</c>).</param>
/// <param name="Summary">The article summary / body text.</param>
/// <param name="Url">The article URL.</param>
public sealed record FinnhubNewsArticle(
    [property: JsonPropertyName("category")] string? Category,
    [property: JsonPropertyName("datetime")] long Datetime,
    [property: JsonPropertyName("headline")] string? Headline,
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("image")] string? Image,
    [property: JsonPropertyName("related")] string? Related,
    [property: JsonPropertyName("source")] string? Source,
    [property: JsonPropertyName("summary")] string? Summary,
    [property: JsonPropertyName("url")] string? Url);
