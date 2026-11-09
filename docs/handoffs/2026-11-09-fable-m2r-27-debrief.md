**Work session handoff** (Fable, scheduled run, 9 Nov 2026 about 13:00 UTC)

Expected look of this build and why: the same screens as main. The Sim now gives the debrief an opening paragraph (`Debrief().Summary`) and plain labels (`DebriefWords`), but no game screen shows them until Codex's M2R-28. Record text a player reads changes slightly: "1 day" not "1 day(s)", no "(oos1)", out-of-scope work no longer called a handshake, sentence starts capitalised.

## PRs reviewed this session
- None open at session start (PRs 126 and 127 were merged at the check-in).

## Changed
- Third pre-gate walk, on main, from screenshots taken this run (Godot 4.6.1 under Xvfb with the game's smoke scenes and their screenshot flags; a throwaway scene, not committed, for the debrief). Written up in docs/reviews/2026-11-09-m2r-pre-gate.md; eight screenshots committed as `docs/handoffs/images/2026-11-09-fable-walk3-*.png`.
- M2R-27 (Sim): `DebriefSummary` and `DebriefView.Summary`, `DebriefWords` (factor, tag meaning, timeline kind, spoken numbers), `Session.Plural` and `SentenceCase`, the claim record without its id, `CommitmentName` for out-of-scope work, "4,000 gal" in the owner factor. ADR-0006 addendum.
- Tests: DebriefSummaryTests (approach 1 opens on the unowned fuel from Maj Cole; asking the J4 moves it; every golden and policy gets prose about a real gap with that gap's own evidence; no "(s)" in any record or factor; every label has words); PlainNamesTests covers commitment ids and the summary; TransitionTests expectation updated for "5 days".
- Goldens regenerated: text and state hash only. Every changed line is a "(s)" plural, the claim id, the out-of-scope name, the "4,000" figure or a capitalised sentence start; no scorecard or gap line moved.
- Backlog: M2R rows merged in PRs 126 and 127 marked Done; new rows M2R-27 (Fable) and M2R-28 (Codex, the debrief as an after-action); M2R-23 waits on M2R-26, 27 and 28; status line rewritten.

## Why
Gate check 4 is the one-sentence lesson, read off the debrief. On main the debrief is a report: percentages first, the lesson two screens down inside "Gap 0". The paragraph puts the lesson first, in a person's voice, using only text the gaps already carry (R4, R9).

## Validated how
dotnet test src/LinesOfSupport.sln: 488 Sim and 121 Harness tests pass. Game project builds; DebriefSmokeTest, PlainNamesSmokeTest, LedgerSmokeTest and CommitmentSmokeTest pass headless (Commitment aborts at exit after printing PASS, the known Godot shutdown flake, Q13). validate.py 0 errors; provenance passes; no conflict markers.

## Unresolved
- Sustainment has no SCO or commander person, so its summary has no speaker (`From` null). Sustainment is paused with the rest of Sim depth.
- The owner factor names the partner as "Partner force" mid-sentence (the scenario's partner name); left as is.

## Next
At the check-in: merge this PR. Then Codex M2R-26 and M2R-28; then one more set of screenshots on main (top strip, debrief) and the ready call for the gate.
