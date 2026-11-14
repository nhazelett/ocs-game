**Work session handoff**

Fable, scheduled run, 14 Nov 2026 around 20:45 UTC. No Fable DESK row was open (DESK-11 and DESK-12 wait for the gate), so this run reviewed and merged Codex's open PR and wrote up what the review found.

Expected look of this build and why: on Today each short request reads as one paper frame: title and asker, the state, then the Sim's reason ("Nothing supplies water at Hub until Main Island Utilities (host nation) starts on day 9."), with nothing loose between frames. On the map the opened card lists each short capability in the resting card's red, each followed by its reason in ordinary ink, so the eye finds the capability first and then why.

## PRs reviewed this session

- 255 (Codex DESK-54, the reason reads as part of its shortfall): CI green, merged. Today, request, tutorial resting and opened, and Spoke B opened pictures all meet the row: the reason sits inside its request's frame under the state line, and the opened card alternates red capability and ink reason. As a first-time player, one older problem shows in the Spoke B day 11 picture (it was already in PR 252's): the opened card sits over the top of the map and covers most of Spoke B, its name and its ring, so the player reads why a site is short without seeing the site. That is DESK-56 for Codex: the card opens beside its site, with the site in view.

Combined tree (the branch was already on main) tested before merging: 687 Sim and 121 Harness tests pass; scenarios and provenance pass; no conflict markers. The PR does not touch the Sim or a scenario, so no balance check.

## Changed

- backlog/BACKLOG.md: DESK-54 Done, DESK-56 added for Codex, status line.

## Next

Codex: DESK-55, DESK-56, ART-20. Then Nick plays again (docs/PLAYTEST.md). DESK-11, DESK-12 and other Sim depth wait for that.
