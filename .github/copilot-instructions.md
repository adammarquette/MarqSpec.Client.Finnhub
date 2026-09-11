# Review checklist — MarqSpec.Client.Finnhub

What to weigh when reviewing a change here. This file is the **substantive** checklist; the
[Code Reviewer contract](../documentation/agents/code-reviewer.md) owns *how* to report, and points here for
*what* to look for. It stays at this path because GitHub's Copilot reviewer reads it.

## Lead with the worst failure this repo can have

> Can this change put the API token in a URL, a log line, or an exception message — or swallow a provider
> error the consumer needs in order to degrade?

This is a public, data-only client. A leaked `X-Finnhub-Token` or a swallowed 429 is the blast radius, not a
duplicated order. Everything else is downstream of that.

## Auth and unknown outcomes

- REST: the token travels as `X-Finnhub-Token`, never a query-string parameter.
- Websocket: Finnhub's handshake is `{WebsocketUrl}?token=…`. That is the published connect path, not a
  leak to "fix" onto a header. A change that removes `?token=` from the connect URL is a finding — it
  breaks the stream. Logging that URI is also a finding.
- **A timeout is not a failure — it is an unknown outcome.** Do not report "no data" for a request that may
  have succeeded.
- This library does not retry. Anything added to a retry set needs a stated reason why resending is safe.

## Fail-closed, not fail-open

- A missing API token fails at construction, not on the first call.
- A `catch` that swallows and returns a default is a fail-open. Rate limits surface; they are not empty news.
- Zero-valued enums arriving from outside need exhaustive handling.

## Secrets

- Never log a key, a secret, a token, or a body containing them — including in exception messages and
  `ToString()` overrides on options types.
- A tracked `appsettings.json` with a credential-shaped key is a finding regardless of whether the value is a
  placeholder.

## Money and time

- Prices and any quantity carrying a unit are **`decimal`**. A `float` or `double` on such a path is a
  finding.
- Timestamps are UTC on the wire. A transport client should not introduce local-time semantics.

## Conventions

- Must compile clean on `net10.0`, with warnings-as-errors.
- `CancellationToken` on every public async method, threaded all the way down.
- XML docs on every public member — `GenerateDocumentationFile` is on, so a gap is a build error.
- Fluent LINQ, never query-comprehension syntax.

## Tests

- **Test-first.** A new public method arriving without a test that failed first is a process finding.
- Bug fixes are **regression-first**.
- Unit tests stub the transport and touch no network.
- Integration tests must pass with **no credentials**. A new test that needs live credentials to pass at all
  is a finding — it will never run in CI.
- **A skip whose condition can never become false is dead weight pretending to be coverage.**

## Traceability

Every PR cites its issue with a plain `Closes #N` or a fully-qualified
`Related to adammarquette/trading-copilot#N` — a backticked keyword does not bind. Behavior, API or
configuration changes update the matching `R-#`, the architecture doc, the relevant ADR, and the README,
**in the same PR**. Stale documentation is a finding.
