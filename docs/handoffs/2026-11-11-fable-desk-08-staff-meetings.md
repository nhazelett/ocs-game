**Work session handoff** (Fable, scheduled run, 11 Nov 2026 03:45 UTC, DESK-08, stacked on DESK-07)

Expected look of this build and why: no new screen. The visible change is in the day's records once the shop has joined the staff meeting: it is now every day, one hour, and most days it reads as ops talk, for example "Staff meeting. Col Helen Ward, Hub commander, ran it. Maintenance walked through the jets down for parts; supply is chasing those through its own channels. Nothing for contracting." On day 6 of the tutorial, if the shop is in the room: "Then the commander turned to the shop: \"The mayor's office tells me Main Island Utilities wants its honey trucks back before long. ...\" That is waste removal for Hub, wanted from day 13. Capt Ana Ruiz, 3 CES, will bring it to you tomorrow." Capt Ruiz then comes on day 7 instead of day 9.

## PRs reviewed this session

Codex 158 (DESK-20), 159 (DESK-21), 160 (DESK-25): heads unchanged since the 00:48 UTC re-check, CI green; still ready to merge, in that order. Codex 161 (ART-09): one new commit since the re-check, a handoff note and Q18 only; CI green; still ready to merge last in the Codex stack. Fable 162 to 166: green, mergeable, carried by 167; ready as part of the stack. Fable 167: unchanged since its review; ready. Q18 (Codex: may screen rows stack on unmerged Fable Sim branches?) answered yes on PR 161 and in its QUESTIONS row.

## Changed

- Rules `goingToPeople.staffMeeting`: `everyDays` 1 (was 7), `weeklyShopHours` 1 (was 2; it is the hours of each meeting), new `contractingPermille` 400 and `opsTalk` (six sentences). Schema too.
- Scenario `requirementCards[].heardAtStaffMeeting` (`fromDay`, optional `by`, `says`). The loader and validate.py require 1 <= fromDay < submittedDay. The tutorial's waste removal card has it (from day 6, said by the commander).
- Sim: one draw per meeting from a new "meeting" stream decides whether the meeting has anything for contracting (previews of requests due within seven days); otherwise one ops talk line, never the same twice running. Priorities only at the first meeting. A tagged request is raised whatever the draw and is on the desk the next morning. The recurring meeting goes to whoever got the shop in (DESK-02), and its text names them.
- `RequirementEditorView.HeardAt`: whether the shop was in the room. The debrief's uncovered-request factor adds "It was raised at the commander's staff meeting on day 6, when the shop was not in the room." when it arrived late.
- docs/assumptions.md (Staff meetings), design doc Sim notes, BACKLOG DESK-08 and DESK-23 rows, status line.

## Why

D4. Nick: "Some days it is sorties and downed jets and nothing for contracting." Being in the room is worth something only if it is sometimes empty and sometimes not, and if not being there costs time.

## Validated how

563 Sim tests (10 new in StaffMeetingTests, two GoingToPeople tests rewritten for the daily cadence) and 121 Harness tests pass. validate.py, the validator unit tests, conflicts.py, provenance.py, harness validate and the game build pass. Goldens: tutorial do-nothing and textbook, one debrief line each (the waste removal factor gains the staff-meeting sentence); no number moved, because no policy attends and a game that never attends draws nothing from the new stream. Spoke B goldens unchanged.

## Unresolved

The tutorial does not point the player at the meeting. Whether it should (a line from Capt Mills, say) is a playtest question; I left it out to keep the opening as Nick liked it. Spoke B has no tagged request; with its requests all in the first three days, its meetings are almost all ops talk after day 2, which is honest but makes the meeting worth little there.

## Next

DESK-09 (SF 44 for recurring buys: a trip and cash each delivery), stacked on this PR.
