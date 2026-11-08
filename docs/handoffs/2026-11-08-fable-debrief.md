**Work session handoff**

Expected look of this build and why: no visible change in Godot. Harness reports gain a GAP TAGS section; Codex's M2-07 debrief screen binds `SessionView.Debrief()`.

## PRs reviewed this session
- None open (PRs 27, 28, 29 merged earlier today).

## Changed
- `Views/DebriefViews.cs`, `Core/Session.Debrief.cs`: DebriefView with timeline bands, events, decisions with information tiers at the time, gaps with tagged factors, scorecard, commitments. `SessionView.Debrief()`.
- SessionReport: GAP TAGS section. Goldens regenerated (text added only; no state hash changed).
- Removed 15 `*.actual.txt` test-failure files I committed by mistake in M1-04; `.gitignore` now excludes them.
- ADR-0006 addendum; assumptions.md "Debrief tags" table; backlog M2-08 Done, M2-07 unblocked.

## Why
SPEC 4 debrief logic and the M2 done criterion: the gap view tags the seeded Spoke B gaps correctly.

## Validated how
- 122 Sim tests (13 new in DebriefTests, plus views-never-change-the-hash now includes Debrief), 94 Harness.
- The seeded gaps: approach 1 day 10 avoidable (missing fuel line, checklist visible day 1); day 16 discoverable (no survey, barge dependency); approach 2 coordination withdrawal avoidable; day 20 population growth avoidable. Approach 2's barge gap is avoidable because its survey returned day 3. Approach 3 has no gaps; approach 4's is avoidable (late, not unlucky); 3b's stop-work is avoidable.

## Unresolved
- Tags do not weigh cost of information against the gap; that is the counterfactual view (M3).

## Next
- Fable: M2-09 PrepView.
- Codex: M2-01 onward; M2-07 can bind the debrief now.
