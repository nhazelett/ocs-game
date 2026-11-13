**Work session handoff**

Fable, scheduled run, 13 Nov 2026 around 06:45 UTC. Review only: every Fable DESK row before the gate is Done, and DESK-11 and DESK-12 wait for the gate, so there was no Fable row to build. No PR merged.

Expected look of this build and why: unchanged from main f7a51dd (PR 208). ART-12 has not landed.

## PRs reviewed this session

- 209 (Codex ART-12, the map uses the real models): CI green, files all in Codex's lane, **not ready**. The kit models read well at zoom, the bladder and tank contrast fix works, and site names now sit beside the right island. Three blockers, in the PR comment:
  1. Spoke B day 11 at Fit map falls back to the name only ("Spoke B · down") because the new badges take the card's slots, so the map no longer says in words what is down (main does). Tutorial day 4 at zoom has the same gap. A down site keeps its facts at every fallback level.
  2. Tutorial day 4: the food service badge sits above the fuel bladder and its stem bends past it to the kitchen tent, so it reads as fuel down. Badges go straight above their own model.
  3. The two new PROVENANCE rows share one description.
  Not blocking: generator, CHU and dumpster are similar boxes at Fit map; the held-playback cargo pointer still crosses the Hub island, as on main.

## Changed

- backlog/BACKLOG.md: status line.
- This note.

## Unresolved

- ART-12 waits on Codex's fix pass and recapture; ART-13 is stacked behind it.
- FOR-NICK #16 is answered on the ART-12 branch (Codex records Nick's approval to open the PR); main shows it answered once PR 209 merges.

## Next

Nick plays the tutorial again (docs/PLAYTEST.md); the UI and UX pass starts with him. Fable reviews ART PRs as they arrive against docs/reviews/2026-11-12-art-review.md.
