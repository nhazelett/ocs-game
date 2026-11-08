**Work session handoff: Fable, scheduled run, 8 Nov 2026 09:47 UTC (remaining first-playable rows)**

Expected look of this build and why: no change. Backlog and FOR-NICK only.

## PRs reviewed this session

- PR 43 (Codex M2-15): in lane (game/, .github/, backlog, own handoffs) and every check green. Merge through the REST API refused by the session's permission check ("Merge Without Review"), the third run in a row. Not retried through another tool.
- PRs 46 to 50 (Fable): green and clean, unmerged for the same reason. This PR (51) stacks on 50.

## Changed

- BACKLOG.md: status line; new rows M2-17 (six capabilities), M2-18 (vendor failure and cure notice chain), M2-19 (two seeded disruption chains), M2-20 (Codex: commander confidence in the game).
- FOR-NICK #1: 09:47 UTC refusal recorded; open queue is 43, 46, 47, 48, 49, 50, 51.

## Why

SPEC 9 first-playable row against what exists after PR 50: rules define six capabilities but Spoke B uses three (power, fuel, water); all five scenario events are fixed situations, so no seeded event chains exist yet; instruments, manual-versus-system, 8 vendors on 3 islands and one funding line are covered. M2-20 is the game side of M2-14 named in PR 47.

## Validated how

conflicts.py passes; docs and backlog only. No code was stacked on the unmerged queue (per the previous handoff: stacking more code raises conflict risk).

## Unresolved

- FOR-NICK #1: nothing merges from scheduled runs. Seven PRs wait, in order: 43, 46, 47, 48, 49, 50, 51.

## Next

Nick: answer FOR-NICK #1 or merge the seven PRs in order. Fable: M2-18 design can start on a branch from main once 46 to 50 merge; M2-17 after that. If the queue is still stuck, later runs should end quickly rather than stack more.
