# MarqSpec.Client.Finnhub

A .NET client library for the **Finnhub news REST API** — a **data-only** news source.

> **Status: scaffolding.** No implementation yet — this repo currently holds the requirements only. Start at
> [`PRD.md`](PRD.md); the layout below is the plan, not the present.

## What this is

A typed, async .NET client for **Finnhub's news surface** — the market/company news REST endpoints — returning
raw provider payloads for a consumer to normalize. It is **data-only**: it discovers and fetches news, and does
**not** place orders, hold accounts, or execute anything. That is the point of the R-17 split between an
*execution venue* (ProjectX) and a *data source* (this).

It is a **sibling** of [`MarqSpec.Client.ProjectX`](https://github.com/adammarquette/MarqSpec.Client.ProjectX)
and [`MarqSpec.Client.Tradovate`](https://github.com/adammarquette/MarqSpec.Client.Tradovate) — parallel in shape
and convention, deliberately **different in signatures**. The clients must **not** share a public interface; the
venue-neutral symmetry lives in the consumer's `INewsSource` / `ITradingVenue` seams, not here.

**Scope note.** Finnhub also exposes an equities/indices **market-data** surface (websocket cross-asset context,
SPY ↔ ES). That is a **separate concern** and **not** part of this repo's first cut — see the trading-copilot
issue. This client covers **news** only.

**Tracking issue:** [`adammarquette/trading-copilot#383`](https://github.com/adammarquette/trading-copilot/issues/383)

## Consumed by

The [trading-copilot](https://github.com/adammarquette/trading-copilot) pins this repo as a git submodule under
`external/` and wraps it in a `FinnhubNewsSource : INewsSource` adapter (in a `.Integration.Finnhub` project),
which translates Finnhub's payload into the consumer's venue-neutral `NewsItem`. **Free-tier data quality is
flagged unverified** by the consumer's engineering guide, so the first live pass is also the first real check of
it.

## Planned layout

```
MarqSpec.Client.Finnhub/
  MarqSpec.Client.Finnhub/          # the client library (net10.0)
    FinnhubNewsClient.cs            # the typed REST client — GetCompanyNewsAsync / GetMarketNewsAsync
    FinnhubOptions.cs              # API key + base URL; key from config/env, never in source
    Models/                        # the raw payload records (news article, category)
  MarqSpec.Client.Finnhub.sln
  PRD.md · README.md · LICENSE
```

## Why a separate repo

Vendored client code lives outside the consumer's `Directory.Build.props` (net10-only, warnings-as-errors), so a
third-party client is not forced to satisfy the app's house rules, and its release cadence is its own. This is
the established venue-client pattern (ProjectX, Tradovate, Webull).

## License

MIT — see [`LICENSE`](LICENSE).
