**Work session handoff**: Fable, chat session with Nick, 9 Nov 2026 evening. M2R-35, plus merges.

Expected look of this build and why: no change on screen yet. M2R-36 (Codex) uses the new view to grey dead choices. What the player will read, Spoke B day 1, lodging, with Main Island Utilities picked: every instrument greyed with "Main Island Utilities (host nation) is host nation support. It is requested under the host nation agreement, not bought with cash, the card or a contract." Fuel, the partner force: "The partner force supplies under the ACSA. That is the J4's to arrange, not a purchase by this office." Power with Meridian Site Services picked, once a paying agent exists: cash buy greyed, "Meridian Site Services does not take cash."; BPA call greyed, "This shop holds no BPA with Meridian Site Services." A cash buy picked: every funding line greyed, "A cash buy is paid from the cash box, not a funding line."

## Changed
- `Core/Session.Choices.cs`, `AwardChoicesView` and `ChoiceOption` in ViewModels.cs, `SessionView.AwardChoices`. ADR-0006 addendum.
- Backlog: M2R-35 Done, M2R-36 (Codex, next), ART-07 (Codex, low priority). art-direction.md records Nick's note on the three looks.
- Merged at this check-in: PR 141 (ART-01 to ART-03). This branch carries PR 144 (the ART review, FOR-NICK #8, M2R-34).

## Why
Nick's playthrough: the dropdowns let him pick host nation and then an SF 44, and only then refused. He wants the dead choice unselectable with the reason shown.

## Validated how
498 Sim and 121 Harness tests pass (5 new test methods, 8 cases): the host nation and partner sentences; cash greyed by the vendor once a paying agent exists; picking the instrument first greys vendors; every greyed pair refused by Preview across four scenarios on days 1, 6 and 12 with no ids in any reason; no state change. Validators and marker check clean.

## Unresolved
- FOR-NICK #8 (gate before or after ART) is still unanswered; Nick played part of a build on 9 Nov and has not said which.
- Other editors (BPA and IDIQ setup, external support, host nation request, ACSA order) still learn their refusals at preview. Same treatment later if Nick hits it there.
