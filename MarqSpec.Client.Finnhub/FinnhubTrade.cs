namespace MarqSpec.Client.Finnhub;

/// <summary>One trade print from Finnhub's websocket feed.</summary>
/// <param name="Symbol">The Finnhub symbol (e.g. <c>SPY</c>, <c>BINANCE:BTCUSDT</c>).</param>
/// <param name="Price">The trade price. <see cref="decimal"/>, never a float — a price is money.</param>
/// <param name="Volume">The trade size.</param>
/// <param name="TimestampUtc">When the trade occurred, from Finnhub's millisecond epoch.</param>
public sealed record FinnhubTrade(string Symbol, decimal Price, decimal Volume, DateTimeOffset TimestampUtc);
