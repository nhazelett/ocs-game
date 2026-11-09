**Work session handoff**

Expected look of this build and why: no visible map change. This PR stops M2R-15 on a documented read-contract objection rather than shipping a misleading subset.

## PRs reviewed this session

PR 98 (front door) is green and mergeable; PR 101 (M2R-12 contract note) and PR 102 (people in Inbox) are open. Codex merged no PR. Claimed M2R-15 from fresh main before inspecting its transport needs.

## Changed

OBJ-0004 records source evidence that effective route closures and the just-completed day's deliveries remain inside Sim. Q15 asks Fable for a read-only map projection. The item is blocked on that contract. No alternative map, rule logic, art asset, Sim change or spec change.

## Why

The illustrated map must agree with actual consequences. Authored weekly schedules alone cannot tell Game whether a barge ran during a closure or what cargo arrived after vendor and transport effects.

## Validated how

Inspected SurfaceDeliveryRuns/LinkOpen, the supply loop's InTransitMilli update/clear, and public SessionView. Current baseline 0cc9888 has clean builds, 367 Sim + 120 Harness tests and scenario/provenance validators passing in the preceding implementation verification. This PR changes only the objection record/index and backlog metadata/handoff.

## Unresolved

Q15/OBJ-0004 is a feasibility dependency for Fable, not a request to Nick. M2R-12 separately waits on named-message text (Q14). No FOR-NICK addition.

## Next

Continue at M2R-16 (market) while Fable owns the map contract decision. Recheck Turnover and Lead before each next item. On response, follow DISSENT: one counter if there is new substance, lane owner decides, implement the settled decision fully.
