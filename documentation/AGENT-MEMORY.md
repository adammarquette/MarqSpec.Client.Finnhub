# Agent memory

The catch-all for things that must persist across sessions but **don't fit any other formal document**. It is
deliberately informal, and it is *overflow* — not a substitute for the PRD, an ADR, or a role contract.

**How to use it**

1. **Read it before starting work.** It's short; that's the point.
2. **Append, don't overwrite.** Date every entry `YYYY-MM-DD`.
3. **Promote when it grows up.** When an entry earns a formal home — an ADR, a requirement, a contract — move
   it there and leave a one-line pointer behind.
4. **Keep entries terse.** A paragraph, not an essay.

---

## Practices to follow

**Seeded with the template — Work in a `git worktree`, never in the main checkout.** Sessions run in parallel;
sharing a working tree means one session's uncommitted edits land in another's commit. `scripts/claim.sh`
creates one for you.

**Seeded with the template — `git status` can be confidently wrong after a stale fetch.** A checkout whose
remote-tracking refs have not been updated reports "up to date" against a branch that has moved. `git fetch
--prune` before believing it.

**Seeded with the template — PowerShell 5.1 strips embedded double quotes from native-command arguments.** A
`gh api --jq '... "foo" ...'` invocation arrives with the inner quotes gone and fails to parse. Fetch raw JSON
and pipe to `ConvertFrom-Json`, or pass a payload with `--input <file>`. Same for `git commit -m` with a
multi-line message containing quotes — use `git commit -F <file>`. Also: `Set-Content -Encoding utf8` writes a
BOM, which `gh api --input` rejects.

## Notes & communications

*(nothing outstanding)*
