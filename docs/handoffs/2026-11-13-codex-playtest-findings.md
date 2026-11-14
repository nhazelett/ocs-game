**Work session handoff** (Codex, Nick's 13 Nov 2026 Eastern playtest feedback)

Expected look of this build and why: no visible change. This records Nick's review of the Windows demo and his day-four screenshot so Fable can write the next UI/UX specification.

## PRs reviewed this session

Live inventory: no open PRs at the start. Pulled main `826753f`; the previous 02:00 handoff is merged. This session follows Nick's direct request to record new playtest findings rather than taking an implementation row.

## Changed

- Added section 7 to docs/design/desk-redesign.md, preserving earlier decisions. Eight finding groups cover teammate capacity at assignment; unexplained water/food shortfalls; decision history; the full day's execution preview; a separate summary of actual end-of-day results; requests and conversation discovery; new pictures for the whole cast, preferably 3D; and the screenshot's repeated staff-meeting status/cost/queue presentation.
- Captured the visible day-four evidence: Hub water and food at 0%, three people at 8 of 8 hours assigned, two differently priced previews, 154 queued hours and a duplicated meeting warning. The screenshot remains in Nick's chat; this PR records its observations in text. No causal bug is claimed without the save or input log.
- Linked the findings from docs/PLAYTEST.md and added Q22 @Fable in backlog/QUESTIONS.md, requesting design decisions, any required read contracts and ordered acceptance-scoped work. The newer request explicitly revisits section 6's excluded task/status surfaces and ART-11's earlier Later label.

## Why

Nick says the game is getting there but still feels "juggly", the UI/UX needs an overhaul, and the characters need replacement. His instruction is to record the notes so Claude/Fable can build the specification accordingly. Forecasting the day's work and reporting actual results are two separate requested outcomes. The feedback does not supply a passed DESK gate.

## Validated how

Release solution build passes with eight existing warnings and zero errors. All 757 .NET tests pass (636 Sim, 121 Harness), zero failed/skipped; VSTest prints the existing artifacts-directory fallback notice. Scenario validation passes (five folders, one rules file), as do asset provenance, tracked conflict markers and whitespace checks. Reviewed the Markdown and local cross-links against the supplied notes and visible screenshot.

Documentation only: no Game, Sim, scenario, rules or asset changes; no new screenshots, native Godot check, Windows execution or performance measurement in this session. Publication uses the authenticated GitHub connector because the terminal's push credentials were unavailable earlier in this conversation.

## Unresolved

Q22 awaits Fable's specification and ordered rows. The actual causes of the water/food shortfalls and the meaning of the displayed queue must be checked against game state, rather than inferred from the image. Fable decides the new portrait implementation and visual acceptance bar. No new FOR-NICK entry or owner approval request.

## Next

Fable reviews this playtest record, writes the next design/specification pass and orders the corresponding Sim and Codex rows. The UI gate remains unresolved. Codex has not started that overhaul or changed the active backlog headline.

No merge, main push, force-push or shared-branch deletion by Codex.
