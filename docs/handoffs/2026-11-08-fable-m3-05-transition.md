**Work session handoff**

Expected look of this build and why: no visible change in the game. The transition plan exists in the Sim, the harness report (a TRANSITIONS section and TRANSIT timeline lines) and two new goldens; the transition planner screen with overlap bars is new UI and waits for the M2 gate.

## PRs reviewed this session
- Opened PR 82 (M3-05 transition plans, stacked on 81) and PR 83 (M3-07 plan and backlog rows M3-16 to M3-18, docs only, stacked on 82).
- PRs 78, 79, 80 and 81 (Fable, stacked in that order): "ready to merge" comments already stand and no head has moved since, so no new comment. No open Codex PRs. Scheduled run: no merges attempted (FOR-NICK #1).

## Changed
- fable/M3-05-transition, stacked on fable/M3-06-offisland (PR 81). New `Session.Transition.cs`: `CheckTransition`, `ApplyPlanTransition`, `IncomingStart` (known start, or a projected range for an external request not yet awarded), `TransitionWarnings`, `TransitionClocks` (in `AdvanceDay` after COR reporting), views and `TransitionFactors` (debrief).
- `PlanTransitionAction` (`kind: "planTransition"`); `TransitionState`, `TransitionOutcome`, `RecordKind.Transition`; rules `transition` block (`defaultOverlapDays` 2, `warnDays` 5) in rules-2025-10.json and the schema; `TransitionView`, `TransitionPreview`, `SessionView.Transitions()` and `PreviewTransition`; `GapFactorKind.TransitionGap`; metrics `transitions.*` only when a plan exists.
- Goldens (spoke-b-90): `approach4-transition-plans` (the paired approach 4 log with two plans instead of the day 19 hand termination: gens still ends day 19, the turnkey UCA cuts over to the external contract on day 49 instead of running to 51; both clean) and `approach4-transition-gap` (25-day bridge: the plan on day 17 shows a 5-day gap, the board flags it day 36, power is down days 42 to 46, the debrief tags it avoidable through the planner's warning).
- ADR-0005 and ADR-0006 addenda, assumptions.md "Transition plan" route row and "Transition gap" debrief tag row, backlog M3-05 row and status line.

## Why
SPEC 2 (the OCS seat plans transitions), SPEC 3 "Transition plan" (outgoing and incoming on a timeline with overlap, handoff conditions, who owns the gap; highlights gaps in coverage and funding; the gap becomes real if left there), SPEC 4 scorecard "transitions completed without a gap", SPEC 5 approaches 3 and 4 (two-day overlap; the enduring answer must overlap the bridge).

## Validated how
- TransitionTests (21): the plan reproduces the hand termination day; no plan means no transition metrics (old hashes hold); over 20 seeds the cut-over never starts before the overlap is verified or within the overlap after a failure; an onDay handoff; the gap golden's warning day, plan text, Gap outcome and avoidable debrief factor; the morning board item; a lost incoming (terminated before it starts) flagged and resolved as a gap; an external request's projected start range narrowing to the award's start day; 11 hard stops with preview equal to Apply and the hash unchanged; a second plan on the same outgoing refused; save and resume mid-plan on five days.
- Mutation: disabling the cut-over fails 5 tests; disabling the warning fails 4.
- No existing golden changed. 280 Sim and 120 Harness tests; validate.py and its pytest suite, provenance.py, conflicts.py.

## Unresolved
- No extension of the outgoing by modification: a projected gap is closed by another award or an earlier incoming. No phase-in or phase-out scope and no acceptance step at handoff. Owner is a label only. Planning costs no shop hours. An early end of a UCA returns no money (existing termination rule).
- Awards still in shop work cannot be an incoming yet (only arrangements and external requests); plan after the award completes.
- The warn and overlap defaults are placeholders for playtest.

## Next
- Fable: M3-16 (inherited arrangement fidelity), then M3-17 and M3-18, then M3-07, per the plan in docs/design/M3-07-representative-scenario.md (PR 83, stacked on PR 82; stack the next branch on fable/M3-07-plan).
- Codex: M3-12, M3-13, M3-14, M3-11; M3-15 after the stack merges. Codex `validate.py` needs no change for this PR (the rules block is schema-checked).
