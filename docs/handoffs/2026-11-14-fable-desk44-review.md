**Work session handoff**

Fable, scheduled run, 14 Nov 2026 around 18:45 UTC. No Fable DESK row was open (DESK-11 and DESK-12 wait for the gate), so this run reviewed and merged Codex's two open PRs and wrote up what the review found.

Expected look of this build and why: every short capability now says why. On Today and on the request page the Sim's sentence sits under the short request ("Nothing supplies water at Hub until Main Island Utilities (host nation) starts on day 9."). On the map a resting card is as before; a click on it or its island opens one paper card with each short capability and its reason, and the hint beside Fit map reads "Click a site to see why it is short." A "Who can help" entry that has been answered ends with the answer, with no hours line under it.

## PRs reviewed this session

- 252 (Codex DESK-44, the reason beside every shortfall): CI green, merged. Meets the row and Q24: the resting card is unchanged, the opened card is opaque, inside the map, wraps the full sentences, and Spoke B's busy day 11 fits (power, fuel and lodging with their three reasons). As a first-time player, two reading problems in its pictures, both DESK-54 for Codex: on Today the reason sits outside the request's frame, between two requests, and reads as an entry of its own (the same problem DESK-52 fixed for decisions); and in the opened card the capability and its reason are the same grey, so six lines read as one block where the resting card's red would let the eye find each capability first.
- 253 (Codex DESK-53, an answered helper reads as done): CI green, merged. Before, pending and answered pictures show the hours line only while someone is asking, and the answered entry ends with Dunn's words. Its pending picture shows something older: the one-line message after an action ("The task is on your team's work list.") pushes the panes up and the map loses about 36 px while it shows. That is DESK-55 for Codex.

Combined tree (main plus 252 plus 253) tested before merging: 687 Sim and 121 Harness tests pass; scenarios and provenance pass; no conflict markers. Neither PR touches the Sim or a scenario, so no balance check.

## Changed

- backlog/BACKLOG.md: DESK-44 and DESK-53 Done, DESK-54 and DESK-55 added for Codex, status line.

## Next

Codex: DESK-54, DESK-55, ART-20. Then Nick plays again (docs/PLAYTEST.md). DESK-11, DESK-12 and other Sim depth wait for that.
