**Work session handoff**

Fable, scheduled run, 14 Nov 2026 around 22:45 UTC. No Fable DESK row was open (DESK-11 and DESK-12 wait for the gate), so this run reviewed and merged Codex's open PR.

Expected look of this build and why: when a player clicks a short site on the map, its opened card sits beside the site rather than over it. On tutorial day 4 the card for Hub opens to the right of the island; on Spoke B day 11 it opens below Spoke B. The island, its red ring and the resting "down" card that names it stay in view, and the card's thin tether runs to the site.

## PRs reviewed this session

- 257 (Codex DESK-56, the opened map card leaves its site in view): CI green, merged. Read as a first-time player against D18 and the row's acceptance. Spoke B day 11: the card opens below the island; the island, its three red markers, its ring and the "Spoke B · down" card are all clear of it, and the tether meets the ring. Tutorial day 4: the card opens to the right of Hub; it overlaps a little of the beach but none of the ring or the buildings, and "Hub · down" stays readable below. Type is unchanged at 17 px and every reason is whole. Nothing new to send back. Two things noted and left alone because they do not stop a player: the resting card and the opened card both show while the card is open (the resting one is how the site is named, so it should stay), and on Spoke B the opened card's top edge sits just under the "Barge lane" label.

Combined tree tested before merging: 687 Sim and 121 Harness tests pass; scenarios and provenance pass; no conflict markers. The PR does not touch the Sim or a scenario, so no balance check.

## Changed

- backlog/BACKLOG.md: DESK-56 Done, status line.

## Next

Codex: DESK-55, ART-20. Then Nick plays again (docs/PLAYTEST.md). DESK-11, DESK-12 and other Sim depth wait for that.
