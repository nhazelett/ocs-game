**Work session handoff** (Fable, 18 Nov 2026, 14:45 UTC scheduled run)

Expected look of this build and why: no game change. The build is main at 0a42ce1 (the D36 request page, DESK-93 to DESK-101); this PR adds the day's play report, its rows and one decision.

## PRs reviewed this session

- PR 327 (Codex, the 14:00 queue-check handoff): merged. One Markdown file under docs/handoffs, every CI check green.

## Changed

- docs/reviews/2026-11-18-claude-play.md: the day's play, sorted (35 notes), the four answers, Fable's reading and the Sim pass. Logs and four pictures under docs/reviews/plays/2026-11-18/ (plain PNG, each under 200 KB).
- docs/design/desk-redesign.md section 13: D38 (the debrief says what earned each line of the bar) with Sim notes for DESK-102, DESK-103 and DESK-106.
- backlog: DESK-102 to DESK-109; PLAY-03 status; status line.
- backlog/FOR-NICK.md #23: stopping a buy the shop is still writing.

## Why

PLAY-03: the first run on or after 14:00 UTC on a day with no play, with game/, src/ and scenarios/ changed since 17 Nov. The new rows are from that play: two Sim-view defects (DESK-102, DESK-103), one decision (D38) and its two rows, a set of small screen defects (DESK-108) and one driver defect (DESK-109). Stopping unsigned work is a new player action, so it goes to Nick as #20 did (DESK-104, DESK-105 wait on it).

## Validated how

The play ran on Godot 4.6.1 .NET under an authenticated TCP-loopback Xvfb display, 1280 by 800, Dummy audio, seed 20261118, 235 commands to the debrief. The harness replays its input log to the same game (state hash ad72dce1). `batch tutorial --seeds 200`: unchanged from 17 Nov in every reported column; textbook meets every pass line in every seed. No code changed, so no tests were rerun beyond the build the replay needed. Godot, the build and the play's 236 screenshots stayed in the session's scratch directory and were deleted at the end of the run.

## Unresolved

FOR-NICK #23 (stop a buy still being written; Fable recommends yes, before the gate).

## Next

Fable: DESK-102, then DESK-103, then DESK-106. Codex: DESK-108, then DESK-107 after DESK-106; DESK-109 when a slot is free. The next Claude play is due with the first run on or after 14:00 UTC on 19 Nov. Nick plays again when he chooses.
