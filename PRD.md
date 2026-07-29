# PRD — MarqSpec.Client.Finnhub

**Status: requirements only.** This document is the plan; the repo holds no implementation yet
(scaffolding, per the venue-client pattern). Tracking issue:
[`adammarquette/trading-copilot#383`](https://github.com/adammarquette/trading-copilot/issues/383).

## Purpose

A typed, async .NET client for **Finnhub's news REST API**. Data-only: it fetches news; it never trades. It is
consumed by the [trading-copilot](https://github.com/adammarquette/trading-copilot) as one of two news sources
(the other is [`MarqSpec.Client.Tiingo`](https://github.com/adammarquette/MarqSpec.Client.Tiingo)), fanned in and
deduped behind the consumer's `INewsSource` seam.

## Scope

- **News only.** `GET /news?category=general` (market news) and `GET /company-news?symbol=&from=&to=` (per-symbol
  news). The equities/indices **quote** surface (websocket cross-asset context) is explicitly **out of scope** for
  this repo's first cut.
- **Read-only.** No mutating calls exist on this surface, and none may be added.
- **Typed payloads.** Each endpoint returns strongly-typed records mirroring Finnhub's JSON — the consumer's
  adapter maps them to its venue-neutral `NewsItem`; this client does no normalization or dedup of its own.

## Non-goals

- Order placement, accounts, positions — this is a data source, not an execution venue (R-17).
- Dedup / relevance / storage — those belong to the consumer, not the client.
- Sharing a public interface with the ProjectX / Tiingo clients — the venue-neutral symmetry lives in the
  consumer's seams. Parallel in convention, distinct in signatures.

## Requirements

- **`net10.0`**, async-all-the-way with `CancellationToken`, `HttpClient` injected (not newed), typed via
  `System.Text.Json` source-gen or `JsonSerializerDefaults.Web`.
- **Auth from configuration** — the API token is supplied by the caller (`FinnhubOptions`), sourced from the
  consumer's config/environment. **No secret is ever committed here.**
- **Free-tier aware.** Finnhub's free tier is rate-limited (≈60 calls/min) and its **data quality is unverified**
  — the client surfaces provider errors and rate-limit responses to the caller rather than swallowing them, so
  the consumer can degrade to the other source.
- **Errors are the caller's to handle.** Transport faults and non-success statuses surface as typed
  exceptions/results; the client does not retry silently or block.

## Relationship to the sibling clients

`MarqSpec.Client.ProjectX` (execution + market data) and `MarqSpec.Client.Tradovate` (execution) are the
execution-venue siblings; `MarqSpec.Client.Tiingo` is the other data-only news sibling. All follow the same
**convention** (typed, async, injected `HttpClient`, config-sourced auth, no shared public interface) so the
consumer's adapters are parallel in shape — but each client's signatures are its own, mirroring its provider's
actual API rather than a forced abstraction.
