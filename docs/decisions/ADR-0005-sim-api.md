## Title
ADR-0005: Simulation API for the Harness and the Game

## Status (Proposed / Accepted / Superseded)
Accepted

## Context
Codex asked (Q5) for a harness-facing contract: load, seed, advance, apply dated actions, policies, metrics. The Godot desk will bind to the same surface. The shape below is what M0-05 implemented; it replaces the sketch in the Q5 answer where they differ.

## Options considered
1. Expose the mutable session to callers.
2. Session mutates only through `Apply` and `AdvanceDay`; callers read through a read-only `SessionView`, flat metrics and cause records.

## Decision
Option 2. Namespace `LinesOfSupport.Sim`:

- `ScenarioBundle.Load(scenarioDir, rulesDir = null)`: reads `scenario.json` and `scenarios/rules/rules-<rulesVersion>.json`; checks references and that every event, effect and prep kind is implemented.
- `Session.Create(bundle, seed, prepChoices = null)`; `session.Day` (day about to be played, from 1), `LastDay`, `Finished`.
- `session.Apply(PlayerAction) -> ActionResult` (Accepted, Message, HardStops). Actions: `survey`, `setLine`, `award` (instruments sf44, gpc, purchaseOrder, bpaCall, uca in M0), `coordinate`, `definitize`, `terminate`. Rejections change nothing except a `Rejected` record.
- `session.AdvanceDay()`.
- Read surface: `session.View` (`SessionView`), `session.Metrics` (flat `IReadOnlyDictionary<string,double>`, keys match `passCriteria.metric`), `session.Records` (`CauseRecord` with `CausedBy` ids), `session.StateHash()`.
- Saves and replays: `InputLog` (JSON: scenario, rulesVersion, seed, prepChoices, actions with `kind` first and `day`); `Replay.Run(bundle, log)`; `Replay.Run(bundle, seed, policy)`; `session.ToInputLog()`.
- Policies: `IPolicy { Name; PrepChoices(bundle); Decide(SessionView) }`; `PolicyCatalog.Create("do-nothing" | "greedy" | "textbook")`.
- Text report: `SessionReport.ToText(session, title)` (timeline, capability-by-day strip, scorecard, gap chains, pass criteria, hash).

## Consequences
- The Harness swaps `PlaceholderRun` for `Replay.Run` and prints `SessionReport.ToText` (backlog M1-01, Codex).
- The Game binds its panels to `SessionView` and records; it never mutates state except through `Apply`/`AdvanceDay`.
- Adding an action kind or a metric key is additive; renaming one is an ADR.

## Date
2026-11-08

## Author
Fable

## Addendum, 2026-11-08 (M2-12): establishBpa
Action `establishBpa { id, vendor, capability, scopeLines, callLimitCents?, orderingPeriodDays? }` (additive). Costs `bpaEstablish` shop days at today's manual or system speed; on completion the BPA joins `SessionView.Vehicles` (scenario vehicles plus established ones) and takes `bpaCall` awards with the existing checks and decline odds. Call limit defaults to the simplified acquisition threshold and may not exceed it; ordering period defaults to the rest of the scenario. `SessionView.PreviewEstablish(action)` returns hard stops, shop hours and the first day calls could be placed.

## Addendum, 2026-11-08 (M2-18): cureNotice and performance issues
A contract with written terms awarded by this office that under-delivers on `performance.underDeliveryDays` of the last `windowDays` days opens a performance issue (`SessionView.PerformanceIssues`, Inbox record). Action `cureNotice { issue }` (additive) costs `cureNoticeShopHours`; when delivered, a `cureDays` cure period starts, and at its end a roll on the new `performance` stream (`cureRecoverPermille`) decides cured (reliability raised to `curedReliabilityPermille`) or default (reliability drops by `defaultReliabilityDropPermille`). Re-source and terminate use the existing award and terminate actions; `SessionView.PerformanceDecision(issue)` reads the choice back for the debrief. New rules block `performance` (rules schema). Metrics `performance.issues`, `cureNotices`, `cured`, `defaults`. Textbook issues a cure notice the morning an issue arrives.

## Addendum, 2026-11-08 (M3-01): saves and resume
A save is an `InputLog` with `savedDay` (additive, optional): the day about to be played, with every action dated that day already applied and the day not advanced. `session.ToSave()` writes it (`savedDay` absent once the game is finished); `Replay.Resume(bundle, log)` replays days before `savedDay`, applies that day's actions in log order and returns the session on that day. A log without `savedDay` plays to the end as before, so every existing input log and golden is a valid save. Resume refuses a log for another scenario or rules version, a `savedDay` outside the scenario's days, or an action dated after `savedDay`. No state snapshot is stored: replay is the only load path, so a save can never disagree with the rules (SPEC 8); a snapshot for quick resume can be added later as a cache checked against `StateHash()`. The game autosaves `ToSave()` each morning after the day advances and on demand (M3-08, Codex).

## Addendum, 2026-11-08 (M3-06): establishIdiq, idiqOrder and the capacity event
Action `establishIdiq { id, vendor, capability, scopeLines, ceilingCents, minimumGuaranteeCents?, orderingPeriodDays?, fundingLine }` (additive). Single award. Costs `idiqEstablish` shop days (8 to 15) at today's manual or system speed. The minimum guarantee defaults to, and may not be below, `idiqEstablish.obligatesMinimumGuaranteeFraction` of the ceiling (5%), may not exceed the ceiling, and is obligated from `fundingLine` when the work completes (refused with a Rejected record if the line can no longer cover it). The ceiling may not exceed the warrant. On completion the IDIQ joins `SessionView.Vehicles` (kind `idiq`, `ceilingCents`) and `SessionView.Idiqs` (`IdiqState`: ceiling, ordered, guarantee not yet ordered). `SessionView.PreviewEstablish(EstablishIdiqAction)` returns hard stops, shop hours, the guarantee in `TotalCents` and the first day orders could be placed.

`award` with `instrument: "idiqOrder"` and `vehicle` orders against it: hard stops outside scope, past the ordering period, over the ceiling (counting orders still in shop work), wrong vendor or place of performance. Binding: orders are never declined, and they are not stopped at the vendor's capacity. An order spends the guarantee not yet ordered first (`Arrangement.FromGuaranteeCents`) and new money from `fundingLine` for the rest; a fully guarantee-funded order needs no funding line. Terminating an order never returns its guarantee-funded part. `cost.obligatedCents` adds the guarantee not yet ordered: it is owed whether or not it is ordered.

Capacity event: each day, for a vendor and capability with an IDIQ order running, if running arrangements exceed the offer's `capacityPerDay`, other instruments' arrangements (inside capacity when awarded) are served first and IDIQ orders share the rest oldest first; an order short of its quantity delivers the fraction it gets. New `RecordKind.VendorCapacity` (one record per episode, a cause of the shortfall), debrief factor `GapFactorKind.VendorCapacity` (avoidable: the order preview showed the capacity left). Not a performance issue: no `VendorFailure` record. No random draws, and games without IDIQ orders are unchanged (all goldens identical). `VehicleOption` gains optional `CeilingCents`, `OrderedCents`, `GuaranteeUnorderedCents`; the action editor lists `idiqOrder`, greyed until an IDIQ is open.

## Addendum, 2026-11-08 (M3-03): funding lines, purpose and a late release
Scenario `fundingLines[].capabilities` (optional; absent or empty means any) is the line's purpose. `FundingLineState` gains `PlannedAvailableDay`, `Capabilities` and `Pays(capability)`; `AvailableDay` gets an internal setter (still read-only outside the Sim). The scenario field `replacementArrivesDay` is removed (it was never read); a replacement is a second line whose release a scenario event can delay. New event effect `funding.late { target: line id, days | daysRange }`: the comptroller pushes the line's release back; it does nothing once the line is released. New hard stops (editor and `Apply`): a line whose purpose does not cover the capability (awards, `establishIdiq`, `definitize`); a severable service longer than `Session.SeverableServiceMaxDays` (365) that runs past its line's year end. Ratifications choose only a line whose purpose fits. An award rejected at completion for money now says why (expired, not released, or short). New `RecordKind.Funding` (appended), written at the end of each day for the next morning: a release, a late notice, the warning `Session.FundingExpiryWarningDays` (10) before a year end with money unobligated, the expiry, and the first day no line has money usable. No new random draws in existing streams: the delay is drawn from the events stream at creation, after the other events, so scenarios without the event replay unchanged.

## Addendum, 2026-11-08 (M3-04): CORs and report fidelity
Scenario `cors[]` (`id`, `name`, optional `unit`, `quality`) is the COR roster; rules `cor` gives `qualities` (`lagDays`, `reportPermille`), `noCorLagDays` and `reliefQuality`. A scenario without a roster behaves exactly as before. `AwardAction.Cor` (optional) designates a COR at award; a COR on an instrument without written terms, an unknown COR or a COR in a scenario without a roster is a hard stop. New `DesignateCorAction { Arrangement, Cor }` (`kind: "designateCor"`) designates or replaces the COR on a running or pending contract this office awarded with written terms; it takes effect at once, with no shop hours. `Arrangement.CorId` holds the designation. `Session.Cors` (`CorState`: `Id`, `Name`, `Unit`, `Quality`, `RotatedDay`), `Session.CorReports` (`CorReport`: arrangement, failure day, arrive day, COR or null for a unit report, the failure record, and the delivered record) and `Session.CorModeled` are read-only.

With a roster, a vendor failure on such a contract is reported, not seen: the COR's quality sets the lag and the odds (one draw per failure from a new "cor" stream, none at 100%, so other streams never shift); with no COR the unit reports a failure only when the site's capability was below full that day, `noCorLagDays` later. New `RecordKind.CorReport` carries reports and the rotation notice. Performance issues on these contracts open from reported failure days (same two-in-three rule), the issue text names the source, and its causes include the reports and the failures. Event effect `cor.rotates` (target: COR id) turns the COR into an untrained relief at `reliefQuality`; the designations stay. The textbook policy names the best COR on every written award and re-designates when a relief is worse than someone else on the roster; greedy names none.
