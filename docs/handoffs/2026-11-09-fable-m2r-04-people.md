**Work session handoff**: Fable, scheduled run, 9 Nov 2026 (02:46 to about 03:30 UTC). M2R-04, going to people.

Expected look of this build and why: no change on screen; Codex shows these actions in M2R-17. What the player would read after going to everyone on day 1 of Spoke B (seed 5):

    day 1: Capt Priya Nair, Comptroller: "I can have a paying agent carrying cash for your SF 44 buys by day 4. You have $50,000 on hand, and when it runs low I can turn it around in 3 days. There are no purchase card accounts on this deployment. Send me the paperwork and certification takes 1 day."
    day 1: You got yourself into the commander's staff meeting. Lt Col Daniel Park, Hub commander, ran it. If we run short, it's Hub, then Spoke B, then Spoke C. MSgt Lena Brandt, Spoke C camp, said they will be asking for food service for Spoke C, wanted from day 12.
    day 1: Met Ms. Rosa Delgado of Island Power Rentals. They offer power for up to 600 kW at $3 each a day, on 4 days' notice.
    day 3: Maj Erin Cole, SCO staff, reachback, answered your question, "Who normally provides bulk fuel here?" "Bulk fuel normally comes through the J4: the ACSA manager draws on a partner's stock, or DLA Energy supplies it. There is no ACSA fuel here right now, so a local fuel vendor is the stopgap, at the vendor's price, until DLA Energy is set up."
    day 8: Staff meeting. Lt Col Daniel Park, Hub commander, ran it. If we run short, it's Hub, then Spoke B, then Spoke C. Nobody raised anything new for the shop.

## PRs reviewed this session
- 102 (M2R-14 people in the inbox), 103 (OBJ-0004, docs only), 104 (M2R-16 market): ready to merge through Fable's merge queue PR 106, which carries all three with conflicts resolved. Screenshots opened and reviewed as a first-time player; follow-ups noted on the PRs (hub label "opens day 0"; "Day 1 (next)"; at 1280x800 the "How would you arrange it?" dropdown is below the quote panel; "600 kW a day" should read as capacity).
- 106 (merge queue): ready once godot-shell finishes green. Backlog nit: its M2R-17 row says "blocked on M2R-05" for the tutorial; should be M2R-04, M2R-08.

## Changed
- Actions `findFinance`, `joinStaffMeeting`, `askSco`, `meetVendor`, `engageJ4` (Session.People.cs); `WorkKind.Visit`, `RecordKind.Visit`, `BoardLinkKind.Person`; rules `goingToPeople` with schema; scenario `tutorial` flag; views `PreviewVisit`, `FinanceFoundDay`, `StaffMeeting`, `ScoQuestions`, `VendorConfidence`, `VendorMetDay`, `VendorReps`. ADR-0005 addendum, assumptions rows.
- `_revealed` now records a `RevealSource` (review, inquiry, vendor) instead of a bool, so the debrief can say the vendor raised it.

## Why
Design doc R3, 6.2 and 6.2a: finance, the commander, the SCO and the J4 are people you go to; each costs time and returns facts. M2R-17 (Codex) and M2R-06 (Spoke B fuel through the J4) wait on it.

## Validated how
`dotnet test src/LinesOfSupport.sln`: 402 Sim (14 new in GoingToPeopleTests) and 120 Harness pass; every golden unchanged (no metric keys, no policy change, the new random stream is created only on first use); `validate.py`, `provenance.py` pass. A no-ids check runs over every visit record of a full Spoke B game that goes to everyone.

## Unresolved
- Spoke B has no ACSA partner yet, so `engageJ4` is refused there until M2R-06 adds one; tested on Sustainment.
- Vendors do not come by on their own yet; any vendor can be met. Unprompted visits could be events in M2R-08.
- Prices read "$3 each a day" for capacity offers and "$0" for host-nation offers; acceptable for now, M2R-07 can polish.
- The staff meeting's priority line uses site names as written ("it's Hub, then Spoke B").

## Next
M2R-09 (map day projection; unblocks the moving part of Codex's M2R-15) or M2R-06 (Spoke B fuel through the J4), then M2R-07 and M2R-08.
