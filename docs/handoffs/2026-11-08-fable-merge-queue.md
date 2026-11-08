**Work session handoff: Fable, scheduled run, 8 Nov 2026 13:47 UTC (one-click merge queue)**

Expected look of this build and why: once PR 52 merges, main gets everything in PRs 43 and 46 to 51: the Directory gains Establish BPA (M2-15), the ledger field for M2-05 exists, commander priority (M2-14) and Spoke C on Third Island (M2-13) are in the Sim and scenario, and the debrief names consumable shortfalls. No new behavior beyond those PRs.

## PRs reviewed this session

- PR 43 (Codex M2-15): in lane (game/, .github/ smoke step, backlog, own handoffs), all checks green. Reviewed the code: the form reads vendors, scope lines and vehicles from `ActionEditor`, and stops, shop hours and ready day from `PreviewEstablish`; no engine rule is copied into the game. Fit to merge. Merge through the REST API refused by the permission check ("Merge Without Review"), the sixth run in a row; not retried.
- PRs 46 to 51 (Fable): green and clean, unmerged for the same reason.

## Changed

- New branch `fable/merge-queue` (PR 52): origin/main, then codex/m2-15, then fable/m2-rows (which carries 46 to 50). One conflict, in BACKLOG.md rows M2-13 to M2-20; kept the Fable side, which already records M2-15 as awaiting merge.
- FOR-NICK #1 and the status line: 13:47 UTC refusal and the one-click option.

## Why

Six runs have ended at the same refused merge. Combining the queue costs Nick one merge instead of seven in order, with one known conflict already resolved. The original PRs stay open and untouched; GitHub marks them merged when PR 52 lands.

## Validated how

On the combined tree: `dotnet test src/LinesOfSupport.sln` (94 harness, 136 Sim, all pass), validate.py (3 scenarios, 0 errors), provenance.py and conflicts.py pass. Godot smoke tests run in CI only.

## Unresolved

- FOR-NICK #1: scheduled runs cannot merge.

## Next

Nick: merge PR 52 (or answer FOR-NICK #1). Fable: after it lands, M2-18 then M2-17 on branches from main; Codex: M2-05, M2-20. If still stuck, later runs end quickly.
