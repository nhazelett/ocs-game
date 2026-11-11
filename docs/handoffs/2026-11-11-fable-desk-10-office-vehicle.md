**Work session handoff** (Fable, scheduled run, 11 Nov 2026, DESK-10, stacked on DESK-09)

Expected look of this build and why: no new screen until DESK-29 binds it. In the tutorial, Coastal Rentals' vendor card now also offers a "Vehicle for the contracting office" at $90 a day. A player who buys one sees vendor meetings preview at 1 shop hour instead of 2, and SF 44 delivery trips on Main Island at 1 hour instead of 2.

## PRs reviewed this session

See docs/handoffs/2026-11-11-fable-desk-08-staff-meetings.md (same run).

## Changed

- Rules: capability `shopVehicle` in the catalog with new flag `forShop` (no site demand, not scored); new block `shopVehicle.visitHoursPermille` 500, the one rule number. Schema too.
- Tutorial: Coastal Rentals offers `shopVehicle` (2 a day, a day's notice, $90).
- Sim (Session.ShopVehicle.cs, new): an award for a `forShop` capability at any site is an ordinary rental; it draws no failure or confusion roll, so buying one moves no other draw. While one runs, `MeetVendorAction` and SF 44 trips on the shop's island cost half, rounded up to the hour. `SessionView.OwnInitiative` (site, capability, label, a sentence, vendors, whether you have one) and `SessionView.HasShopVehicle`.
- docs/assumptions.md, design doc Sim notes, BACKLOG DESK-10 row, new Codex row DESK-29 (the vehicle on screen), DESK-28's list, status line.

## Why

D5: the first use of "Buy on your own initiative" is a vehicle that gets you downtown and makes vendor visits cost fewer hours.

## Validated how

572 Sim tests (4 new in ShopVehicleTests: the list entry and its sentence, the meeting at half, the SF 44 trip at half, and a game with the vehicle ending with the same gaps as one without) and 121 Harness tests pass. validate.py and its unit tests, provenance.py, harness validate and the game build pass. Goldens unchanged: no policy buys the vehicle, and a vehicle draws nothing.

## Unresolved

Only the tutorial sells a vehicle; Spoke B gets one with its team after the gate (DESK-12). Whether finance, the SCO or the J4 should also get cheaper with a vehicle is left for the playtest; D5 names vendor visits only.

## Next

All Fable DESK rows before the gate are built (DESK-02 to DESK-10, PRs 162 to 170). The Fable queue is at nine unmerged PRs; the next runs should do review, backlog and design work until a check-in merges the stack.
