**Work session handoff** (Fable, scheduled run, 10 Nov 2026, DESK-03, stacked on DESK-02)

Expected look of this build and why: no screen change yet. The Sim can now complete a request by asking someone about it. DESK-23 (Codex) replaces the scope checkboxes with what is known, who to ask and "add what you learned".

## PRs reviewed this session

See docs/handoffs/2026-11-10-fable-desk-02-team.md (same run).

## Changed

- Scenario cards gain `knownBy`: for a line the request left off, the person who knows and what they say.
- `askAbout` (card, person): a 1-hour visit, given to a teammate like any other work. When it is done the person speaks, and any line they know about is revealed. Someone who knows nothing says "Nothing I can add on the request for ...".
- `addLine` (card, line): adds a revealed line to the request; refused before anyone has told you.
- `RequirementLine` gains Known, CanAdd, Learned and LearnedFrom; `RequirementEditorView.WhoToAsk` lists whoever brought the request, then anyone who knows a part it left out, with Asked and Pending.
- Data: tutorial forklift operators (SMSgt Dunn, LRS, knows he has no licensed drivers); Spoke B power operators and maintenance (Capt Ruiz, CE); Spoke C water operators and maintenance (Capt Ruiz: "Nobody is running the plant at Spoke C"); Spoke C food staff (MSgt Brandt). Spoke B-90 power as Spoke B. The tutorial's bottled water has no hidden plant lines (bottled water needs no operators).
- Spoke B fuel has no `knownBy`: nobody tells you, the dependency checklist and the J4 remain the way to it, so the lesson is intact.

## Why

D3: no checkboxes the player ticks without knowing why; going to the right people is the mechanic.

## Validated how

526 Sim tests (5 new in DiscoveryTests) and 121 Harness tests pass; every golden unchanged (setLine is untouched and no log asks anyone yet); validate.py and provenance.py pass.

## Unresolved

- The ask hour is a rules default (1) in code, not yet a line in rules-2025-10.json.
- Sustainment's cards have no `knownBy`; it is not in the game.

## Next

DESK-04 (end-of-day check), which can now name a teammate with free hours.
