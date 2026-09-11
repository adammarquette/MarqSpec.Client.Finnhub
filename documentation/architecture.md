# Architecture — MarqSpec.Client.Finnhub

How the library is put together, and why. Requirements are in the [PRD](prd.md) (`R-#`); decisions and their
alternatives are in [`adr/`](adr/README.md).

## The one-paragraph version

A consumer constructs `FinnhubNewsClient` / `FinnhubMarketDataClient` with an injected `HttpClient` and
`FinnhubOptions` (API token + base URL), or `FinnhubQuoteStream` with an `IFinnhubWebSocket`. There is no
registration extension yet — the consumer's adapter owns DI. Auth is the `X-Finnhub-Token` header. Failures
surface; nothing is retried here.

## Composition

```
Consumer adapter (trading-copilot .Integration.Finnhub)
├── FinnhubOptions              section from the host; key from env
├── HttpClient                  host-configured (timeouts, handlers)
├── FinnhubNewsClient           news REST
├── FinnhubMarketDataClient     quote REST
└── FinnhubQuoteStream          trades, over IFinnhubWebSocket
                                (ClientWebSocketTransport is the default)
```

**This library does not register itself.** Lifetimes are the consumer's: the HTTP clients are typically
typed/`AddHttpClient` in the host; the stream is a long-lived subscriber. A lifetime chosen here would sit
below the host's gate and could not be audited there.

## The request path

**News (R-1).** `GetMarketNewsAsync` builds `GET {BaseUrl}/news?category=…`, adds `X-Finnhub-Token`, sends,
`EnsureSuccessStatusCode`, deserializes `FinnhubNewsArticle[]`. A 429 is an `HttpRequestException`.

**Quote (R-3).** `GetQuoteAsync` builds `GET {BaseUrl}/quote?symbol=…`, same header. HTTP 429 is
`FinnhubRateLimitException` — a distinct, retryable outcome — not a generic status.

**Trades (R-4).** `FinnhubQuoteStream` subscribes through `IFinnhubWebSocket`. Crossing Finnhub's free-tier
subscription cap surfaces `FinnhubSubscriptionLimitException`. The default transport is
`ClientWebSocketTransport`; tests substitute a stub.

REST never puts the token in a URL (R-8). The trade websocket does: Finnhub's published handshake is
`{WebsocketUrl}?token=…` on connect. That split is load-bearing — a later change that "makes auth
consistent" by moving the WS token into a header will silently stop ticks.

## Failure semantics

- **Nothing is retried in this library.** A timeout is an unknown outcome the caller owns.
- Rate limits are typed on the quote path and raw HTTP on the news path — that asymmetry is inherited, not
  a new decision. Do not "fix" one without an ADR; the consumer already branches on both.
- A missing API token fails at construction (`InvalidOperationException`), not on the first call.

## The consumer boundary

| This library provides | The consumer provides |
|---|---|
| Transport, serialization, header auth | Policy, limits, orchestration, DI |
| Typed models and errors | `NewsItem` / context-market-data domain types |
| Raw Finnhub payloads | Dedup, relevance, storage |

## Testing shape

| Tier | Project | Backing | Runs |
|---|---|---|---|
| Unit | `MarqSpec.Client.Finnhub.Tests` | stubbed `HttpMessageHandler` / websocket, no I/O | always, in seconds |
| Integration | `MarqSpec.Client.Finnhub.IntegrationTests` | loopback `HttpListener`, no credentials | always |
| Live | same project, `Category=Live` | the real Finnhub API | opt-in only; none yet |

## Known shape issues

- News 429s are not `FinnhubRateLimitException`. Quote 429s are. Consumers must handle both.
- The trade websocket authenticates with `?token=` on the connect URL. REST does not. See R-8.
- There is no `AddFinnhub…` extension. Adding one is a surface change, not a cleanup.
- Company news (R-2) is specified and absent.
- The library is `net10.0` only. Multi-targeting would be a new ADR, not an assumption of the template.
