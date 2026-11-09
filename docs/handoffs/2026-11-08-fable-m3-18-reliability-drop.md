**Work session handoff**

Expected look of this build and why: no visible change. No shipped scenario authors a drop, and their only inherited rows are host nation support, which the curable change leaves alone.

## PRs reviewed this session
- Same session as docs/handoffs/2026-11-08-fable-m3-17-file-review.md: PRs 78 to 83 unchanged and already marked ready; PR 84 still red only on the `godot-shell` shutdown crash after a rerun (Q13); Codex PR 85 (M3-11 diorama) marked ready to merge. No merges (FOR-NICK #1).

## Changed
- fable/M3-18, stacked on fable/M3-17 (PR 86). Event effect `vendor.reliabilityDrop` (inherited arrangement, new reliability from the event day, never raises); predicates `arrangement.noCureNotice` and `arrangement.notReplaced`; bundle checks for their targets and the value. New `Session.Reliability.cs`.
- Inherited contracts with written terms are now curable: performance issues, cure notices, COR designation and COR-reported failures, like the office's own awards.
- Debrief: a failure after a drop that a performance issue opened before it had warned of is Avoidable ("had shown the slips"); without that warning it stays Unforeseeable with a note naming the drop.
- ADR-0005 addendum, assumptions.md row and debrief tag row, backlog M3-18 status, M3-21 widened to the M3-18 validate.py checks.

## Why
docs/design/M3-07-representative-scenario.md chain 3 (bFuel slips, then defaults unless cured or re-sourced) has to be authored as data, and bFuel is inherited, so the M2-18 issue flow had to reach inherited contracts.

## Validated how
- ReliabilityDropTests (10): the drop lands on its day; over 200 seeds it fires exactly when no cure notice was delivered or in work (both branches exercised); a replacement delivering by the day holds it; failures after the drop more than double against a no-drop control over 40 seeds; debrief Avoidable after reported slips, Unforeseeable without; an inherited contract opens an issue, takes a cure notice and a COR, host nation support still does not; shipped scenarios inherit only host nation support; three bad events refused.
- Mutation: either predicate forced true, or the avoidable branch removed, each fails a test.
- No golden changed. 319 Sim and 120 Harness tests; validate.py and its pytest suite, provenance.py, conflicts.py.

## Unresolved
- Drops can target only inherited arrangements (award ids are chosen in play). Enough for M3-07.

## Next
- Fable: M3-19 (termination notice and claims), then M3-07. Stack on fable/M3-18.
- Codex: M3-20, M3-21 after the Fable PRs merge.
