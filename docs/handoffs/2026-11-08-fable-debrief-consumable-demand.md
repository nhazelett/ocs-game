**Work session handoff: Fable, scheduled run, 8 Nov 2026 08:47 UTC (consumable demand factor)**

Expected look of this build and why: no change on the board. In the debrief, a consumable gap caused by a daily rate below daily use now says so, for example greedy's Spoke C water from day 15: "Arrangements at Spoke C were sized for 1800 gal a day against demand of 2400 a day. Population rose to 160 on day 15, which the plan showed from day 1."

## PRs reviewed this session

- PR 43 (Codex M2-15): in lane and green. Merge refused again by the session's permission check ("Merge Without Review"). Not retried through another tool. FOR-NICK #1 updated.
- PRs 46, 47, 48, 49 (Fable, previous run): green, unmerged for the same reason. This PR stacks on 49.

## Changed

- `Session.Debrief.cs`: the demand-above-supply factor now covers consumables, comparing the contracted daily rate with daily demand (it was continuous only). Consumable text says "a day".
- DebriefTests: greedy's Spoke C water gap names demand above supply.
- assumptions.md gap table row reworded.
- Goldens regenerated: five reports gain one factor line each (Spoke B fuel 700 against 1170 gal a day where the handshake vendor was the only fuel source; Spoke C water in greedy). No strip, record or tag changed.

## Why

The previous handoff found that greedy's Spoke C water gap from day 15 named only the vendor failure. The real cause is that 1,800 gal a day was bought for 120 people and the site grew to 160; the debrief must teach that.

## Validated how

- 230 .NET tests pass. validate.py, provenance.py and conflicts.py pass.
- Golden diffs read by hand: only added factor lines, each numerically right (fuel demand 1170 = 260 people; water 2400 = 160 x 15).

## Unresolved

- FOR-NICK #1: nothing can merge from scheduled runs. Six PRs wait: 43, 46, 47, 48, 49, 50, in that order.

## Next

Nick: answer FOR-NICK #1 or merge the six PRs in order. Fable: next SPEC 11 first-playable items (6 capabilities, 3 event chains) once the stack is in; stacking more on six unmerged PRs raises conflict risk, so the next run should add backlog rows rather than code if nothing has merged.
