# PRD — MarqSpec.Client.Finnhub

**Status: news REST and the market-data surface have shipped.** Company-news (per-symbol) has not.
Tracking issues: [`trading-copilot#383`](https://github.com/adammarquette/trading-copilot/issues/383) (scaffold),
[`#439`](https://github.com/adammarquette/trading-copilot/issues/439) (news flow),
[`#495`](https://github.com/adammarquette/trading-copilot/issues/495) (market-data),
[`#1168`](https://github.com/adammarquette/trading-copilot/issues/1168) (repo-template backfill, of
[`#701`](https://github.com/adammarquette/trading-copilot/issues/701)).

Ids are appended, never renumbered.

## Purpose

A typed, async .NET client for **Finnhub's news REST API** and **equities/indices market-data**. Data-only: it
fetches news and prices; it never trades. It is consumed by
[trading-copilot](https://github.com/adammarquette/trading-copilot) as a news source (alongside
[`MarqSpec.Client.Tiingo`](https://github.com/adammarquette/MarqSpec.Client.Tiingo)) and as the single-source
equities/indices price feed behind the consumer's `IContextMarketDataSource` seam.

## Scope

- **News.** `GET /news?category=general` (market news). `GET /company-news?symbol=&from=&to=` (per-symbol news)
  is required (**R-2**) and not yet built.
- **Market data.** Quote REST snapshot and a trade websocket for equities/indices (SPY, QQQ, …) used as
  cross-asset context. Shipped with gh#495.
- **Read-only.** No mutating calls exist on this surface, and none may be added.
- **Typed payloads.** Each endpoint returns strongly-typed records mirroring Finnhub's JSON — the consumer's
  adapter maps them to its venue-neutral types; this client does no normalization or dedup of its own.

## Non-goals

- Order placement, accounts, positions — this is a data source, not an execution venue.
- Dedup / relevance / storage — those belong to the consumer, not the client.
- Sharing a public interface with the ProjectX / Tiingo clients — the venue-neutral symmetry lives in the
  consumer's seams. Parallel in convention, distinct in signatures.

## Requirements

- **R-1 News REST.** `GetMarketNewsAsync` fetches general market news over REST. Shipped.
- **R-2 Company news.** `GetCompanyNewsAsync` (per-symbol, dated range). Not built.
- **R-3 Market-data snapshot.** `GetQuoteAsync` returns a typed quote. Shipped (gh#495).
- **R-4 Trade stream.** `FinnhubQuoteStream` over `IFinnhubWebSocket` delivers trades. Shipped (gh#495).
- **R-5 Read-only.** The public surface exposes no mutating HTTP verb and no order/account type.
- **R-6 Typed payloads.** Records mirror Finnhub's JSON; the consumer normalizes.
- **R-7 net10.0, async, injected transport.** `CancellationToken` on every public async method; `HttpClient`
  injected (not newed); JSON via `System.Text.Json`.
- **R-8 Auth from configuration.** The API token is supplied by the caller (`FinnhubOptions`), sourced from the
  consumer's config/environment. **No secret is ever committed here.** The token travels as `X-Finnhub-Token`,
  never a query-string parameter.
- **R-9 Free-tier aware.** Rate-limit responses surface as typed errors (`FinnhubRateLimitException` on the
  quote path; HTTP 429 on news) rather than being swallowed, so the consumer can degrade.
- **R-10 Errors are the caller's to handle.** Transport faults and non-success statuses surface; the client
  does not retry silently or block.

## Relationship to the sibling clients

`MarqSpec.Client.ProjectX` (execution + market data) and `MarqSpec.Client.Tradovate` (execution) are the
execution-venue siblings; `MarqSpec.Client.Tiingo` is the other data-only news sibling. All follow the same
**convention** (typed, async, injected transport, config-sourced auth, no shared public interface) so the
consumer's adapters are parallel in shape — but each client's signatures are its own, mirroring its provider's
actual API rather than a forced abstraction.
