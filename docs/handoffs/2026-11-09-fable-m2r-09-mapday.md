**Work session handoff**: Fable, chat session with Nick, 9 Nov 2026. M2R-09 map day projection (OBJ-0004 accepted: the map draws what the Sim says moved).

Expected look of this build and why: no change on screen. Codex's M2R-15 moving part is now unblocked. What the map would draw for Spoke B approach 1, days 8 to 20 (the morning after each day; S = service scheduled, O/X = open/closed):

    day 8:  barge -O  lightAir SO  ferry -O   nothing moving
    day 9:  barge -O  lightAir SO  ferry SO   Spoke C water vendor failed overnight
    day 10: barge SO  lightAir SO  ferry -O   Spoke B fuel, power, lodging go short
    day 12: barge -O  lightAir SO  ferry -O   Main Island fuel vendor ratified; cargo waiting, no barge today
    day 13: barge SO  lightAir SO  ferry -O   fuel run uc1, 2,340 units across the barge; Spoke B fuel, power, lodging restored
    day 14: barge -O  lightAir SO  ferry -X   fuel waiting, no barge today
    day 15: barge -O  lightAir SO  ferry -X   Spoke C water fails again; Spoke B goes short
    day 16: barge -X  lightAir SO  ferry SO   barge closed by weather; fuel waiting
    day 17: barge SX  lightAir SO  ferry -O   barge day but closed: fuel held by the closure
    day 18: barge -X  lightAir SO  ferry -O   still waiting
    day 20: barge SO  lightAir SO  ferry -O   fuel run uc1, 8,190 units; Spoke B fuel restored, power and lodging still short

## Changed
- `Views/MapViews.cs`: `MapLink`, `MapRun`, `MapHeld`, `MapShipment`, `MapChange`, `MapDayView`.
- `Core/Session.Map.cs`: `BuildMapDay()`; `Session.Day.cs` records runs and holds while it resolves capabilities; `Session.cs` gains a `SurfaceDeliveryRuns` overload that reports the link used.
- `SessionView.MapDay()`. ADR-0006 addendum (M2R-09). Backlog: M2R-09 Done, M2R-15 unblocked.

## Why
OBJ-0004 (Codex): a moving map drawn from schedules would show a barge arriving on a day the Sim closed the route. The view is taken from the engine's own day processing so the two can never disagree.

## Validated how
`dotnet test src/LinesOfSupport.sln`: 392 pass (4 new in MapDayTests: empty before day 1; approach 1 days 8 to 20 show a fuel run on an open scheduled barge and cargo held when it is closed, never a run on a closed or unscheduled link; day 10 changes include the Spoke B shortfall and every change points at a real record; building the map twice leaves the state hash unchanged). All seven Spoke B golden hashes identical to main. `validate.py`, `provenance.py`, `conflicts.py`, harness validate pass.

## Unresolved
- On-island deliveries (a truck from a Spoke B vendor to the Spoke B site) are not in `Runs`; the map shows them through site state. If Codex wants trucks on the island, that is a small follow-on row.
- `Shipments` is only exercised by the off-island tests indirectly; no map test uses an airlift scenario yet.
