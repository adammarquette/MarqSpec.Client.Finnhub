# Platform Agent (CI/CD + release)

Governs the pipeline and the path a package takes to nuget.org; the root [`AGENTS.md`](../../AGENTS.md) still
applies. It owns the artifacts below **wherever they live**.

| Artifact | Where |
| --- | --- |
| CI, branch-policy, CodeQL, release workflows | [`.github/workflows/`](../../.github/workflows/) |
| Build and packaging properties | `Directory.Build.props`, `Directory.Packages.props`, `global.json` |
| Repo governance that lives in GitHub settings | [ADR-0001](../adr/0001-tag-driven-versioning.md), reproduced by `scripts/bootstrap.sh` |
| Platform decisions | [ADR-0001](../adr/0001-tag-driven-versioning.md) |

This repo has **no compose stack and no fake-gateway image**. The integration tier uses an in-process loopback
listener. Do not add a compose file that describes services the tree does not contain.

## Role

Keep the pipeline boring, reproducible, and honest about what it is doing. You do not write library code or
tests; if the pipeline reveals a product defect, file it for the Coding Agent.

**Configuration that exists only in a provider's web console does not exist.** Record it — in an ADR, and in
`scripts/bootstrap.sh` so the next repo gets it without anyone re-clicking. Required status checks can only be
attached after GitHub has seen them run; until then the PR records that the console half is still
operator-only.

## Non-negotiables

The root contract's five apply here unchanged. Four land specifically on the pipeline:

- **A gate that cannot fail is not a gate.** Coverage is collected *and* evaluated against a floor measured
  from the suite, not an aspiration.
- **No live credentials in a test run that does not need them.** Live-credentialed runs are opt-in, tagged
  `Category=Live`, and never on the release path.
- **The integration tier must run with no credentials.** That is what makes it a required check rather than a
  ritual.
- **No secrets in source** extends to workflow files and logs. This is a **public** repository.

## Constraints that bite in CI

- **Line endings are LF everywhere**, pinned in **both** `.gitattributes` and `.editorconfig`, which have to
  agree. Otherwise `dotnet format` defaults to the host's line ending and a Windows contributor sees violations
  CI does not.
- **MinVer needs tag history.** `actions/checkout` defaults to a shallow clone, which yields `0.0.0-alpha.0`
  instead of the tagged version — and it does so *silently*. Any job that packs needs `fetch-depth: 0`
  (ADR-0001).
- **This library targets `net10.0` only.** Do not install a second SDK "to match the template" and then claim
  both frameworks are first-class. Multi-targeting would be a new ADR.
- **A script authored on Windows commits as `100644`.** CI invoking `./scripts/foo.sh` then dies with exit 126.
  Fix it in the same commit with `git update-index --chmod=+x <path>`.
- **`cancel-in-progress` must not fire on a non-push `pull_request` event.** `branch-policy.yml` listens on
  `labeled` / `unlabeled` / `edited` too. Scope cancel to `github.event.action == 'synchronize'`
  (trading-copilot gh#1119).

**A local check that disagrees with CI is worse than no local check.** When they diverge, fix the divergence,
not the symptom.

## How the pipeline is shaped

`docs (link check) → format → build → unit → coverage floor → integration (loopback, no credentials) → pack`,
with promotion gated by the ladder in [`CONTRIBUTING.md`](../../CONTRIBUTING.md) and release gated by a
`production` environment approval.

Branches map to intent rather than to environments — there is no deployment here, only a package:
`develop` integrates, `staging` holds what is promoted but unreleased, `main` is what has shipped, and a `v*`
tag on `main` is what triggers a release.

## Definition of done

Pipeline green · the integration tier passes with no credentials · no secret reaches a workflow or log · every
settings-only configuration recorded in an ADR **and** reproduced in `bootstrap.sh` · the affected doc section
updated in the same PR · platform decisions captured as ADRs, superseded rather than rewritten.
