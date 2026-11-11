**Work session handoff** (Fable, scheduled run, 11 Nov 2026, DESK-06, stacked on DESK-05)

Expected look of this build and why: no screen change until DESK-26 adds the three toggles. The Sim only acts when an award names a pick, and nothing sends one yet.

## PRs reviewed this session

See docs/handoffs/2026-11-11-fable-desk-05-vendor-cards.md (same run): 158, 159, 160 and 161 re-checked after their new commits, all ready to merge in that order.

## Changed

- `AwardAction.Priorities`: two of "fast", "cheap", "good", on written contracts (purchase order, BPA call, IDIQ order, UCA). Null is no pick and changes nothing.
- Rules block `pickTwo` (schema too): `competeShopDays` 2, `premiumPermille` 150, `firstQuoteFailurePermille` 15.
- Cheap and good: the action's shop days plus 2 (added after the random draw, so the work stream is unchanged). Fast and good: price and mobilization plus 15%, so thresholds, call limits and funding see the higher price. Fast and cheap: the contract's reliability minus 15 in a thousand.
- `ActionPreview.Trade`: one sentence with this award's numbers ("Fast and good: you pay a 15% premium for a quick award without cutting corners, about $X of this price."). Hard stops for a pick on cash or card, or anything but two of the three.
- The action record ends "Picked fast and good." (the debrief's decision list). The debrief's vendor-failure factor adds "You took the first quote (fast and cheap), which made a failure more likely every day it ran."; the uncovered-request factor adds "It was competed (cheap and good), which added about 2 days of shop work." when the late arrangement was competed. `Arrangement.Pick` keeps the pick.
- All of it is in Session.PickTwo.cs plus lines marked DESK-06, so it can be stripped if Nick does not like it.
- docs/assumptions.md, design doc Sim note, BACKLOG DESK-06 and DESK-26 rows, status line.

## Why

D7: Nick's points of emphasis, with competition and evaluation as consequences of the pick rather than a screen.

## Validated how

547 Sim tests (9 new in PickTwoTests, including a debrief check with the failure odds forced to 1000 through a copied rules file) and 121 Harness tests pass. Goldens unchanged. validate.py, provenance.py, harness validate and the game build pass.

## Unresolved

- The Sim does not require a pick on a written contract, so the policies and old logs keep playing. The game should always send one (DESK-26 says so).
- The three numbers are placeholders for the DESK gate.

## Next

DESK-07 (COR fit and the customer's ask), stacked on this PR.
