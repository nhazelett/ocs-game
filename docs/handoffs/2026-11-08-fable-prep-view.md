**Work session handoff**

Expected look of this build and why: no visible change. M2-10 (Codex) builds the prep screen on this.

## PRs reviewed this session
- None open.

## Changed
- `Views/PrepViews.cs`: `PrepScreen.View(bundle)` and `PrepScreen.Check(bundle, picks)`; usable before a Session exists. Each choice shows what it changes with and without it, using the scenario's own defaults (for Spoke B: system online day 3 with it, day 12 without; paying agent day 1 vs day 5; no purchase cards at all without the GPC choice). Choices the build records but does not model say so.
- `Session.Create` rejects a prep choice picked twice.
- Schedule: Fable now runs hourly (Nick, 8 Nov 2026); FABLE-SESSION, CODEX-SESSION and CODEX-KICKOFF updated.
- ADR-0006 addendum; backlog M2-09 Done, M2-10 unblocked.

## Why
SPEC 4.2: the pre-deployment window is a decision with a budget (all choices cost 17 prep-days against 14).

## Validated how
- PrepTests: the view lists every rules choice with effects, the budget does not fit everything, and Check's stops equal the exceptions Create throws for over-budget, unknown and duplicate picks. 124 Sim, 94 Harness tests pass.

## Unresolved
- Four prep effects are still recorded only (vehicle and vendor info tiers, vetting speed, partner agreements); the view says so.

## Next
- Fable: add the next M2 Fable items (commander priority allocation for the refuse penalty is a candidate; tutorial scenario JSON from SKELETONS.md).
- Codex: M2-01 onward.
