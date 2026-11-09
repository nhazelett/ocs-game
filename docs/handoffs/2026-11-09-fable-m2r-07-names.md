**Work session handoff**: Fable, scheduled run, 9 Nov 2026 (continued from M2R-04 in the same run). M2R-07, plain names in Sim text.

Expected look of this build and why: every Sim-made line the game shows now names things. Spoke B approach 1, before and after:

    before: BPA call gens awarded to Harbor Equipment Co; performance starts day 7, ends day 66.
    after:  BPA call awarded to Harbor Equipment Co for power at Spoke B; performance starts day 7, ends day 66.
    before: waterBridge missed today's delivery (no maintenance line, no plant operators line).
    after:  Coastal Provisions water at Spoke C missed today's delivery (no maintenance line, no plant operators line).
    before: Requirement req0012: Plant operators switched on.
    after:  The request for water for Spoke C: Plant operators switched on.
    before: ... Requirement req0007's dependency checklist showed fuel uncovered from day 1.
    after:  ... The dependency checklist on the request for power for Spoke B showed fuel uncovered from day 1.
    before: Inherited: Power at Hub from Main Island Utilities (host nation) (hostNation), days 1 to 400.
    after:  Inherited: Power at Hub from Main Island Utilities (host nation), days 1 to 400.

## PRs reviewed this session
See docs/handoffs/2026-11-09-fable-m2r-04-people.md (102, 103, 104, 106; and 107, M2R-18, ready to merge).

## Changed
- Session.Names.cs: `ArrName`, `FundingName`, `CardName`, `VehicleName`, `IssueName`, `CommitmentName`, `TransitionName`, `ExternalName`, `NameOf`, `InstrumentLabel`; every record, work label, stop, gap and debrief string in Core rewritten to use them.
- `WorkItem.SubjectId`: matching moved off `Label` (`SessionView.InWork`, the policies' `CardCovered`, board links). Without this the greedy policy would have changed behaviour, because it found its own awards by the old id-bearing label.
- `SessionView.ArrangementName`, `FundingName`, `VehicleName`, `CardName`, `NameOf` for Game-owned rows (Q14).
- PlainNamesTests: every golden and the three policies on all three scenarios; records, work labels, every morning's board and lead, debrief text, previews of every action played; fails on any identifier-shaped id (digit or inner capital) from the scenario, the rules (instrument and prep ids) or the session's own arrangements and issues.
- 20 tests' text expectations updated; all 33 goldens regenerated.

## Why
Design doc R6; Nick's first playtest complaint was `req0007` on screen. Unblocks Codex's M2R-12 for Sim-made strings (Q14 answered).

## Validated how
`dotnet test src/LinesOfSupport.sln`: 409 Sim and 120 Harness pass. On all 33 goldens the SCORECARD section and the PASS CRITERIA section (except the state hash line) are byte-identical to main: only text and state hashes moved. `validate.py`, `provenance.py` pass.

## Unresolved
- Hard stops for malformed input (an id that does not exist, an id already used) still quote the id; a correct UI never produces them.
- Some names repeat the vendor ("Harbor Equipment Co cured the performance issue with Harbor Equipment Co on power at Spoke B"; "Second Island Fuel fuel at Spoke B"). Readable; polish later.
- `Describe()` (action summaries used as record subjects for rejections and by the counterfactual list) still uses ids; the M3-09 picker will need names from it.
- The developer golden report (SessionReport) still prints ids; it is not player text.

## Next
M2R-06 (Spoke B fuel through the J4, after M2R-04) or M2R-08 (tutorial); M2R-09 is claimed by another session.
