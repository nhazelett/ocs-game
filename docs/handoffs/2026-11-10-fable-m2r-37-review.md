**Work session handoff** (Fable, scheduled run, 10 Nov 2026 03:50 UTC)

Expected look of this build and why: unchanged on main's screens. The one Sim string that changes is the reason the award editor gives for the host nation utility once M2R-36 (PR 146) shows it: in Spoke B it now reads "Main Island Utilities (host nation) supports the force under an agreement made before you arrived. This office cannot buy from it or add to that support." instead of "... is host nation support. It is requested under the host nation agreement, ...", which pointed at a request Spoke B has no agreement for.

## PRs reviewed this session

All three open PRs are Codex's, in lane (game/, handoffs, backlog, ci.yml), every CI check green, mergeable. Screenshots opened from the branches as a first-time player.

- PR 142 (ART-04): ready to merge. Day 11 reads "Power, fuel and lodging down" under Spoke B; day 18 labels "Barge lane · closed" and "Fuel for Spoke B waiting: barge lane closed." at the hub shore, tied to its crate. The caption's lower edge still clips "Main Island" (M2R-38, polish).
- PR 143 (ART-05): ready to merge with 142. Day 10 playback names fuel, power and lodging short at 0%; day 17 says cargo waited at a closed route.
- PR 146 (M2R-36, stacked on 143): not ready. The two screenshots read well (reasons under the controls, Arrange support grey), but no screenshot shows a list open with greyed entries, nor the third case in the row (BPA call picked first); the handoff note is local only. Q17 (Codex asks whether the host nation vendor should be selectable) answered in the PR: no, unselectable with its reason under the control is right.

## Changed

`Session.HostNationVendorReason` (src/LinesOfSupport.Sim/Core/Session.Choices.cs): a host nation vendor's reason names a host nation request only where the scenario's agreement covers that capability and site and is not withdrawn. AwardChoicesTests updated (Spoke B lodging) and one new test (sustainment fuel covered, power not). ADR-0006 M2R-37 note. Backlog rows M2R-37 (Fable) and M2R-38 (Codex polish: "(no card)" in the arranging list, the cargo caption over the hub name).

## Why

Nick's 9 Nov playthrough asked that a refused choice explain itself. The explanation for the host nation utility sent the player to a host nation request that neither Spoke B nor the game offers.

## Validated how

`dotnet test src/LinesOfSupport.sln`: 499 Sim and 121 Harness tests pass. validate.py and provenance.py pass. Goldens unchanged (views only). No game code or text in PR 146 asserts the old sentence.

## Unresolved

FOR-NICK #8 (gate before or after the ART merge) still open. M2R-23 stays ready on today's main.

## Next

Re-review PR 146 when the screenshots land. No other Fable M2R row is open; Sim depth stays paused.
