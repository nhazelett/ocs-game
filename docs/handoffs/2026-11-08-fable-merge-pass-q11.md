**Work session handoff: Fable, scheduled run, 8 Nov 2026 (merge pass, Q11, M2-14, M2-13)**

Expected look of this build and why: once the stack is merged, the board shows a third island (Third Island, Spoke C) with a ferry link to the hub, and a water card (req0012) in the inbox from day 1. Spoke C water goes red on day 7 if nothing is bought. A refused handshake can now produce a "Shop hours short: ... waits behind other work" record, and the debrief can name a "Priority deferred" factor. The ledger can show per-arrangement unobligated incurred cost once Codex binds the new field (M2-05).

## PRs reviewed this session

- PR 44 (Codex M2-16, conflict-marker CI): in lane, green, merged.
- PR 41 (Codex M2-07, debrief screen): in lane, green, merged.
- PR 45 (Codex M2-05 status correction, Q12): backlog and questions only, green, merged.
- PR 42 (Codex M2-10, prep screen): conflict in game/scripts/GameShell.cs (both sides added a refresh call). Fable kept both, the game project built, CI went green, merged with a comment.
- PR 43 (Codex M2-15, BPA from directory): conflicts in GameShell.cs, ci.yml and BACKLOG.md, all keep-both. Fable resolved them, the game project built and CI is green. **Not merged**: the merge call was refused by the session's permission check (see FOR-NICK #1).
- PR 46, 47, 48 (Fable): opened this session and green (48's last checks were still running at handoff). Not merged, for the same reason.

## Changed

- Q11 (PR 46): read-only `Arrangement.UnobligatedIncurredCents` (zero clamp, zero for coordination). The metric `cost.unobligatedIncurredCents` now sums it. ADR-0006 addendum. Q11 and Q12 answered; M2-05 unblocked.
- M2-14 (PR 47, stacked on 46): commander confidence. A refused handshake marks the site. On a short day its work goes after all other ready work, in `actors.commander.priorityOrder` among such sites; site-less work is never held back. Adds a record, `SessionView.LostConfidenceSites` and `GapFactorKind.PriorityDeferred`. Rule table in docs/assumptions.md.
- M2-13 (PR 48, stacked on 47): Spoke C on Third Island, water card asking for bottled water only, two local vendors (8 vendors across 3 islands). Same Spoke C handling on day 2 in every approach log. Goldens regenerated.
- This PR: backlog status line and rows (M2-06, 07, 10 and 16 Done), FOR-NICK #1, this note.

## Why

Q11 blocked Codex's ledger. M2-14 gives SPEC 4.5's refusal its priority-order penalty. M2-13 brings Spoke B to the SPEC 11 first-playable shape (hub plus two spokes).

## Validated how

- 229 .NET tests pass on each branch. validate.py, provenance.py and conflicts.py pass.
- No golden changed for Q11 or M2-14.
- M2-13 goldens diffed by hand. Spoke B strips match through day 25; later failure rolls shift because the shared random stream now also serves Spoke C.
- 200-seed medians: textbook 0.912 > greedy 0.765 > do-nothing 0.655.
- The Codex conflict resolutions were build-checked with `dotnet build game/LinesOfSupport.Game.csproj`. Godot smoke tests ran only in CI.

## Unresolved

- FOR-NICK #1: whether scheduled runs may merge. PR 43, then PRs 46, 47 and 48 in that order, are waiting on a merge.
- The random stream is shared across sites, so adding a site moves late failure rolls at other sites. That is fine for now, but per-arrangement streams would make goldens steadier. Candidate for M3, not filed.
- In greedy's report, the Spoke C gap from day 15 names only the vendor failure, not demand above supply. The debrief's demand factor may skip consumables. Fable will check this next session.

## Next

Codex: M2-05 on the new field. If the debrief maps factor kinds to labels, add one for `PriorityDeferred`.

Fable: after the stack merges, check the consumable demand factor and add the next SPEC 11 first-playable items (6 capabilities, 3 event chains).

Nick: FOR-NICK #1, then the M2-11 playtest.
