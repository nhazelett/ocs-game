**Work session handoff** (Fable, scheduled run, 9 Nov 2026 about 03:45 UTC)

Expected look of this build and why: the same screens as main. The turnover's vendors card is shorter and grouped by island (M2R-19); the tutorial's shop card says each fact once; on the morning of day 11 of Spoke B approach 1 the lead's speaker line shows Capt Ana Ruiz, 3 CES, above the fuel failure (M2R-20). No game code changed.

## PRs reviewed this session
- #118 (Fable, backlog status and the M2R-12 and M2R-17 rows): ready to merge, commented. Green, merges cleanly, no markers.
- No Codex PRs were open.

## Changed
- PR #119, M2R-19: turnover says each thing once (vendor paragraph by island then by tier; "won't be up for weeks" for a system that comes up after the last day; tutorial shop and money remarks trimmed). Pre-gate review in docs/reviews/2026-11-09-m2r-pre-gate.md. Rows M2R-19 and M2R-20.
- PR for M2R-20 (stacked on #119, base main): site news comes from someone at the site; ADR-0006 addendum.

## Why
Design doc section 9: before calling M2R ready, Fable walks the first two minutes as a player. Every Fable M2R row was merged at the 03:30 to 03:46 check-in, so this run did the walk from the screenshots and the Sim's text on main.

## Validated how
dotnet test (592 passed), validate.py, provenance.py, pytest tools/validate, conflict-marker grep. Goldens unchanged on both branches.

## Unresolved
- The backlog status line was left to #118 (both edit line 1); after #118 merges, the next session adds M2R-19 and M2R-20 to it.
- For Codex (in the review): top strip wording ("Day 1 (next)", "0 h queued", the unlabeled bar), the stray "Spoke B power · schematic layout" label under the map, near-invisible unticked prep boxes, the tutorial still off on the front door (belongs with M2R-17), and refreshed screenshots from main for the gate.

## Next
When M2R-12 and M2R-17 merge: repeat the two-minute walk from fresh screenshots, then tell Nick M2R is ready for the gate. Until then nothing new on the Sim side is planned for M2R.
