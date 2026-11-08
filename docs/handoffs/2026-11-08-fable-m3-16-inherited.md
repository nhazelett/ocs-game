**Work session handoff**

Expected look of this build and why: no visible change. No shipped scenario sets the new inherited fields; they exist for M3-07 (Sustainment).

## PRs reviewed this session
- Same session as docs/handoffs/2026-11-08-fable-m3-05-transition.md: PRs 78 to 81 already marked ready, 82 and 83 opened and marked ready after green CI. No open Codex PRs. Scheduled run: no merges (FOR-NICK #1).

## Changed
- fable/M3-16, stacked on fable/M3-07-plan (PR 83). `AddInherited` takes `quantity`, `dailyCostCents`, `obligatedCents`, `fundingLine` and a live `ucaState` (`UcaState.PriorObligatedCents`, optional `quotedTotalCents`); UCA caps, the 50% trigger and definitization count prior obligations; no mobilization for rows started before day 1; supply lines sized on rows with a quantity. Bundle checks and schema. ADR-0005 addendum, assumptions.md row, backlog M3-16 status, new rows M3-19 (notice and claims, Fable) and M3-20 (validate.py mirror, Codex).

## Why
docs/design/M3-07-representative-scenario.md section 5: the Sustainment skeleton starts on day 90 with two UCAs on their own clocks and contracts already funded. The schema carried `ucaState` but the Sim ignored it.

## Validated how
- InheritedTests (12): shipped inherited rows unchanged; an inherited contract's quantity, price and obligation (fully covered, no mobilization); an inherited UCA with a proposal in hand comes due on day issue+180 with the HHQ day set; prior obligations trigger the 50% rule on the computed day and the definitized price funds price less all obligations; prior obligations use up the pre-proposal cap; a UCA past due before day 1 is overdue on day 1 and draws the inquiry; a missing proposal day is drawn deterministically; five bad rows refused.
- Mutation: dropping prior obligations from the cap room, or from the trigger ratio, each fails a test.
- No golden changed. 292 Sim and 120 Harness tests; validate.py and its pytest suite, provenance.py, conflicts.py.

## Unresolved
- CI: godot-shell crashed twice at Godot shutdown after MORNING_SMOKE_PASS on this PR, then passed on a rerun; asked Codex in QUESTIONS Q13.
- Costs incurred before day 1, modifications and invoices in work are not carried. `visibleProblem` is still ignored (M3-17). `noticeDays` is still ignored (M3-19).

## Next
- Fable: M3-17 (hidden problems and file review), M3-18 (reliability drop event), M3-19, then M3-07. Stack on fable/M3-16.
- Codex: M3-20 after this merges; M3-12 to M3-15, M3-11 as before.
