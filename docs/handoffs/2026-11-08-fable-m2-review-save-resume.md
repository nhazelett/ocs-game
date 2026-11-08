**Work session handoff**

Expected look of this build and why: no visible change. Saves and counterfactuals exist in the Sim only; the game binds them in M3-08 and M3-09 (Codex).

## PRs reviewed this session
- None open at session start (PRs 59 and 60 had merged). No Codex PR to review.

## Changed
- PR 61 (fable/m2-review): `docs/reviews/2026-11-08-m2-pre-gate.md`, first milestone review (M0, M1 and M2 against SPEC 11, 200-seed balance table, objection health check). Backlog status line, M2-19 Done, M3-01 to M3-07 (Fable lane; only M3-01 and M3-02 gate-independent). FOR-NICK #3.
- PR 62 (fable/M3-01, stacked on 61): `InputLog.SavedDay`, `Session.ToSave()`, `Replay.Resume`; ADR-0005 addendum; new Codex row M3-08 (save and load in the game).
- fable/M3-02 (stacked on 62): `Counterfactual.Run`, `Choices`, `CounterfactualChange`, `CounterfactualView` (gap diffs, `NoLongerApplied`); `Session.AppliedRejected`; ADR-0006 addendum; SPEC 5 counterfactual example corrected; new Codex row M3-09 (picker prototype, after the gate).

## Why
All Fable M2 items are merged; M2 waits on Codex's game rows and Nick's playtest. Save and resume does not depend on the editor model the gate is judging.

## Validated how
- SaveResumeTests (22): all 15 approach goldens saved on every day resume to the same state hash and the same save JSON, and continue to the same end hash; textbook and greedy, 20 seeds, both scenarios, saved on six days each; finished saves; refused saves. A mutation (Resume skipping the saved day's actions) fails 20 of them.
- CounterfactualTests (10): replacing approach 1's ratify with refuse or park reproduces those goldens' hashes exactly; inserting or removing the spoke-b-90 day 43 cure notice or termination reproduces those goldens both ways; the SPEC 5 story; scope break named; mid-game horizon; 90-day re-run about 20 ms.
- 193 Sim and 97 Harness tests; validate.py, provenance.py, conflicts.py.
- Balance (main, 200 seeds, median mission.output): spoke-b textbook 0.931, greedy 0.714, do-nothing 0.607; spoke-b-90 0.926, 0.671, 0.646.

## Unresolved
- Design finding (M3-02): single-decision counterfactuals are rarely clean. SPEC 5 said a day 1 survey added to approach 1 removes the day 10 gap; in the Sim a survey alone changes nothing (corrected). The hub fuel coordination on day 3 does remove it, but the day 11 handshake then never happens and the coordination's withdrawal opens new fuel gaps, so on spoke-b-90 that change scores 0.058 worse. That is a real lesson (coordination is not a contract), but the picker must show `NoLongerApplied` and the new gaps prominently or the player will read it as "the right move was wrong". Worth a look in the M3-09 prototype.
- FOR-NICK #3: start M3 before the gate. Fable is proceeding on (a); if Nick says (b), close the M3 PRs unmerged.
- spoke-b-90 greedy is only 0.025 above do-nothing at the median; watch in the playtest (review section 1).

## Next
- Fable: the other M3 rows wait for the gate (FOR-NICK #3). Next Fable work without the gate: review Codex PRs as they arrive; design notes for M3-03 (funding lines) and M3-04 (COR fidelity) as assumptions.md drafts.
- Codex: M2-05, M2-20, M2-21, M2-22, M2-23; then M3-08 after M3-01 merges; M3-09 after M3-02 and the gate.
