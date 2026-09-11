# AGENTS.md — Coding Agent (the library)

The **Coding Agent** contract, governing the library and its unit tests. Takes precedence over the root
`AGENTS.md` for this subtree. The **QA Agent** owns the integration tests separately — see
[`MarqSpec.Client.Finnhub.IntegrationTests/AGENTS.md`](../MarqSpec.Client.Finnhub.IntegrationTests/AGENTS.md).

## Role

Write **library code** and the **unit tests** that drive it. You do **not** write integration tests — QA does
that *independently*, so intent and implementation are verified separately.

## Test-first (mandatory)

- Write the **failing unit test before** the implementation (red → green → refactor). No new public method
  without a failing test written first; bug fixes are regression-first.
- Unit tests go in **`MarqSpec.Client.Finnhub.Tests`**, **every public method covered**, stubbed transport —
  no I/O, no network. A unit test that opens a socket belongs in the integration project. Name:
  `MethodUnderTest_Should{ExpectedBehavior}_When{condition}`.
- Prefer `[Theory]` where a fact is really a table.

## Standards

`net10.0`, C# latest. File-scoped namespaces, nullable on, **warnings-as-errors**, immutability by default,
**DI through the constructor**, async-all-the-way with `CancellationToken` on every public async method,
structured logging via `ILogger` when a type logs, exhaustive switches.
**Money, prices and any quantity carrying a unit are `decimal` — never `float`/`double`.** Fluent LINQ, never
query-comprehension syntax.

Every public type and member carries XML documentation — `GenerateDocumentationFile` is on and the package
ships the XML, so a missing comment is a build error, not a style note.

## What this client is not

- **It does not decide.** Policy, limits, eligibility and orchestration belong to the consumer. Rejecting a
  malformed request or a missing credential is transport, not policy, and is fine.
- **It does not retry.** This surface is read-only. A timeout is an unknown outcome the caller owns.
- **It does not log credentials.** Not the key, not a bearer token, not a URL that contains them. The token
  travels as `X-Finnhub-Token`, never a query string.

## Wire models

Finnhub's published JSON is authoritative: **when a model and the payload disagree, the payload wins and the
model is the defect.** Keep one public type per file where practical.

## Dependencies

Central Package Management (`Directory.Packages.props`) — `.csproj` files carry no versions. The library's
dependency surface is part of its public contract. A new dependency needs a line in the PR saying why.

## Definition of done

Failing-test-first now green · every public method covered · standards + `dotnet format --verify-no-changes`
clean · `dotnet build` clean under warnings-as-errors · traces to the task's issue and a PRD requirement
(`R-#`) · no secrets · the README updated if the public surface moved.
