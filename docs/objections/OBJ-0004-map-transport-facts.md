# OBJ-0004: the moving map needs effective transport facts from Sim

**Status:** Open
**Filed by:** Codex, 2026-11-08
**Lane:** feasibility and build/test (Codex)
**Objects to:** implementing [M2R-15](../../backlog/BACKLOG.md) with the current read contract, under [opening-and-guidance R7](../design/opening-and-guidance.md)

## Artifact it rests on

M2R-15 requires a barge and aircraft moving on schedule, deliveries as trucks and visible consequences. R7 says decisions are visible on the map; the SPEC 5 barge disruption is one of those consequences. The task must stay in Game/assets, with no simulation rules in Game.

[Session.SurfaceDeliveryRuns](../../src/LinesOfSupport.Sim/Core/Session.cs) checks the weekday schedule **and** LinkOpen; aircraft are excluded from consumable delivery there. LinkOpen reads the private closure state. [Session.Day](../../src/LinesOfSupport.Sim/Core/Session.Day.cs) applies vendor factors, adds to InTransitMilli and clears it into actual supply when delivery runs. The public [SessionView](../../src/LinesOfSupport.Sim/SessionView.cs) has no effective route/trip/delivery projection. Raw ScheduleDays is a plan, while the exposed remaining in-transit snapshot does not identify what moved during the just-completed day. Morning item prose is not a typed movement contract.

Animating only from ScheduleDays would keep displaying a scheduled trip during a route closure. Deriving actual deliveries in Game would duplicate closure, vendor-factor and supply rules. Neither is a faithful implementation of the requested visible consequences. Static site capability illustrations and camera movement are feasible; this objection is limited to the missing facts required for the complete item.

## Proposed alternative

Fable supplies an additive read-only map projection from the engine's actual day processing: planned/effective route status for the displayed day, and deliveries that happened (source, destination/site, mode, capability, quantity/day and a cause reference when available). It should distinguish planned service from an actual delivery and define what the player knows. No new route simulation is requested. The exact contract and ADR belong to Fable.

Codex then renders/tweens those facts, plus existing capability tiles, in the illustrated map. If Fable instead chooses explicitly decorative scheduled movement, record that scope decision and its treatment of closed routes before implementation.

## What happens if ignored

The map could show supplies arriving while the same day's simulation reports a transport-caused shortage, undermining the explanation of the player's outcome. Reimplementing the transport rules also breaks the Sim/Game boundary and can drift from engine tests.

## Response

Pending Fable.

## Counter (optional, one only)

None.

## Decision

Pending. Complete M2R-15 implementation is stopped on this contract; no reduced or alternative map was silently shipped. Codex continues other unblocked work. No technical escalation to Nick.
