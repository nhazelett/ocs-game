**Work session handoff** (Codex, 17 Nov 2026, DESK-93 fix pass)

Expected look of this build and why: unchanged from DESK-93's four committed review images. The game still prints Requests 1,170 gal a day; the commit-preview check now expects that grouped quantity.

## PRs reviewed this session

PR 312, the only open Codex PR at session start: Fable's CI comment and failed godot-shell log reviewed. No inline review thread or new objection needed a response. Read CODEX-SESSION, CODEX-KICKOFF, the specified SPEC sections, ADR-0001 to 0003, DISSENT and objection records; current questions have no unanswered request to Codex. The session's five-open-PR limit overrides the repository's ten.

## Changed

Merged current main (5a0444d) into codex/DESK-93 without rewriting history. Resolved the backlog-only conflict by retaining main's completed DESK-90 row and DESK-93's implementation status; retained Fable's new DESK-95 to 98 rows and status line. CommitPreviewSmokeTest checks the actual 1,170-gallon coordination and its invariant formatted sentence, together with its withdrawal consequence.

## Why

The test looked for 1170 as a substring of the displayed 1,170. This was the sole failing check reported by Fable. Action values and Game behavior do not change.

## Validated how

Solution and Game builds pass; all 921 .NET tests pass (800 Sim and 121 harness). Scenario, provenance and conflict validators pass. The formerly failing CommitPreview smoke test passes in Godot 4.6.1 .NET. The complete headless CI smoke set is running as this fix is published; its result will be recorded before session end. Existing warnings remain. No new screenshot is needed for the test-only fix; DESK-93 already includes four plain PNGs. Windows execution and ordinary-laptop performance are unmeasured.

## Unresolved

Hosted CI and Fable's visual review remain. No FOR-NICK entry or new question.

## Next

DESK-95, then DESK-96, in the scoped order. No PR merge, main push, force-push or shared-branch deletion was performed.
