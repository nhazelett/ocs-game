**Work session handoff** (Fable, scheduled run, 10 Nov 2026 00:45 UTC)

Expected look of this build and why: unchanged. This PR is backlog and handoff text only. Main still has the pre-ART look (engine panels, illustrated 2D map); the ART stack (PRs 139 to 143) would change every screen to Tabletop when it merges.

## PRs reviewed this session

All five open PRs are Codex ART rows, entirely in Codex's lane, every CI check green, no conflict markers, no test output committed. Screenshots opened from each branch's docs/handoffs/images/ and read as a first-time player.

- PR 139 (ART-01, three art studies behind Developer tools): ready to merge.
- PR 140 (ART-02, Tabletop theme on every screen): ready to merge, only together with 142 or later. On its own, the old map's capability cards cover the site names and status lines.
- PR 141 (ART-03, seventeen fictional portraits): ready to merge as part of the stack.
- PR 142 (ART-04, the 3D map): not ready. (1) The morning map no longer says what is down: day 11 shows "Support interrupted" and three small red crosses on miniatures a new player cannot tell apart; the site line should name what is short in words. (2) The day 18 "Cargo waiting" label floats in the corner of the sea, attached to nothing, and does not say what is waiting or for where.
- PR 143 (ART-05, the day plays back): not ready only because it stacks on 142 and carries the same cargo label. The playback itself reads well.

Nick's Tabletop pick is recorded by Codex (ART-02 status, game/theme/README.md), not by Nick or Fable on main. The check-in should confirm it before merging.

## What a first-time player sees in the first two minutes on the ART stack (from the screenshots)

A warm paper title with New game and Continue; Maj Cole's Before you leave list in her voice with days to spend; then the board with Capt Ruiz's face, rank and unit beside her words, and three small islands with tents, tanks and generators joined by labelled barge and air lanes, each site captioned with its name and "Supported" or "Opens day 7". On day 11 the Spoke B caption turns red ("Support interrupted") and the lead says fuel is down; on the map itself the player cannot yet tell that power and lodging went with it, which is fix (1) above. Pressing Next day plays the day for five seconds with a "what happened" strip and a Skip button.

## Changed

- backlog/FOR-NICK.md #8: play the M2R gate on today's main, or after the ART stack merges; confirm Tabletop. Fable recommends merging ART first (after the two fixes), a fresh pre-gate walk, then the gate.
- backlog/BACKLOG.md: status line update; new Codex row M2R-34 (quiet morning: no disabled Open button when there is no lead).

## Why

Every Fable M2R row is done and the gate (M2R-23) is Nick's. The ART stack changes everything a first-time player sees, so its review and the gate sequencing are the useful work this run.

## Validated how

Backlog edits checked for conflict markers. No code changed.

## Unresolved

FOR-NICK #8. ART-06 (sound) is built on codex/ART-06 with no PR yet (Codex's five-PR cap).

## Next

When Codex pushes the ART-04 fixes, re-review 142 and 143 from new screenshots. If Nick chooses (b) in FOR-NICK #8, run the fifth pre-gate walk on the merged ART build. M3-23 waits for the end of M2R (Sim depth paused).
