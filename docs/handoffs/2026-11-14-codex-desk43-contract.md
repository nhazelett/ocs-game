**Work session handoff** (Codex, 14 Nov 2026)

Expected look of this build and why: no visible change. DESK-43 is stopped on missing Sim timing for reassignment and other existing non-visit work; no incomplete picker is shipped.

## PRs reviewed this session

Live GitHub inventory: zero open PRs. No red CI, review comments or conflicts needed a fix. Read the latest Fable DESK-37 handoff and Codex 04:00 handoff, session/kickoff, required SPEC sections, ADR-0001 to 0003, DISSENT, backlog, questions, FOR-NICK and decided objections. No question or objection awaits a Codex response.

## Changed

Claimed codex/DESK-43 from main 9a84b49. Added Q23 for Fable and marked only DESK-43's own Status cell blocked. The active delegation's five-open limit takes precedence over repository wording.

## Why

D17 and DESK-43 require completion timing wherever work is assigned, explicitly including reassignment. AssigneeChoices dispatches AssignAction, SurveyAction, CoordinateAction and DefinitizeAction into the visit checker, which refuses them as "not a visit". Every ResultDay is null. HoursIfAssigned gives reassignment hours but no result day; queue order, waits and pace belong to Sim. The actual AssignAction keeps the work's existing position rather than appending a new job, so reusing a new-action forecast would also be incorrect. No Sim change is authorized by this Game-only row.

## Validated how

Source traced through Session.Team.BuildAssigneeChoices/PreviewAny, Session.People.CheckVisit, ApplyAssign and the existing Game assignment picker. Release solution build passes (zero errors, eight existing warnings); all 763 .NET tests pass (642 Sim, 121 Harness); scenario validation (five folders), provenance, tracked conflict markers and whitespace checks pass. No native screenshot is required for this documentation-only dependency record.

## Unresolved

Q23 awaits Fable's supported read projection. No Nick action, technical dispute or new objection. No game, Sim, rules, scenarios, goldens or assets changed.

## Next

Continue DESK-49, which has an existing staff-meeting read contract. Fable supplies Q23; Codex resumes DESK-43 after it is ready. Fable reviews and merges; Codex merges nothing.
