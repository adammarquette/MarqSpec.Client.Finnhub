# AGENTS.md — MarqSpec.Client.Finnhub (root)

Rules for **every** agent in this repository — the .NET client for Finnhub's news REST and equities/indices
market-data surfaces. It is consumed as a submodule by
[trading-copilot](https://github.com/adammarquette/trading-copilot). Role- and subtree-specific rules live in
their own contracts, so they cost context only when they apply.

## Take your role's contract first

| If you are… | Read first | How it loads |
|---|---|---|
| writing library code or unit tests | [`MarqSpec.Client.Finnhub/AGENTS.md`](MarqSpec.Client.Finnhub/AGENTS.md) — Coding | on your first read of a file there |
| writing integration tests | [`IntegrationTests/AGENTS.md`](MarqSpec.Client.Finnhub.IntegrationTests/AGENTS.md) — QA | on your first read in that project |
| **reviewing any change** | [`agents/code-reviewer.md`](documentation/agents/code-reviewer.md) | **open it yourself** |
| **touching CI/CD, packaging, or release** | [`agents/platform.md`](documentation/agents/platform.md) | **open it yourself** |
| **assigning work from the board, or driving a task to approval** | [`agents/coordinator.md`](documentation/agents/coordinator.md) | **open it yourself** |

The subtree contracts load by directory proximity — **lazily, when you first read a file there, not at session
start**. The role contracts follow *what you are doing* rather than where a file sits, and never auto-load.
**Wearing one of those hats without opening its contract is the most common way agents get a repo wrong.**

> Each `AGENTS.md` has a one-line `CLAUDE.md` beside it holding `@AGENTS.md`. **Those shims are load-bearing** —
> Claude Code reads `CLAUDE.md`, not `AGENTS.md`. Deleting one as "redundant" silently unloads that contract.

## What this repo is

A **data-only** typed .NET client for Finnhub: market/company news over REST, plus equities/indices quotes
(REST snapshot) and a trade websocket. It does not place orders, hold accounts, or execute anything. Published
as the NuGet package `MarqSpec.Client.Finnhub`, targeting `net10.0`. Solution
`MarqSpec.Client.Finnhub.slnx`: the library, its unit tests, and its integration tests. Build with
`dotnet build MarqSpec.Client.Finnhub.slnx`; before a PR, `dotnet format --verify-no-changes` and tests green.

The public surface a consumer touches: `IFinnhubNewsClient` / `FinnhubNewsClient`, `IFinnhubMarketDataClient` /
`FinnhubMarketDataClient`, `FinnhubQuoteStream` (over `IFinnhubWebSocket`), and `FinnhubOptions`. Everything
else is wire models, transport, and typed errors (`FinnhubRateLimitException`,
`FinnhubSubscriptionLimitException`).

## Source of truth

The markdown under [`documentation/`](documentation/) **and the GitHub issues and PRs** are the highest-level
source code of the system: the C# below is reconstructable from them. Read them as source and keep them
compiling. `R-#`, ADR numbers and `gh#N` are its symbol table.

**Route, don't read.** [`documentation/README.md`](documentation/README.md) maps every document — what it is
and when to open it. Resolve the section you need through it; **never load the corpus**.

[`AGENT-MEMORY.md`](documentation/AGENT-MEMORY.md) is the catch-all for practices with no formal home — check
it before starting, and add dated entries only when nothing formal fits.

## The non-negotiables

- **No secrets in source.** Credentials arrive through the Options pattern and environment; never a literal,
  never a tracked `appsettings.json`, never a log line. The API token authenticates via the
  `X-Finnhub-Token` header, never a query-string parameter. Assume this repository is public.
- **The library transmits; it does not decide.** Policy, limits and orchestration belong to the consumer. A
  policy check added here sits *below* the consumer's own gate, in a different repository, where that gate can
  neither see nor audit it.
- **Know which operations are safe to repeat.** This surface is read-only today. A timeout is an *unknown
  outcome*, not a failure. Anything that resends must prove the operation tolerates it, and anything that
  reports "it did not happen" must actually know that.
- **Test-first is the Definition of Done.** No new public method without a failing test written first.
- **Wear a hat, open its contract** — before you start, not after.

## Working rules

- **Docs in lockstep — the same-PR rule.** A change whose behavior, API or configuration a document describes
  updates **the affected section of that document, in the same PR** — the PRD (`R-#`), the architecture doc,
  the ADRs, the README that ships inside the package, this file. Update the section, not the whole file.
- **Issue-first — no orphaned PRs.** Every PR cites an issue opened before it (`Closes #N` / `Related to #N`);
  cite issues as `gh#N`. Work here is often driven by an issue in trading-copilot — cite that issue with a
  fully-qualified reference; a Finnhub PR cannot close a trading-copilot card. **Task specs and acceptance
  criteria belong in the issue**, never as files under `documentation/`.
- **Maximal metadata on every issue and PR:** assignee, milestone, `work:*` and `Work Estimate` labels. Issues
  are the board cards; a PR is not carded. A thin issue is a defect — the next agent rebuilds context from
  these fields.
- **Commits:** Conventional Commits, plus **both** an `Assisted-by:` and a `Co-Authored-By:` trailer on
  AI-authored changes. Full type list: [`CONTRIBUTING.md`](CONTRIBUTING.md).
- **Branch off `develop` and PR back into it.** `develop` is the sole integration branch, never a workspace.
  Promotion is one-way with one source per step: `staging` ← `develop`, `main` ← `staging`. Never branch off or
  PR into `main`. Name branches `<type>/<work-item-id>_<title>`. **A release is cut on `main`, and the tag is
  the version** — nothing declares a version in a file
  ([ADR-0001](documentation/adr/0001-tag-driven-versioning.md)).
- **Work in a `git worktree`, never in the main checkout** — `git worktree add .worktrees/<branch> <branch>`.
  Sessions run in parallel; sharing a working tree means one session's uncommitted edits land in another's
  commit.
- **Claim before you start — `scripts/claim.sh <issue-id>`.** The **pushed** branch is the claim; a local
  worktree is invisible to parallel sessions. A tip unmoved for 4 hours is fair game — say so on the issue
  first. **This repo is one half of a two-repo card**: check both trackers before assuming a card is free.

*Every line here is paid by every agent in every session. Keep it small: anything role- or subtree-specific
belongs in its contract, and anything with a formal home belongs there rather than restated here.*
