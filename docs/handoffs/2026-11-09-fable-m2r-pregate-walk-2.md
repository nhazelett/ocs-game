**Work session handoff** (Fable, scheduled run, 9 Nov 2026 about 09:00 UTC)

Expected look of this build and why: the same screens as main plus #119 to #124. This PR changes only docs and the backlog; no game or Sim code.

## PRs reviewed this session
- #125 (Codex, M2R-22 fuel owner on the checklist and Ask the J4, stacked on #123): ready to merge, commented. CI green. Screenshots opened: the checklist names the J4 (ACSA) or DLA Energy as fuel's usual owner and that nobody engaged them, with the ask right under it, defaulted to Spoke B and 1,170 gal/day.
- #126 (Codex, M2R-25 seven tiles fit, quieter previews, stacked on #125): ready to merge, commented. CI green. Screenshots opened: all seven tutorial hub tiles inside the card; the finance preview has no zero-queue clause.
- Combined tree of main plus #124 plus #126, with the backlog conflict resolved (take #124's rows, Codex's status cells for M2R-22 and M2R-25, no blank lines): dotnet test 602 passed, game project builds, validate.py 0 errors, provenance passes, no conflict markers.
- #119 to #124: heads unchanged since their ready-to-merge comments; not re-commented.

## Changed
- Second pre-gate walk added to docs/reviews/2026-11-09-m2r-pre-gate.md, from the PR screenshots: what a first-time player now sees (no ids, the J4 on the checklist, people to reach, the tutorial mentor, the tutorial hub fitting) and what still trips them (the top strip, the "Day 0 results" header, dark-on-dark checkboxes). Gate readiness rewritten: merge both stacks, then M2R-26, then one walk on main.
- Backlog: new Codex row M2R-26 (top strip and first morning in plain words, before the gate); M2R-23 waits on it too; status line updated.

## Why
The walk was waiting on M2R-12, M2R-17 and M2R-22. All three now have reviewed screenshots, so the walk can be done now from them and the last one on main stays small.

## Validated how
Docs and backlog only. Every claim in the walk is from a screenshot named beside it. Conflict-marker grep clean.

## Unresolved
- The backlog rows of the two stacks conflict; resolution above, to apply at the check-in merge.

## Next
At the check-in: merge #124 and #126 (and M2R-26 if it is up), then the walk on main from fresh screenshots, then tell Nick M2R is ready for the gate.
