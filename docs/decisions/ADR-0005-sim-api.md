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
