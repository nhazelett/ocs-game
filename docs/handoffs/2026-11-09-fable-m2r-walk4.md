# Fable handoff: fourth pre-gate walk and the ready call (9 Nov 2026, scheduled run)

## Done
- Reviewed PR 132 (Codex M2R-26) and PR 133 (Codex M2R-30) as a first-time player from their PNGs and a fresh Godot run: both "Fable review: ready to merge" (PR comments).
- Fourth pre-gate walk on main plus PRs 132 and 133, from screenshots taken this run (`docs/handoffs/images/2026-11-09-fable-walk4-*.png`), written up in docs/reviews/2026-11-09-m2r-pre-gate.md. Checks 1, 3 and 4 pass on screen; check 2 is Nick's to play.
- Ready call: M2R is ready for the gate (M2R-23) once PRs 132 and 133 merge. This PR carries both (base main), so merging it alone lands all three.
- New rows from the walk, none holding the gate: M2R-31 (Fable, the inherited partner row reads as a noun), M2R-32 (Codex, the lead gap's tag said once), M2R-33 (Codex, map caption, cash line and list labels).
- FOR-NICK #6 recorded on main from Codex's unpublished handoff branch (approval refusal for a docs-only handoff PR), with Fable's recommendation.

## PRs reviewed this session
- PR 132 (M2R-26): ready to merge. Green, in lane, clean merge.
- PR 133 (M2R-30): ready to merge. Green, in lane, clean merge.

## Expected look of this build
Main plus PRs 132 and 133: the top strip reads "Morning of day 1", "16 shop hours a day, none spoken for", "Cash: $50,000.00 of $50,000.00" over a full bar; checkboxes are visible; the inbox header is "Your first morning"; the debrief's quoted lead factors show only their tag lines. Nothing else changes; this PR adds docs, screenshots and backlog rows only.

## Validated how
On the combined tree: `dotnet test src/LinesOfSupport.sln` (611 passed), validate.py (5 scenarios, 0 errors), provenance.py pass; Godot 4.6.1 smoke scenes FrontDoor, Chrome, Turnover, Lead, J4Dependency, GoingToPeople, Debrief and Morning all printed their PASS lines. Conflict-marker grep clean.

## Next
At the check-in: merge this PR (or 132, 133 and then this), then Nick plays M2R-23 from a fresh main build. Fable takes M2R-31 next (stacked on this PR if it is up).
