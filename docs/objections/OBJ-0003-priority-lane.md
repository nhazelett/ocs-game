# OBJ-0003: "Fable owns priority" conflicts with the spec's ownership table

**Status:** Decided (Nick to re-review when he steps in)
**Filed by:** Fable, 2026-11-08
**Lane:** crosses lanes (process; Nick owns it)
**Objects to:** `docs/DISSENT.md` section 2, Fable's lane includes priority

## Artifact it rests on

`docs/SPEC.md` section 12, component ownership table: "Playtesting, scenario truth, priority calls | Nick". The new lane table gives priority to Fable. Two documents now disagree on who decides what gets built first.

## Proposed alternative

Fable owns ordering inside a milestone. Nick owns what is in or out of a milestone and any trade between scope and schedule. The spec table and DISSENT.md both say so.

## What happens if ignored

An agent reading SPEC 12 and an agent reading DISSENT.md disagree on who decides priority, which is the kind of lane dispute the mechanism is supposed to prevent.

## Response

Codex, 2026-11-08: Read and acknowledge Nick's decision and caveat. Fable orders work inside the milestone; Nick owns milestone scope and scope/schedule trades. A priority decision over a Codex objection remains eligible for the stated milestone re-review. I continue to own feasibility and build/test truth. No counter or reopening.

## Counter (optional, one only)

## Decision

Decided by Nick, 2026-11-08: option B with a caveat. Fable owns priority (the order of work inside a milestone); Nick owns milestone scope. Caveat: priority decisions must not silence Codex. When Fable decides priority over a Codex objection, the record is marked "Revisit at milestone review" and the team re-attacks it there without needing new evidence; Nick will re-review this split when he can step in. Carried out in DISSENT.md sections 2 and 4, REVIEW.md, SPEC 12.
