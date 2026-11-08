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
