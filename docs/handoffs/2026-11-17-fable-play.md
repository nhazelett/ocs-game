**Work session handoff** (Fable, 17 Nov 2026, 14:45 UTC scheduled run)

Expected look of this build and why: unchanged from PR 297 (DESK-71 on main). This PR adds the day's play report, its rows and D35; no game, Sim or scenario file changes.

## PRs reviewed this session

- None open at the start of the run.

## Changed

- docs/reviews/2026-11-17-claude-play.md: the day's play (PLAY-03), seed 20261117, on 23f51f9. Every pass line met with no gaps; 41 notes sorted.
- docs/reviews/plays/2026-11-17/: commands, input log, four plain PNGs under 400 KB.
- docs/design/desk-redesign.md section 11: D35 (the last day to order is one the player can trust) and Sim notes.
- backlog: DESK-85 to DESK-87 (Fable), DESK-88 to DESK-90 (Codex); PLAY-03 and the status line.

## Why

PLAYTEST-CLAUDE section 2: no play yet for 17 Nov, and game, Sim and scenarios changed since the 16 Nov play.

## Validated how

The input log replays through the harness to the same result (`run tutorial --log`, state hash c412d969). `batch tutorial --seeds 200` for the Sim pass. Each defect was checked against the code or the screen text before it became a row: `Session.News.OrderBy` (best roll, fastest route), `Session.Turnover` ("estimates" by info tier), the tutorial's food stock (three days) under the dining slips, the card surcharge in Island Mart's price, the Requests pane's text file after Add what you learned. Godot, the build and the 276 screenshots stayed in the session's scratch directory and were deleted.

## Unresolved

FOR-NICK #20 and #21 still wait on Nick. Nothing new for him.

## Next

Fable: DESK-85, then DESK-86 and DESK-87. Codex: DESK-68, DESK-83, DESK-84, then DESK-88 to DESK-90.
