**Work session handoff** (Fable, 17 Nov 2026, the 02:45 UTC scheduled run, continued in chat with Nick)

Expected look of this build and why: nothing in the game changed. This session is a review and the rows that follow from it. `main` at 76c69ca is the build reviewed.

## PRs reviewed this session

None were open at the start of the run or at its end.

## Changed

- docs/reviews/2026-11-17-gameplay-review.md: the thorough gameplay review Nick asked for in chat. Three blind players (tutorial careful, tutorial hasty, Spoke B cold), every note checked against the Sim and the game code, three logs replayed, 200 seeds of every scenario.
- docs/reviews/plays/2026-11-17-review/: each play's commands, input log and the player's own notes, and four pictures.
- docs/design/desk-redesign.md section 10: decisions D28 to D34.
- backlog/BACKLOG.md: rows DESK-67 to DESK-81 and the status line.
- backlog/FOR-NICK.md: #19 (changing a running contract) and #20 (requests and buying as full pages).
- docs/PLAYTEST-CLAUDE.md: the brief no longer says "from New game" (the driver opens inside it, and Back then led to a screen with no way out); a notes file for the player; two games at most on two CPUs; section 6b for a review on request.
- .gitattributes: pictures under docs/reviews/plays/ are plain files, as the playtest doc says. The 16 Nov play's four pictures had gone to LFS; they are plain now.

## Why

Nick, in chat: "I think we need a thorough review of the gameplay. You're authorized. Switching to Fable to direct changes." The daily play checks one cold tutorial run; this asked whether the game is worth playing through, which needed a hasty player, the main scenario, and a count of what each day asks.

## Validated how

- Each play's input log replays through the harness to the game the player saw (state hashes in the report).
- Every finding from the Spoke B play was checked against the Sim and the game code by file and line before it was sorted. The preparation screen's cause and the skip button's behavior are readings of the code, not reproductions by hand.
- `batch` over 200 seeds for tutorial, spoke-b, spoke-b-90 and sustainment.
- `python3 tools/validate/validate.py` and `python3 tools/validate/provenance.py` pass; no code or scenario changed, so `dotnet test` was not needed and was run anyway on main.

## Unresolved

- The first attempt at both tutorial plays ended on the preparation screen with no way out (DESK-67). They were rerun with the players told not to press Back.
- The Spoke B play stopped at the morning of day 17, at its step limit, when the driver stopped answering. The Sim did not hang; three games on two CPUs most likely outran the driver's 30 seconds. Unproven: Fable stopped that game before looking at it.
- D30 (skip ahead) amends Nick's D1, and D31 changes the tutorial's length. Both are written as built unless he strikes them, and they are the two for him to look at first.
- FOR-NICK #19 and #20 wait on Nick.

## Next

- Codex: DESK-67 first (it blocks any replay, Nick's included), then DESK-63, DESK-64, DESK-71, DESK-68.
- Fable: DESK-73 (answers and news reach the player), then DESK-74, DESK-77, DESK-78, DESK-76, DESK-75, DESK-80.
- The 17 Nov daily play is still due with the first run at or after 14:00 UTC. Its brief is the corrected one.
