**Work session handoff**: Fable, chat session with Nick, 10 Nov 2026. The M2R gate result, the DESK milestone, DESK-01.

Expected look of this build and why: one change a player can meet. Terminate on host nation or ACSA support is refused: "Main Island Utilities (host nation) power at Hub is host nation support under the agreement. It is not this office's contract to end." The Ledger still shows the button until DESK-21; pressing it now does nothing harmful.

## Changed
- docs/design/desk-redesign.md: Nick's playtest in his words, decisions D1 to D12, what leaves the screen, Sim notes, order of work.
- Backlog: M2R-23 played and closed; DESK-01 to DESK-11 (Fable), DESK-20 to DESK-28 (Codex and Nick), ART-10, ART-11; status line.
- DESK-01: `Session.NotOursToEnd`, `SessionView.TerminateBlocked`, NotOursToEndTests. game/tests/OtherActionsSmokeTest.cs now terminates the office's own contract (it picked the first active arrangement, which was host nation support).
- art-direction.md: Nick's note on model quality; the four Suno tracks approved.

## Why
Nick played the tutorial and reached a screen where the hub read 0% everywhere with nothing to do. The host nation's power and fuel had been ended from the Ledger; he could not say whether he had pressed Terminate, "and therein lies the problem".

## Validated how
511 Sim and 121 Harness tests pass; goldens unchanged. OtherActionsSmokeTest and PerformanceSmokeTest pass on a local Godot 4.6.1. Validators and marker check clean.

## Unresolved
- Nick's SF 44 note said the tutorial lets one SF 44 take several deliveries and payments; DESK-09 gives that its two costs.
- The scheduled-run prompt was updated to the DESK milestone in the same check-in.
