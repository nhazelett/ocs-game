**Work session handoff** (Fable, scheduled run, 11 Nov 2026, DESK-05, stacked on DESK-04)

Expected look of this build and why: no screen change from the Sim itself; the game still has its Directory tab until DESK-24 binds the new cards. Two things a player can notice: the Visits panel now offers a meeting with every vendor (13 new reps across the tutorial and both Spoke B cuts, shown with the fallback face), and a meeting's record ends "They take cash, not the card. They deliver on Main Island only. Their vendor card is filled in." instead of the old directory tier sentence.

## PRs reviewed this session

- PR 158 (DESK-20): re-checked after 2a12d30. Ready to merge. Menu screenshot: Skip quiet days aligned, "Continue saved game" named.
- PR 159 (DESK-21): re-checked after 004f802 and 49f6e48. Ready to merge after 158. All three earlier follow-ups landed (pays-nothing sentence, back bar clear of Menu, capitals).
- PR 160 (DESK-25): re-checked after the shutdown cleanup commits. Ready to merge after 158 and 159. No player-visible change in the new commits.
- PR 161 (ART-09): re-checked after c78b461 and 7b67f8d. Ready to merge last. The caption now reads "Fuel 0% supplied".
- PRs 162 to 164 (Fable, DESK-02 to 04): unchanged since opened, CI green; this PR stacks on 164.

## Changed

- `SessionView.VendorCards()`: one `VendorCard` per vendor the office could buy from (host nation and ACSA suppliers get none). Always known: name, island, what they sell. Null until learned: cash, card, prices and lead times (known once met, after doing business, or vendor `infoTier` "observed"); capacity and islands reached (the same, or a survey of their island returned during play, or `infoTier` "surveyed"). Also what the office holds with them (arrangement and vehicle ids), the rep, the met day, whether a meeting is set up, standing, and two sentences: `KnownFrom` (where the facts came from) and `StillToLearn` ("Not known yet: how they take payment, their prices, how much they can supply and where they deliver. Meet Mr. Marco Silva to find out.").
- `VendorOption` keeps its types (the game build does not break) and gains `PaymentKnown` and `CapacityKnown`.
- Meeting a vendor: the record says how they take payment and where they deliver, and "Their vendor card is filled in." The old "Your directory entry for them is now estimated" sentence is gone (the Directory leaves the screen).
- Scenarios: reps for every buyable vendor in the tutorial (5 new), Spoke B (5) and Spoke B 90 (3). Tomas Reyna of Harbor Equipment Co now appears in Spoke B as well, with his portrait; the others have no portrait yet.
- docs/assumptions.md (vendor knowledge section), design doc Sim note, BACKLOG DESK-05 and DESK-24 rows, status line.

## Why

D6: vendors, not a Directory; what you have not learned is blank, not hidden; meeting vendors fills the cards in; more vendors to meet. Knowledge does not gate a buy (the Sim still awards on true facts), so no replay or golden report moved.

## Validated how

`dotnet test src/LinesOfSupport.sln`: 538 Sim tests (8 new in VendorCardTests; GoingToPeopleTests' meeting test updated for the new record text and Second Island Fuel's new rep) and 121 Harness tests pass. Goldens unchanged. validate.py and provenance.py pass. `dotnet build game/LinesOfSupport.Game.csproj` succeeds against the changed Sim.

## Unresolved

- On day 1 of Spoke B nearly every card is blank (only Harbor Equipment Co, held under a BPA, is filled in). That is D6 as written, and meetings are 2 hours each; if the gate shows it slows the power buy too much, mark Island Power Rentals `observed` in Spoke B.
- The 13 new reps need faces (Codex, assets; fallback until then).
- `VendorConfidence` (the old tier) still exists for the Visits panel's "Directory confidence" line; DESK-24 drops that line, after which the tier can go.

## Next

DESK-06 (fast, cheap, good: pick two), stacked on this PR.
