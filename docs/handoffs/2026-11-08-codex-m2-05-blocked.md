**M2-05 — missing per-arrangement ledger field**

Expected look of this build and why: no visible change. This records a contract dependency; it does not substitute an incomplete ledger.

## PRs reviewed this session

PRs 31 and 33 have been merged by Fable; PRs 34 and 35 contain the requirement/action editors. M2-01's main conflict was fixed with all checks passing. Codex merged none.

## Changed

Claimed codex/m2-05 and inspected the exact read surfaces. Recorded Q11 to Fable and marked M2-05 blocked on that field. No implementation or protected Sim/content edit.

## Why

M2-05 requires status, obligated, incurred and unobligated-incurred amounts for each arrangement. Arrangement exposes the first three facts but no per-arrangement unobligated-incurred amount. Session.Metrics and Debrief.Scorecard expose only a total. Core/Session.Day.cs calculates that total with the Sim's coordination exclusion and zero clamp; copying that business rule into Game would cross the stated boundary.

## Validated how

Read Core/State.cs Arrangement, Core/Session.Day.cs Metrics, Views/ViewModels.cs and Views/DebriefViews.cs. Searched Sim for Unobligated/unobligated/Ledger: no per-arrangement read field exists. Latest baseline build/tests and both validators pass. Documentation only; no replacement arithmetic or partial feature was shipped.

## Unresolved

Q11 @Fable requests an additive read-only Arrangement.UnobligatedIncurredCents property (or equivalent ledger row view) with the engine's existing semantics. This is a missing contract dependency, not a design objection or a Nick-only escalation. FOR-NICK.md unchanged.

## Next

Move to M2-06 immediately. Resume M2-05 on this branch once Fable supplies the field; do not invent it in Game.
