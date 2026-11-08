**Work session handoff**

Expected look of this build and why: no visible change in Godot. In the harness, reports gain an UNAUTHORIZED COMMITMENTS section, and handshakes now play out as decisions instead of automatic ratification.

## PRs reviewed this session
- None open at start of M1-04 (PRs 21, 24, 25 were merged earlier today; see 2026-11-08-fable-view-models.md).

## Changed
- `Core/Session.Commitments.cs`: situation 1 (handshake: ratify, park, refuse, with seeded 5 to 15 day patience, walk-off, decline or 25% markup afterward, 10-day HHQ inquiry), situation 2 (out-of-scope work under an active contract: deny or ratify; unresolved becomes a claim and an inquiry), per-site unit habit (precedent) that sets the odds of the next handshake.
- New action `resolveCommitment`; rules block `unauthorized` (schema + rules-2025-10); predicate `contract.present`; effect `vendor.outOfScopeWork`; event `unitExtraWork` in spoke-b and spoke-b-90 (day 15+, $2,400).
- CheckAward refuses a soured vendor and prices the markup; ActionEditor shows `VendorOption.Standing`. Termination keeps ratified out-of-scope money obligated.
- Textbook policy ratifies handshakes and denies out-of-scope work. Report section per commitment. Metrics listed in ADR-0007.
- Goldens: approach 1 and 2 now ratify explicitly (the old automatic ratification), approach 3 denies; new approach1-park, approach1-refuse, approach3-oos-ratify. All regenerated.
- ADR-0007, assumptions.md (5 rows), SPEC 5 approach 1 sentence, backlog.

## Why
SPEC 4.5 and Nick's call: every cost here is something that happens in the world, and "park it" is a gamble, not a free pass.

## Validated how
- 109 Sim tests (12 new in CommitmentTests), 94 Harness tests.
- Old approach outcomes reproduce exactly with the explicit ratify (Spoke B 83/85/94/82%, Spoke B 90 76/77/88/83/80%). Ratify 83% beats park and refuse (75%) on approach 1, and park and refuse are different stories (refuse cuts fuel the same day; park runs until the vendor walks).
- 200 seeds per policy: textbook unchanged (spoke-b median 0.936, 187/200 pass; spoke-b-90 0.926, 187/200); greedy 0.756 and 0.671 (was 0.806 and 0.745: it ignores the inbox); do-nothing 0.673 and 0.646. Order holds.

## Unresolved
- Refuse's priority-order penalty waits for commander priority allocation (assumptions.md, M2).
- The Claude Doc copy of the spec still has the old approach 1 sentence; docs/SPEC.md is current.

## Next
- Codex: M1-05.
- Fable: next items from SPEC 11 for M1 close-out and M2 (debrief gap tags, requirement and action editor experiments).
