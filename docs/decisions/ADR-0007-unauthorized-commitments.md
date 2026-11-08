## Title
ADR-0007: Unauthorized commitments in the Sim (SPEC 4.5)

## Status (Proposed / Accepted / Superseded)
Accepted

## Context
Backlog M1-04. SPEC 4.5 (decided with Nick, 8 Nov 2026) defines two situations, one decision each, and per-unit precedent. Until now a handshake created a ratification work item automatically, so there was no decision and no consequence beyond shop time.

## Options considered
1. Keep automatic ratification and add a penalty meter.
2. Make each commitment an inbox decision with consequences that happen in the world, and let precedent change the odds of the next one.

## Decision
Option 2.

- New action `resolveCommitment { commitment, choice, fundingLine? }`. Handshakes take `ratify`, `park` or `refuse`; out-of-scope work takes `ratify` or `deny`. A commitment opened during a day's processing can be decided from the next morning. Additive under ADR-0005.
- New rules block `unauthorized` (rules schema): patience range, walk-off decline/markup odds, inquiry delay and hours, ratified bridge days, deny hours, habit start, rises, drop, quick-ratify window and cap.
- New event vocabulary: predicate `contract.present`, effect `vendor.outOfScopeWork` (value in cents). Spoke B and Spoke B 90 carry `unitExtraWork` from day 15.
- New random stream `unauthorized` (ADR-0002 list of streams), drawn only when a commitment exists or a unit's habit is above zero, so a game without commitments replays exactly as before.
- Read surface: `SessionView.Commitments`, `SessionView.UnitHabit(site)`; `VendorOption.Standing` (ADR-0006, additive); metrics `compliance.handshakeWalkOffs`, `habitHandshakes`, `outOfScopeWork`, `outOfScopeDenied`, `commanderConfidenceHits`, `commitmentsUnresolved`, `site.<id>.unitHabitPermille`; a report section listing each commitment and its decision.
- The textbook policy ratifies handshakes and denies out-of-scope work the morning they arrive. Greedy and do-nothing leave them open.

## Consequences
- Every state hash changed (new metric keys). Approach logs that previously relied on automatic ratification now ratify explicitly (approach 1 day 12, approach 2 day 20), which reproduces their earlier outcomes exactly; approach 3 denies the out-of-scope invoice. New goldens: approach1-park, approach1-refuse, approach3-oos-ratify.
- Balance, 200 seeds: textbook unchanged on both scenarios; greedy falls (it ignores the inbox) but still beats do-nothing.
- The refuse choice's priority-order penalty waits for commander priority allocation (assumptions.md).

## Date
2026-11-08

## Author
Fable
