## Title
ADR-0006: View-model contracts for the morning board and the two editors

## Status (Proposed / Accepted / Superseded)
Accepted

## Context
Backlog M0-07. The Godot desk (M1-05, then M2) needs a stable shape for what it shows: the top strip, the morning board, the requirement editor (OCS seat) and the action editor (CCO seat). SPEC section 2 makes the action editor's hard-stop versus soft-gap display one of the first UI experiments, so the editor has to be able to ask "what would this award do" without doing it. ADR-0005 already fixes that the Game reads through `SessionView` and changes state only through `Apply` and `AdvanceDay`.

## Options considered
1. The Game reads raw state (`Arrangements`, `Work`, `Records`) and works the board and editor logic out itself.
2. Interfaces in the Sim that the Game implements or mocks.
3. Immutable snapshot records built by the Sim on request, plus a side-effect-free `Preview` for awards.

## Decision
Option 3. Namespace `LinesOfSupport.Sim.Views` (`src/LinesOfSupport.Sim/Views/ViewModels.cs`), built from `SessionView`:

- `TopStrip()` -> `TopStripView`: day, shop hours per day, hours queued and queue days, cash and its full value, replenish day, usable funds, system-online, paying agent, GPC.
- `MorningBoard()` -> `MorningBoardView`: the records written by the last `AdvanceDay`, classified into `BoardCategory` (Delivery, Failure, Report, Request, VendorNotice, HigherHq, Event) and `BoardSeverity` (Alert first), each with a `BoardLinkKind` and link id (arrangement, card, `site.capability`, island, event, cash, work) and its `CausedBy` chain; plus one `CapabilityTile` per site capability. The player's own actions and rejections are not board items.
- `RequirementEditor(cardId)` -> `RequirementEditorView`: the card, peak demand over its window, each line with its kind and the consequence of leaving it off, each dependency with coverage `inRequirement`, `arranged` or `none`.
- `ActionEditor(siteId, capabilityId, cardId?)` -> `ActionEditorView`: instruments with the fact that blocks each one today, per-action limit, shop hours and start lag; vendors offering the capability with free capacity, lead time, quoted unit price and mobilization; known vehicles; funding lines.
- `Preview(AwardAction)` -> `ActionPreview`: the exact hard stops `Apply` would return (same code path, `Session.CheckAward`), soft gaps the sim will play out, daily cost, mobilization, total, shop-hour range and earliest-start range at best and worst roll ignoring the queue ahead.

Records rather than interfaces: the Game binds to data, and records are trivially faked in Godot-side tests by constructing them. Building any view changes nothing and draws no random number; a test plays the textbook policy with every view built twice a day and requires the same state hash as a plain run.

Text in views states facts and limits ("No operators line: daily failure odds rise while it runs"), never directions on how to do the job.

## Consequences
- Codex binds the M1-05 shell's top strip to `TopStrip()` and M2's panels to these views instead of raw state.
- Adding a field, a category or a link kind is additive. Renaming or removing one is a new ADR.
- Quotes are the scenario's offer prices until the quote band and market research land (assumptions.md, Pricing); `UnitPriceCents` keeps its name and meaning when they do.
- The instrument list shows only instruments implemented in this build; IDIQ and others appear with their milestone.

## Date
2026-11-08

## Author
Fable

## Addendum, 2026-11-08 (M2-08): debrief view
`SessionView.Debrief()` -> `DebriefView` (`Views/DebriefViews.cs`): capability bands by day (-1 before a site opens), timeline events, decisions with each island's information tier at the time, gaps (consecutive days below 100% per site capability) each tagged `Avoidable`, `Discoverable` or `Unforeseeable` (SPEC 4) from its factors, scorecard, and commitments. A gap takes its most controllable factor's tag; a `Dependency` factor inherits the tag of the gap it points to. The builder diagnoses from end-of-game state (arrangement dates and lines, survey records, population, records by day) and is read-only, so it is safe mid-game. Tag rules per factor are in docs/assumptions.md (Debrief tags). The text report gains a GAP TAGS section built from it.

## Addendum, 2026-11-08 (M2-09): prep screen
`PrepScreen.View(bundle)` -> `PrepView` (budget days, the cost of all choices together, and each choice with what it changes "with" and "without", flagging choices this build records but does not model) and `PrepScreen.Check(bundle, picks)` -> `PrepCheck` (accepted, days used, stops). Both work before a Session exists; the game shows the prep screen, then calls `Session.Create(bundle, seed, picks)`. Check's stops are the same messages Create throws. Create now also rejects a choice picked twice.

## Addendum, 2026-11-08 (Q11): per-arrangement ledger field
`Arrangement.UnobligatedIncurredCents` (read-only): incurred minus obligated, clamped at zero, and always zero for a coordination arrangement. The metric `cost.unobligatedIncurredCents` is now computed as the sum of this property, so the ledger row and the total cannot drift. The game's Ledger panel (M2-05) reads it from `SessionView.Arrangements` and does no arithmetic of its own.

## Addendum, 2026-11-08 (M2-14): commander confidence
`SessionView.LostConfidenceSites` (sites whose commander lost confidence through a refused handshake) and a new `GapFactorKind.PriorityDeferred` (appended; Avoidable). The rule itself is in docs/assumptions.md (Commander priority when capacity is short). The held-back record is a `RecordKind.Compliance` record whose subject is the work item id. Additive; no existing view changes shape.

## Addendum, 2026-11-08 (M2-19): seeded disruptions
`SessionView.PlannedPopulation(siteId, day)` (the scenario plan's population; `Population` now includes unplanned arrivals once their notice has come) and a new `GapFactorKind.PopulationJump` (appended; Unforeseeable for 7 days after the arrival, then Avoidable). The arrival notice is a `RecordKind.Event` record whose subject is the site id; a ferry forecast is a `RecordKind.Knowledge` record whose subject is the event id (`ferryWeather`), so the morning board links it as an Event. Scenario schema additions (Fable lane): effect `daysRange` and `forecastDays`, effect kind `site.populationJump`, vendor offer `sourceIsland`. Additive; no existing view changes shape.

## Addendum, 2026-11-08 (M3-02): counterfactual view
`Counterfactual.Choices(session)` lists every logged action by its index in the input log, with its day, kind, text and whether it was rejected (`Session.AppliedRejected`, parallel to `AppliedActions`). `Counterfactual.Run(bundle, log, change)` applies one `CounterfactualChange` (Replace an action, Remove it, Insert one on a day, after that day's actions or before a given one, or SetPrep) and re-runs both logs with the same seed, from day 1 (identical up to the change's day, so this is the same as re-running from that day, and costs about 20 ms for 90 days). `CounterfactualView`: FromDay, HorizonDay (the last day for a finished game; the last day played for a mid-game save), ChangeText, both scorecards, MissionOutputDelta, `GapDiff` per gap (Gone, Shrunk, Unchanged, Worse, New, by shortfall in permille-days over the gap's days, with down days and both gap indexes) and `NoLongerApplied` (actions the re-run refused that the played game did not). NoLongerApplied exists because one changed decision often moves later ones: in the SPEC 5 example, adding the hub fuel coordination removes the day 10 outage, so the day 11 handshake never happens and the logged ratification of it is refused. The re-run replays the rest of the log as logged; it does not ask a policy what the player would have done instead.

## Addendum, 2026-11-08 (M3-03): funding in the views
`FundingOption` gains optional trailing fields `PlannedAvailableDay`, `Capabilities` and `PaysForThis` (whether the line's purpose covers the action editor's capability); a later `AvailableDay` than `PlannedAvailableDay` is a late release. `BoardLinkKind.Funding` (appended): `LinkId` is a funding line id, or `funding` for the shop's money as a whole; open the ledger. `RecordKind.Funding` items show on the morning board as Report: Info for a release, Warning otherwise, and appear on the debrief timeline. `GapFactorKind.FundingLapse` (appended): nothing supplied the capability on a day with no line able to pay for it; Avoidable when money was usable before the window, Unforeseeable when none had been released (docs/assumptions.md, Debrief tags).
