**Work session handoff**

Expected look of this build and why: no visible change. Saves exist in the Sim only; the game binds them in M3-08 (Codex).

## PRs reviewed this session
- None open at session start (PRs 59 and 60 had merged). No Codex PR to review.

## Changed
- PR 61 (fable/m2-review): `docs/reviews/2026-11-08-m2-pre-gate.md`, first milestone review (M0, M1 and M2 against SPEC 11, 200-seed balance table, objection health check). Backlog status line, M2-19 Done, M3-01 to M3-07 (Fable lane; only M3-01 and M3-02 gate-independent). FOR-NICK #3.
- fable/M3-01 (stacked on PR 61): `InputLog.SavedDay`, `Session.ToSave()`, `Replay.Resume`; ADR-0005 addendum; new Codex row M3-08 (save and load in the game).

## Why
All Fable M2 items are merged; M2 waits on Codex's game rows and Nick's playtest. Save and resume does not depend on the editor model the gate is judging.

## Validated how
- SaveResumeTests (22): all 15 approach goldens saved on every day resume to the same state hash and the same save JSON, and continue to the same end hash; textbook and greedy, 20 seeds, both scenarios, saved on six days each; finished saves; refused saves. A mutation (Resume skipping the saved day's actions) fails 20 of them.
- 183 Sim and 97 Harness tests; validate.py, provenance.py, conflicts.py.
- Balance (main, 200 seeds, median mission.output): spoke-b textbook 0.931, greedy 0.714, do-nothing 0.607; spoke-b-90 0.926, 0.671, 0.646.

## Unresolved
- FOR-NICK #3: start M3 before the gate. Fable is proceeding on (a); if Nick says (b), close the M3 PRs unmerged.
- spoke-b-90 greedy is only 0.025 above do-nothing at the median; watch in the playtest (review section 1).

## Next
- Fable: M3-02 counterfactual re-run engine, stacked on M3-01.
- Codex: M2-05, M2-20, M2-21, M2-22, M2-23; then M3-08 after M3-01 merges.
