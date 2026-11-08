**Work session handoff**

Expected look of this build and why: no visible change. M0-07 is the contract the desk binds to; nothing in the Godot shell uses it yet (M1-05 will).

## PRs reviewed this session
- PR 21 (Codex, M0-15 batch spreads): in lane, CI green, merged.
- PR 24 (Codex, M1-03 objection acknowledgements): responses match the decided options; backlog status-line conflict after PR 21 resolved by merging main into the branch; CI green; merged.

## Changed
- `src/LinesOfSupport.Sim/Views/ViewModels.cs`: TopStripView, MorningBoardView (BoardItem, CapabilityTile), RequirementEditorView, ActionEditorView, ActionPreview, with XML docs.
- `src/LinesOfSupport.Sim/Core/Session.Views.cs`: read-only builders. `SessionView` gains `TopStrip()`, `MorningBoard()`, `RequirementEditor(card)`, `ActionEditor(site, cap, card?)`, `Preview(award)`.
- `Session.CheckAward` extracted from `ApplyAward` so Preview and Apply share one validation path. No behavior change (all goldens identical).
- Session tracks which records the last AdvanceDay wrote (the morning board's "overnight").
- ADR-0006. Backlog: M0-07 Done; M1-05 acceptance points at the new views.

## Why
The action editor's hard-stop versus soft-gap display is the first UI experiment (SPEC 2, 9). It needs "what would this do" without doing it, and the board needs "what changed overnight" without the Game re-deriving sim logic.

## Validated how
- `ViewModelTests` (9 new, 94 Sim tests total): textbook policy on spoke-b and spoke-b-90, 5 seeds each, with every view built before and after the day's actions, ends on the same state hash as a plain run; Preview's Accepted and HardStops equal Apply's on every golden award; requirement editor shows the Spoke B card as written with fuel uncovered, flipping fuelSupply turns it to inRequirement; morning board empty on day 1, alerts first, every item dated the day just played; approach 1's day-10 fuel and power shortfalls appear as linked alerts on the morning of day 11.
- Read real output in a scratch runner: board text, instrument blocks and soft-gap wording state facts only.

## Unresolved
- Quote band and market research (assumptions.md, Pricing) are not in the Sim yet; the editor shows offer prices.
- Preview's earliest start ignores the queue ahead; it reports QueueAheadHours instead.

## Next
- Fable: M1-04 unauthorized commitments (SPEC 4.5).
- Codex: M1-01, then M1-05 using `TopStrip()` and `MorningBoard()`.
