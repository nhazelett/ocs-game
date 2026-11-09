**Work session handoff**

Expected look of this build and why: the scenario picker lists a third scenario, "Sustainment: everything is already running" (the picker reads scenario folders, so no game change was needed). It opens on day 1 with fifteen inherited arrangements in the ledger and the inbox; everything else looks as before. Scheduled run, 9 Nov 2026; nothing merged (merges happen at Nick's check-ins).

## PRs reviewed this session
- None open at session start.

## Changed
- `scenarios/sustainment/scenario.json`: skeleton 3 as planned in `docs/design/M3-07-representative-scenario.md`, hub plus three spokes, 60 days, fifteen inherited arrangements, seven authored event chains plus the inherited UCA clock (chain 4), four trap cards. As-built differences are in section 8 of the plan.
- `scenarios/sustainment/golden/`: approach1-clocks-first, approach2-requests-first (the trap), approach3-cure-bfuel, approach4-resource-bfuel, and the three policy goldens at seed 1.
- `Policies.cs` (textbook): definitizes inherited UCAs the file review shows; holds requirements needed more than 14 days out until those definitizations are queued; a follow-on to vehicle-only support (an ACSA) goes to an open-market vendor. spoke-b and spoke-b-90 goldens unchanged.
- `Session.AddInherited`: an inherited arrangement's `CauseId` is its handover record, so cause chains end there and the crash run's no-cause check passes on inherited rows (it failed on every sustainment game before).
- Tests: `SustainmentTests` (five seeds distinct, every chain fires, clocks first beats the trap on the clock criteria, cure and re-source fix Spoke B fuel, textbook meets every pass criterion on most seeds, invariants with inherited clocks); sustainment added to `GoldenTests` and `BalanceTests`.
- Backlog: M3-07 implemented, awaiting review; new Fable rows M3-22 (file review finding text uses current reliability) and M3-23 (`initialBufferDays` ignored for `opensDay: 0`).

## Why
SPEC 11 M3 asks for the representative mission at intended quality. Sustainment is the scenario where the M3 systems (inherited clocks, file review, BPA ordering periods, IDIQ ceiling, cure notice, ACSA withdrawal, fiscal-year gap, COR rotation) are the game.

## Validated how
- `dotnet test src/LinesOfSupport.sln`: 342 Sim and 120 Harness tests pass. `validate.py` and `provenance.py` pass.
- `batch sustainment --seeds 200`: mission output median textbook 0.910 > greedy 0.700 > do-nothing 0.561; textbook 0 HHQ inquiries and 0 unobligated incurred cost on all 200 seeds.
- `crash-run sustainment,spoke-b,spoke-b-90 --games 300`: 300 passed.
- Read the four approach reports end to end: the gaps each one shows are the lessons the plan names.

## Unresolved
- M3-22 and M3-23 (above). Neither blocks the PR.
- Spoke C lodging degrades to 76% after day 30 in every approach: cLodging cannot grow and the only cash-free resize is the external support package, which no approach or policy requests yet (the OCS seat screen waits for the gate). Intended pressure, but the playtest should say whether it reads as unfair.

## Next
- Fable: M3-22, then M3-23; then the remaining M3 rows in SPEC 11 after the gate.
- Codex: M3-15, M3-20, M3-21 unchanged; nothing new for Codex from this PR.
