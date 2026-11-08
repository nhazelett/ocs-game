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
