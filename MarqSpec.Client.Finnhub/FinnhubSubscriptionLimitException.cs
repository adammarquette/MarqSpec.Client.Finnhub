namespace MarqSpec.Client.Finnhub;

/// <summary>
/// A subscribe was refused because it would exceed the configured simultaneous-symbol cap.
/// </summary>
/// <remarks>
/// <b>Refused at the call, deliberately.</b> Finnhub does not error on an over-cap subscribe — it accepts the
/// frame and silently never sends that symbol's trades. Passing it through would turn a configuration mistake into
/// a symbol that is subscribed on paper, absent in practice, and discovered only by noticing missing data. The
/// cap is stated in the message because the fix is always either raising it or subscribing to less.
/// </remarks>
public sealed class FinnhubSubscriptionLimitException : Exception
{
    /// <summary>Creates the exception.</summary>
    /// <param name="cap">The configured simultaneous-symbol cap.</param>
    /// <param name="symbol">The symbol that would have exceeded it.</param>
    public FinnhubSubscriptionLimitException(int cap, string symbol)
        : base($"Refusing to subscribe to '{symbol}': it would exceed the Finnhub simultaneous-symbol cap of {cap}. "
            + "Finnhub accepts an over-cap subscribe and then sends nothing for it, so this fails here instead.")
    {
        Cap = cap;
        Symbol = symbol;
    }

    /// <summary>Creates the exception with a message.</summary>
    /// <param name="message">The message.</param>
    public FinnhubSubscriptionLimitException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a message and inner exception.</summary>
    /// <param name="message">The message.</param>
    /// <param name="innerException">The cause.</param>
    public FinnhubSubscriptionLimitException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Creates the exception.</summary>
    public FinnhubSubscriptionLimitException()
        : base("Refusing to subscribe: it would exceed the Finnhub simultaneous-symbol cap.")
    {
    }

    /// <summary>The configured cap.</summary>
    public int Cap { get; }

    /// <summary>The symbol that would have exceeded it.</summary>
    public string? Symbol { get; }
}
