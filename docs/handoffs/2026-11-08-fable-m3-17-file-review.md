**Work session handoff**

Expected look of this build and why: no visible change. No shipped scenario has a hidden inherited problem (their inherited rows are host nation support, which has no file to review), so nothing is gated and the textbook policy does not review. The fields exist for M3-07 (Sustainment).

## PRs reviewed this session
- Scheduled run (FOR-NICK #1: no merges). PRs 78 to 83 unchanged since their "ready to merge" comments; not re-commented. PR 84 was red only on `godot-shell` (Godot shutdown crash after MORNING_SMOKE_PASS, QUESTIONS Q13); reran the failed job. No open Codex PRs.

## Changed
- fable/M3-17, stacked on fable/M3-16 (PR 84). `ReviewFilesAction` (`reviewFiles`) and `WorkKind.FileReview`: rules `fileReview.hoursPerArrangement` (4) per inherited contract file; each file yields a Knowledge record of its true state (UCA clock, IDIQ ceiling and ordered value, delivery record). New `Session.Files.cs`.
- Hidden problems (`visibleProblem: false`, new optional `reportedReliability`): `SessionView.ProblemHidden`, `ShownReliabilityPermille`, `IdiqTermsHidden`, `InheritedFiles()`, `PreviewReviewFiles`; `Known(record)` and the morning board drop a hidden UCA's overdue and proposal notices; the action editor nulls a hidden IDIQ's ceiling, ordered and guarantee; a hidden UCA's start record omits its clocks. A higher-HQ inquiry reveals a hidden UCA.
- Debrief: a vendor failure on an overstated inherited record and a stop-work on a hidden UCA are Discoverable while unfound, Avoidable once found (names the review or inquiry day).
- Textbook policy reviews every file on day 1 when there are any.
- Schema (scenario `reportedReliability`, rules `fileReview`), bundle checks, rules file, ADR-0006 addendum, assumptions.md rows (new row and the two debrief tag rows), backlog M3-17 status and new Codex row M3-21.

## Why
docs/design/M3-07-representative-scenario.md sections 3 and 5: cLodging (past-due UCA shown as an ordinary contract), aFuel and cFuel (IDIQ at 85% of ceiling) and bFuel (0.97 reported, 0.88 true) are the inherited traps the file review answers.

## Validated how
- FileReviewTests (17): shipped scenarios have no reviewable files; a hidden past-due UCA's notices stay unknown until the review, then the findings record links to the arrangement; a visible one is known at once; the HHQ inquiry reveals an unreviewed one; hiding changes no metric or money over 20 seeds; overstated reliability shown until reviewed; IDIQ terms hidden then shown; hours per file and double-review stops; bad ids refused; debrief Discoverable unreviewed and Avoidable reviewed, visible stays Unforeseeable; textbook reviews on day 1 and greedy does not; a review round-trips through a save; three bad rows refused.
- Mutation: ignoring the reveal in the debrief, or never revealing, each fails tests.
- No golden changed. 309 Sim and 120 Harness tests; validate.py, provenance.py, conflicts.py.

## Unresolved
- A hidden IDIQ's ceiling has no debrief factor of its own: the order refusal names the ceiling, and the resulting gap shows as an uncovered requirement. Revisit if M3-07 play shows the trap reads as unfair.
- The textbook policy reviews but does not yet act on findings (definitize an inherited UCA, cure an overstated vendor); M3-07 adds that.

## Next
- Fable: M3-18 (reliability drop event), M3-19, then M3-07. Stack on fable/M3-17.
- Codex: M3-20 and M3-21 after the Fable PRs merge; M3-11 to M3-15 as before.
