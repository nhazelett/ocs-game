**Work session handoff** (Codex, 21 Nov 2026)

Expected look of this build and why: no new visible change. PR 395 retains its seven reviewed plain PNGs and its DESK-133 screen changes; the checks run in separate scenes.

## PRs reviewed this session

PR 395: Fable's CI review requested a separate category scene and a merge of main. Pulled main at 3958b7c; merged it into the existing branch without conflicts or lost backlog statuses. No outstanding Codex question or objection. The session uses its explicit five-open-PR ceiling.

## Changed

Removed PlainBoardSmokeChecks from MorningSmokeTest. Added PlainBoardSmokeTest with its own 60-second CI command and required pass marker. It finds both categories in the original authored replay in Sim, then loads those exact mornings through the saved-game path to check Today and Inbox. The original category, enum-name and screenshot assertions remain, with an added exact-state comparison. No production code, Sim, scenario or timeout change.

## Why

The combined Morning scene exhausted CI's 60-second budget. The initial separated category scene passed locally in 53 seconds; loading the relevant mornings avoids rebuilding every intervening day's UI.

## Validated how

Solution build passed serially with .NET 8.0.428; all 997 solution tests passed (876 Sim, 121 harness), none failed or skipped. Game build passed with its existing nullable warning. Godot 4.6.1 headless: Morning 15.1 seconds, Performance 27.4 seconds and Contract 6.0 seconds pass. Final category timing is recorded in the fix comment. Scenario and provenance validators and git diff --check pass. Existing ObjectDB shutdown warnings remain. The default parallel solution build returned an empty failure; the serial build and complete test run succeeded. Windows, audio and frame-rate performance were not tested.

## Unresolved

Hosted CI and Fable review follow publication. No FOR-NICK or QUESTIONS addition. No PR merge, main push, force-push or shared-branch deletion.

## Next

DESK-98 is the highest unblocked Codex backlog row now that DESK-97 is on main; its absent remote claim branch was created before implementation. It uses DESK-133's assigned-action binding and will identify its dependency on PR 395.
