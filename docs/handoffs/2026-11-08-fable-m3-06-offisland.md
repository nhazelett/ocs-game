**Work session handoff**

Expected look of this build and why: no visible change. No shipped scenario has an off-island vendor yet, so off-island awards exist in the Sim and tests only; M3-07 is where one appears. The action editor still lacks a lift picker (waits for the M2 gate).

## PRs reviewed this session
- PRs 78 and 79 (Fable): "ready to merge" comments already stand, heads unchanged. PR 80 (host nation support) opened this session; this branch stacks on it. No open Codex PRs. Scheduled run: no merges attempted (FOR-NICK #1).

## Changed
- fable/M3-06-offisland, stacked on fable/M3-06-hns (PR 80). New `Session.OffIsland.cs`: `OffIslandStops` (in `CheckAward`), `DrawLift`, `LiftRange` (preview), `RegisterShipment` (in `CompleteAward`), `ArrivalChecks` (in `Lifecycle` on the due day: airlift bump, then one customs roll), `LiftDelayFactors` (debrief).
- `AwardAction.Lift`; vendor `lift` modes (`VendorData.Lift`, `OffIsland`); rules `lift` block in rules-2025-10.json (`LiftRulesData`); random stream "lift"; `GapFactorKind.LiftDelay` (Unforeseeable).
- Schemas (rules `lift`, vendor `lift`), bundle check on lift modes, ADR-0005 and ADR-0006 addenda, assumptions.md route row and debrief tag row, backlog M3-06 status, M3-15 extended.

## Why
SPEC 4.1 "Stateside or off-island sourcing": airlift queue 7 to 14 days by priority, sealift 30 to 45; known vendors, lower unit price, large capacity; transport is a coordination that higher-priority cargo can bump; customs on arrival; nothing arrives in week one.

## Validated how
- OffIslandTests (6) on a spoke-b-90 variant with two stateside vendors entering at the hub: over 30 seeds an airlift generator award starts inside the preview range, never in week one, keeps its 60-day duration, and bumps and customs holds both occur; sealift fuel starts after the 30-day minimum and ships onward from the hub; five lift hard stops with the preview leaving the hash unchanged; a power gap during a late lift carries an Unforeseeable `LiftDelay` naming the delay record and the arrival day; save and resume; bundle check.
- Mutation: skipping the arrival checks or dropping the lift from the lead time each fails two tests.
- No golden changed; 255 Sim and 118 Harness tests; validate.py and its pytest suite, provenance.py, conflicts.py.

## Unresolved
- Lift priority is not a player choice; no lift cost; no consumable resupply pipeline (once arrived, supply flows daily). Bump and customs chances are placeholders.
- M3-06 is now complete across PRs 70 (IDIQ), 79 (ACSA), 80 (host nation) and this one, once merged.

## Next
- Fable: M3-05 transition plan (Sim side), then M3-07 (60-day scenario using the partner, host nation and stateside routes).
- Codex: M3-12, M3-13, M3-14, M3-11; M3-15 after PRs 78 to this one merge.
