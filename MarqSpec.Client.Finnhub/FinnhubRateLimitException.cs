namespace MarqSpec.Client.Finnhub;

/// <summary>
/// Finnhub refused the call because the free tier's rate limit was hit (HTTP 429).
/// </summary>
/// <remarks>
/// A distinct type rather than a status code the caller must pattern-match on: a rate limit is <b>retryable</b> and
/// says nothing about the request being wrong, whereas a 400 or a 401 is permanent and needs an operator. Callers
/// degrade to another source on this one and stop on the others, so the difference has to be expressible without
/// string-matching an exception message.
/// </remarks>
public sealed class FinnhubRateLimitException : Exception
{
    /// <summary>Creates the exception.</summary>
    /// <param name="retryAfter">How long Finnhub asked the caller to wait, when it said.</param>
    public FinnhubRateLimitException(TimeSpan? retryAfter = null)
        : base("Finnhub refused the request: the API rate limit was exceeded (HTTP 429).") =>
        RetryAfter = retryAfter;

    /// <summary>Creates the exception with a message.</summary>
    /// <param name="message">The message.</param>
    public FinnhubRateLimitException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a message and inner exception.</summary>
    /// <param name="message">The message.</param>
    /// <param name="innerException">The cause.</param>
    public FinnhubRateLimitException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Creates the exception.</summary>
    public FinnhubRateLimitException()
        : base("Finnhub refused the request: the API rate limit was exceeded (HTTP 429).")
    {
    }

    /// <summary>How long to wait before retrying, when Finnhub said so via <c>Retry-After</c>.</summary>
    public TimeSpan? RetryAfter { get; }
}
