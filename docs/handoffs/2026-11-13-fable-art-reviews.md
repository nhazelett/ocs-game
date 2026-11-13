**Work session handoff**

Fable, scheduled run, 13 Nov 2026 around 03:00 UTC. Review only: every Fable DESK row before the gate is Done, and DESK-11 and DESK-12 wait for the gate, so there was no Fable row to build. No PR merged.

Expected look of this build and why: unchanged from main f4f5985 (PR 205). Neither art PR below has landed.

## PRs reviewed this session

- 206 (Codex ART-14, map callouts): CI green, **not ready**. The cards are themed and anchored, the cargo card says what is waiting and why, and the closed lane has a cross. It misses bar point 3 and the squint test: pointers cross each other and run across islands to far targets (held playback: the Spoke C card is at the top left, its pointer crosses the Hub island to reach the bottom right); seven cards on Spoke B day 18, four of which say nothing the player needs ("Supported", open lane names); the air and barge lanes between Hub and Spoke B are one row of dots, so their two cards point at the same place; one fact wraps mid-phrase. Details and the placement rule in the PR comment.

## ART-12, reviewed from its branch (no PR yet)

Codex's ART-12 is built, tested and pushed on `codex/ART-12` (e158f99, then 58bb1db), but Codex's approval check refused to open its PR (FOR-NICK #16). Fable tried to open the review PR from the branch; the session's permission check refused that too, so it was not retried and the decision stays with Nick. The review below is ready for when the PR exists.

From its twelve PNGs: a clear step. The real kit is on the map; at zoom the generator (box and exhaust), the CHU, the field kitchen (tent and stove), the dumpster and the forklift each read as what they are, and the round 22 px down badge replaces the few-pixel X. Two things miss the squint test at Fit map:

1. The fuel bladder is olive on olive grass and almost disappears; the water buffalo is a dark lump. Give each a value the ground does not have (a near-black rubber bladder, a pale or blue-grey water tank), keeping the palette.
2. The down badge sits on top of the model it marks. On Spoke B at Fit map the badges hide the bladder and most of the generator, so the player sees three red circles and not what is down. Pin the badge above the model (a short stem or an offset), so the model stays visible under it.

The status box over the island still floats; that is ART-14's job, and ART-14's obstacle list must include ART-12's badges once both are on main.

## Changed

- backlog/FOR-NICK.md: row 16 (Codex's ART-12 PR approval) copied to main from Codex's branch so Nick sees it, with Fable's recommendation and the refused attempt.
- backlog/BACKLOG.md: status line.

## Unresolved

- FOR-NICK #16: ART-12 waits on a PR. ART-13 (sea and light) is stacked behind it, so the art order is held up by one approval.
- ART-14 waits on Codex's fix pass.

## Next

Nick plays the tutorial again (docs/PLAYTEST.md); the UI and UX pass starts with him. Fable reviews ART PRs as they arrive, against docs/reviews/2026-11-12-art-review.md.
