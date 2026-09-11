# MarqSpec.Client.Finnhub

A typed, async .NET client for **Finnhub's news REST API** and **equities/indices market-data** (quote REST +
trade websocket). It is **data-only**: it discovers news and prices, and does **not** place orders, hold
accounts, or execute anything.

> **Status:** news REST (`GetMarketNewsAsync`) and the market-data surface (quote REST + trade websocket) have
> shipped. Company-news (per-symbol) is not built yet.

## What this is

A sibling of [`MarqSpec.Client.ProjectX`](https://github.com/adammarquette/MarqSpec.Client.ProjectX) and
[`MarqSpec.Client.Tradovate`](https://github.com/adammarquette/MarqSpec.Client.Tradovate) — parallel in shape
and convention, deliberately **different in signatures**. The clients must **not** share a public interface; the
venue-neutral symmetry lives in the consumer's `INewsSource` / `IContextMarketDataSource` / `ITradingVenue`
seams, not here.

**Tracking issues:** news scaffold
[`trading-copilot#383`](https://github.com/adammarquette/trading-copilot/issues/383), news flow
[`#439`](https://github.com/adammarquette/trading-copilot/issues/439), market-data surface
[`#495`](https://github.com/adammarquette/trading-copilot/issues/495). Repo standards backfill:
[`#1168`](https://github.com/adammarquette/trading-copilot/issues/1168) (of
[`#701`](https://github.com/adammarquette/trading-copilot/issues/701)).

## Consumed by

The [trading-copilot](https://github.com/adammarquette/trading-copilot) pins this repo as a git submodule under
`external/` and wraps it in `.Integration.Finnhub` adapters (`FinnhubNewsSource`, `FinnhubMarketDataSource`).
**Free-tier data quality is flagged unverified** by the consumer's engineering guide, so a live pass is also a
real check of it.

## Layout

```
MarqSpec.Client.Finnhub/
  MarqSpec.Client.Finnhub/                 # the client library (net10.0)
    FinnhubNewsClient.cs                   # news REST — GetMarketNewsAsync
    FinnhubMarketDataClient.cs             # quote REST — GetQuoteAsync
    FinnhubQuoteStream.cs                  # trade websocket over IFinnhubWebSocket
    FinnhubOptions.cs                      # API key + base URL; key from config/env, never in source
    FinnhubNewsArticle.cs · FinnhubTrade.cs
  MarqSpec.Client.Finnhub.Tests/           # stubbed transport, no key/network
  MarqSpec.Client.Finnhub.IntegrationTests/  # loopback listener, no credentials
  MarqSpec.Client.Finnhub.slnx
  documentation/                           # PRD, architecture, ADRs, agent contracts
  CONTRIBUTING.md · AGENTS.md · LICENSE
```

**Not yet built:** `GetCompanyNewsAsync` (per-symbol news).

## Build

```bash
dotnet build MarqSpec.Client.Finnhub.slnx
dotnet format MarqSpec.Client.Finnhub.slnx --verify-no-changes
dotnet test MarqSpec.Client.Finnhub.slnx --filter "Category!=Live"
```

How we work: [`CONTRIBUTING.md`](CONTRIBUTING.md). Agent contracts: [`AGENTS.md`](AGENTS.md). Route docs from
[`documentation/README.md`](documentation/README.md).

## Why a separate repo

Vendored client code lives outside the consumer's `Directory.Build.props`, so a third-party client is not forced
to satisfy the app's house rules, and its release cadence is its own. This is the established venue-client
pattern (ProjectX, Tradovate, Webull).

## License

MIT — see [`LICENSE`](LICENSE).
