**Work session handoff** (Fable, 19 Nov 2026, 14:45 UTC scheduled run)

Expected look of this build and why: one visible change. On days 5 to 8 of the tutorial the forklift request's order-by line no longer names Harbor Equipment (DESK-111), and the debrief's gap line and the COR list read as DESK-112 says. The vendor card's heard line and the quote's button wait for Codex (DESK-113, DESK-114).

## PRs reviewed this session

- PR 348 (Codex, the 14:00 queue-check handoff): merged. One Markdown file under docs/handoffs, every CI check green.
- PR 349 (Fable, the day's play, D39 and rows DESK-111 to DESK-114): merged on green CI.
- PR 350 (Fable, DESK-111) and the DESK-112 PR stacked on it: merged on green CI, or left with "Fable review: ready to merge" if a merge was refused.

## Changed

- docs/reviews/2026-11-19-claude-play.md: the day's play, sorted (30 notes), the four answers, Fable's reading and the Sim pass. Logs and four pictures under docs/reviews/plays/2026-11-19/ (plain PNG, each under 200 KB).
- docs/design/desk-redesign.md section 14: D39 (what the shop has been told reaches the advice), with Sim notes for DESK-111 and DESK-112.
- backlog: DESK-111 to DESK-114; PLAY-03 status; status line.
- DESK-111 (branch fable/DESK-111): `OrderBy` reads the request's parts and the learned ones, and passes over a vendor the shop has heard is short while the effect runs; `VendorCard.Heard` and `VendorOption.Heard`. HeardTests; NewsTests' day 5 case changed (stated in the row). Goldens and batch unchanged.
- DESK-112 (branch fable/DESK-112): the debrief's gap line names the capability's own plant parts; the editor's COR line says when naming him lowers the watch on a contract he already watches; Mills promises a note each day. PlaySentencesTests; CorFitTests' Lane line. Goldens move on the gap-tag line only (eight sustainment and spoke-b reports, listed in the row).

## Why

PLAY-03: the first run on or after 14:00 UTC on a day with no play, with game/, src/ and scenarios/ changed since 18 Nov. The play's leading finding is that on day 5 the game's advice did not take in what the player had just been told (Dunn's drivers, Harbor's shortage): D39. DESK-111 and DESK-112 were the next unblocked Fable rows.

## Validated how

The play ran on Godot 4.6.1 .NET under an authenticated TCP-loopback Xvfb display, 1280 by 800, Dummy audio, seed 20261119, 273 commands to the debrief. The harness replays its input log to the same game (state hash 8d96b798). `batch tutorial --seeds 200`: unchanged from 18 Nov in every reported column. `dotnet test src/LinesOfSupport.sln` (841 Sim, 121 harness), validate.py, provenance.py and conflicts.py pass on DESK-112 (which carries DESK-111). The game project builds against the new optional fields. Godot, the build and the play's 274 screenshots stayed in the session's scratch directory and were deleted at the end of the run.

## Unresolved

Nothing for Nick. The stop (DESK-105) was not exercised: the player never made a buy it later learned was wrong, and the brief is fixed.

## Next

Codex: DESK-113 (the quote's warning carries "Add what you learned", and the small screen fixes) and DESK-114 (the heard line on the vendor card). Fable: no unblocked DESK row before the gate after DESK-112. The next Claude play is due with the first run on or after 14:00 UTC on 20 Nov, if game/, src/ or scenarios/ changed. Nick plays again when he chooses.
