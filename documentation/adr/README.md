# Architecture Decision Records

Why the library is the way it is. **Never read the folder** — resolve the ADR number you need and open that
one.

## How these are written

Nygard form: **Context · Decision · Alternatives considered · Consequences · Follow-ups**, filename
`NNNN-slug.md`.

- Once **Accepted**, the *decision* is immutable. A later ADR **supersedes** it; nothing is rewritten in place.
- A record is a living trail: extend it with dated `## Update` entries under a `## Decision log`, oldest first.
- **`## Follow-ups` stays last.**
- Supersession is cross-linked in **both** directions in the Status column below.
- What must never change is the reasoning. Structural housekeeping that preserves every word is fine.

An ADR is warranted when a choice constrains future work, when a reasonable engineer would ask "why not the
obvious thing", or when a change would break a consumer. Routine implementation does not need one.

**Write the alternatives you actually rejected, and why.** An ADR whose "alternatives considered" is
retrospective justification is a press release. The useful ones name the option that was genuinely tempting.

## Index

| ADR | Title | Status |
|---|---|---|
| [0001](0001-tag-driven-versioning.md) | The git tag is the version | Accepted |

*Adding a record? Add its row here in the same PR, and a routing entry in [`../README.md`](../README.md) if
the corpus shape changes.*
