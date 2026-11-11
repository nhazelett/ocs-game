**Work session handoff** (Fable, scheduled run, 11 Nov 2026, DESK-07, stacked on DESK-06)

Expected look of this build and why: no new screen until DESK-27 binds the COR reasons. One visible line changes in the tutorial: the award preview's COR note now gives the reason, for example "TSgt Pratt (3 CES) works with material handling equipment every day and watches no other contract: catches about 80% of problems, reported 2 days late. Some vendor problems will reach you late or not at all."

## PRs reviewed this session

See docs/handoffs/2026-11-11-fable-desk-05-vendor-cards.md (same run).

## Changed

- `cors[].knows` (scenario schema): what the COR's unit works with. The tutorial's Lane (FSS) knows water, lodging and food; Pratt (CES) knows equipment, power and waste.
- Rules `cor`: `offFitPermille` 300, `perOtherContractPermille` 150, `floorPermille` 100 (schema too). For a COR with `knows`, report odds are the quality's, less 300 off their work, less 150 for each other running contract they watch, never below 100. Lag is unchanged. A COR without `knows` keeps the quality's odds.
- `SessionView.CorChoices(capability)`: `CorChoice` per COR (fits, watching, odds, lag, a `Why` sentence), best odds first. The preview's COR soft gap uses the same sentence when fit is modeled.
- `ActionEditorView.RequestedStartDay`, `RequestedDurationDays`, `RequestedQuantity` (D9).
- docs/assumptions.md, design doc Sim notes, BACKLOG DESK-07 and DESK-27 rows, status line.

## Why

D8 and D9. Nick asked why anyone would pick TSgt Pratt. For the forklift: Pratt 80%, 2 days late; Lane 40% (not their work, already watching billeting and dining), same day. For food it reverses.

## Validated how

553 Sim tests (6 new in CorFitTests) and 121 Harness tests pass. Goldens unchanged: the tutorial's do-nothing run now rolls Lane's dining reports at 850 instead of 1000 (Lane also watches billeting), and at seed 1 every one is still reported; the COR stream is its own, so nothing else moves. validate.py, provenance.py, harness validate and the game build pass.

## Unresolved

Spoke B 90 and the sustainment draft have CORs without `knows`; they should get them when those scenarios are next touched (after the gate).

## Next

DESK-08 (staff meetings that sometimes carry nothing), stacked on this PR.
