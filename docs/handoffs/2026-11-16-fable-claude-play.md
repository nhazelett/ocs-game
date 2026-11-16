**Work session handoff** (Fable, scheduled run, 16 Nov 2026 14:45 UTC)

Expected look of this build and why: unchanged. This PR changes documents and the backlog only; the build played is `main` at e669382.

## PRs reviewed this session

None were open at the start of the run.

## Changed

- docs/reviews/2026-11-16-claude-play.md: the first daily Claude play (PLAY-03), with every note sorted, the player's four answers and the Sim pass.
- docs/reviews/plays/2026-11-16/: the command log, the input log and four of the play's 352 pictures (plain PNG, each under 400 KB).
- docs/design/desk-redesign.md section 9: D25 (asking is worth it, and the debrief says who knew), D26 (goods delivered carry no plant risk) and D27 (pick two only where price is open), with Sim notes.
- backlog/BACKLOG.md: rows DESK-57 to DESK-65, the PLAY-03 status and the status line.

## Why

Nick, 16 Nov: Claude plays the game daily, more critically than he would. A blind agent got only the brief in docs/PLAYTEST-CLAUDE.md section 3. Fable checked each of its notes against the play's text files, the Sim code and the harness replay of its input log before sorting it.

## Validated how

- The harness replays the play's input log to the same game: state hash 70bb7111, every pass criterion met.
- `batch tutorial --seeds 200` was run with the three policies.
- `tools/validate/validate.py` and `tools/validate/provenance.py` pass.
- No conflict markers.

## Unresolved

The player is a model. Notes 6, 25, 26 and 28 come from that and are listed but not acted on. Godot, the build and the play's pictures stayed in the session's scratch directory and were deleted at the end of the run.

## Next

Fable: DESK-57 (the day's results show a declined call), then DESK-58 to DESK-61 and DESK-65.

Codex: DESK-62 (the preparation screen keeps its buttons) first, then DESK-63 and DESK-64.

The next play is the first run on or after 14:00 UTC on 17 Nov, if `game/`, `src/` or `scenarios/` changed.
