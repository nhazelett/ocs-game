# Claude plays the tutorial, 18 Nov 2026

The day's play (PLAY-03, docs/PLAYTEST-CLAUDE.md). A fresh agent got only the brief in section 3 and the driver, plus a notes file of its own. DESK-67 and DESK-68 are merged, so it was not told to avoid Menu. It never read the repository.

- Build: `main` at 0a42ce1 (after DESK-93 to DESK-101: the request opens as its own page), Godot 4.6.1 .NET, 1280 by 800, Xvfb, Dummy audio.
- Seed: 20261118.
- How far: the end of day 16 and the debrief, in 235 commands. It was never stuck for ten steps; the longest stall was five, on day 4 (note 14).
- Logs: [commands](plays/2026-11-18/commands.txt) (the number is the screenshot it produced) and [input log](plays/2026-11-18/input-log.json). The harness replays the log to the same game (`run tutorial --log`, state hash ad72dce1).
- Pictures: four of the 236 the play took, all from the same game.

## The result, from the replay

Every pass line met, mission output 99%, on 64 shop hours. Prep was all five choices (9 of 14 days). Water went on the card to Island Mart on day 1 and ran from day 2. It joined the staff meeting on day 3, after Mills' day 2 note. On day 4 it asked SMSgt Dunn and in the same hour called the Harbor Equipment BPA for forklifts and maintenance without drivers; Dunn's answer that night said drivers were needed. On day 5 it added the operators and wrote a fast-and-good purchase order with Coastal Rentals, which started on day 8; Harbor declined the first call that night, as the tutorial has it decline every call on days 4 to 8. The staff meeting gave it the waste request on day 6; it asked Capt Ruiz on day 7, ordered Main Island Sanitation on day 8, and that started on day 13. Coastal's forklifts failed on day 11 (50%, with drivers and maintenance, so unforeseeable) and came back on day 12. It answered the dining hall's slips with a cure notice on day 13. Days 14 to 16 were End the day.

## The first two minutes

The preparation page read well: each choice says what it changes ("works from day 3, not day 25"). The fifth choice was below the fold, and with all five fitting in 9 of 14 days there was nothing to choose, as on 17 Nov. The turnover was clear on the goal but the player had to fish the pass lines out of the prose on its last page; it found them again as the commander's bar on Today and called that the checklist it wanted. Page 2's "the host nation has power, fuel, water, waste removal and material handling equipment at Hub, for the whole deployment" left it asking why it would ever buy water or forklifts.

On Today on day 1 the water request read once, with its last day to order; the player opened it, saw the new two-column page, and bought on the card. "The quote said day 2 or 3; the confirmation says day 2": the window held, which is D35 working.

## Its notes, sorted

The sort follows section 5: defect, design, known, model. A row named here is new in this PR unless it says otherwise.

| # | Day, screen | What the player said (shortened) | Sort | Row |
| --- | --- | --- | --- | --- |
| 1 | Prep | Two choices pre-ticked, no word whether that means recommended | Known: D32 (pre-ticked only when it changes something); no row | none |
| 2 | Prep | All five fit in 9 of 14 days; "pick what will help most" is not a choice | Known: D32's Sim note accepts it for the first scenario | none |
| 3 | Turnover | Pass lines buried in prose on the last page | Known: D28; the bar on Today is where the player found them | none |
| 4 | Turnover | Host nation has water and forklifts "for the whole deployment", so why buy them | Model, mostly: the next pages say the water point is down and the forklift will go back. Noted for Nick's play | none |
| 5 | Turnover | O&M "good through day 90" against "the 16 days you're here" | Model: two true horizons | none |
| 6 | Day 1, Today | Request entries and teammate names do not look like buttons | Design: a board entry that opens something has no mark that it does | DESK-108 |
| 7 | Day 1, request | The ask of MSgt Ortiz answered at day's end, after the order, and added nothing | Known: D25 (an ask that finds nothing says what it confirmed); no row | none |
| 8 | Day 1, request | After Arrange support the form went blank and its button grey | Known: DESK-63's rule (close onto the request), now on the D36 page where the decision shows in the left column; no row | none |
| 9 | Day 2 and 3, Today | Mills says get into the staff meeting, not where; it is under People to reach at the foot of Today, about six scrolls down | Design: the news that names an action does not carry it (D33's rule for warnings, not yet for news) | DESK-108 |
| 10 | Day 3, Today | Mills' note twice: as the lead above the morning heading, then under New since yesterday | Defect, game | DESK-108 |
| 11 | Day 3, People to reach | "Meet the vendor to learn their terms" offered after the vendor list gave those terms | Defect, game text: the meeting's line says "to learn their terms" whatever is still unknown; the vendor card already has what is left to learn | DESK-108 |
| 12 | Day 4, request | "The host nation wants its forklift back", then three forklifts | Defect, scenario text | DESK-102 |
| 13 | Day 4, request | "Fuel: arranged · host nation fuel" then "The usual owner has not been engaged" | Defect, game: the owner line shows on a dependency that is covered | DESK-108 |
| 14 | Day 4, request | Clicking a teammate's name opened the team page over the request page; Escape did not close it; five steps to get back ([picture](plays/2026-11-18/day4-team-over-request.png)) | Defect, game (Escape) and driver (its text still listed the request page under the team page) | DESK-108, DESK-109 |
| 15 | Day 4, editor | "Choose an available vehicle" means a contract vehicle; the game also sells an office vehicle | Defect, label; DESK-63 renamed the field "Contract vehicle" and missed the refusal | DESK-108 |
| 16 | Day 4 and 5, team page | 8 hours, then 7, then 6.5 for the same task | Model: the quote is your pace, Avery's is faster, and half an hour was done before the hand-over | none |
| 17 | Day 4 night | Dunn's ask was the most important hour in the game; Ortiz's on day 1 was worthless; nothing says which ask is which | Known: D25; the request's warning names who to ask (DESK-58). The difference is the lesson | none |
| 18 | Day 5, Today | "0 alerts" while the BPA call Avery was writing lacked the drivers Dunn had just named; Where things stand said only "MSgt Joan Avery is writing the BPA call with Harbor Equipment Co" | Design: the request's standing does not say that a buy in work leaves out a part someone said is needed | DESK-103 |
| 19 | Day 5, request | No way to stop the Harbor call still being written; the new quote said it doubled up on it ([picture](plays/2026-11-18/day5-double-buy-no-stop.png)) | Design, the most important, and the player's one change. The game warns in amber that the new buy is on top of the call in work and offers nothing to do about it. Stopping work the shop has not signed is a new action, so it goes to Nick | FOR-NICK #23; DESK-104, DESK-105 |
| 20 | Day 5, editor | Pick two has no explanation; both pairs showed the same start | Model, mostly: on day 7 the player read the trade from the quote (fewer shop hours, a firmer start). The toggles say nothing themselves; D7 put the trade in the quote | none |
| 21 | Day 5, editor | The checked "Good" toggle is white on pale, nearly invisible ([picture](plays/2026-11-18/day5-double-buy-no-stop.png)) | Defect, game: a toggle's hover or focus ink | DESK-108 |
| 22 | Day 5, editor | TSgt Pratt "watches no other contract" after being named on the call in work | Model: true as written; a call not yet signed is not a contract he watches | none |
| 23 | Day 6 | The staff meeting gave a day's warning on waste removal | Praise, earned | none |
| 24 | Day 7, request | The waste quote says it is "on top of Main Island Utilities. Every day it would run is already covered", on the page whose request says the host nation pulls the trucks after day 12 ([picture](plays/2026-11-18/day7-waste-already-covered.png)) | Defect in the Sim view, the second most important. The commander tasked the shop on day 6 and the formal withdrawal is recorded on day 8, so on day 7 the summary counts host nation support the request itself says is ending, and tells the player in amber not to buy what the commander asked for | DESK-102 |
| 25 | Day 7 | Waited for Ruiz's answer before ordering, because orders cannot be stopped | Follows from 19 | FOR-NICK #23 |
| 26 | Day 8 | A COR report on the inherited camp services call, not on the bar, no action | Known: D31 seeds it; it becomes a decision on day 12 or 13 (DESK-83) | none |
| 27 | Whole play | The map never changed and "Nothing moves today" was always grey | Model: one island and no lanes in the tutorial; motion is not judged by this play | none |
| 28 | Day 12, Today | The map card says "Hub · down" while the bar on the same screen says "out 0 days so far. Met so far" ([picture](plays/2026-11-18/day12-alert-below-bar.png)) | Defect, game: the map's site word reads "down" for a site at 50%, where the Sim says degraded | DESK-108 |
| 29 | Day 12, Today | "1 alert" in the heading; the alert sits below the bar as the lead, not marked, and its button only highlights the map | Defect, game: the morning's one shortfall is under the bar, after news about the dining hall | DESK-108 |
| 30 | Day 12 | "Nobody has told you why yet", and no way to ask; it came back by itself | Model and design, no row: it chose the COR who reports two days late, and the COR did report it on day 13. The failure was unforeseeable. Two of its three crises fixed themselves (this and the Harbor decline), which it read as luck | none |
| 31 | Day 13, Today | The vendor decision was plain text with no buttons; the buttons were at the foot of Inbox | Known, and wrong: Today carried "Open vendor decision" under it (DESK-83), which the player did not use | none |
| 32 | Day 13, Inbox | The issue says "or terminate" and Inbox offers no terminate | Defect, label: termination is on the contract's page (DESK-21); the issue text should say where | DESK-108 |
| 33 | Days 14 to 16 | Nothing happened | Known: D31 | none |
| 34 | Debrief | "Trace the evidence" gave one sentence: "An earlier cause was not reported to the shop" | Defect, game: the trace starts from the failure record, which the shop only heard of through the COR, and stops there; the harness prints the chain (the failure, then the day 5 purchase order) | DESK-108 |
| 35 | Debrief | It grades outcomes only; nothing on catching the drivers, the call without them, the COR, the cure notice or the cost | Design, the third most important. On 17 Nov the careful player said the same ("not knowing whether any choice I made mattered"). D28 gave the verdict; nothing says what earned it | D38; DESK-106, DESK-107 |

## Its four answers, in its words

- **Who were you?** "A contracting officer who knows government contracting but had never seen this game. I was in charge of a 3-person shop at an island hub of 300 people for 16 days, inheriting host-nation support that kept being withdrawn."
- **What were you trying to do?** "Keep water, forklifts and latrine pumping from ever being out more than a day, and keep sorties at 95% or above."
- **Did you manage it?** "Yes: all four standards met, output 99%. But luck did part of the work. The flawed forklift order I couldn't cancel was declined by the vendor anyway, and the day-11 forklift dip fixed itself. Neither the game nor its debrief ever told me whether my actual decisions were good."
- **One change?** "When something you learn makes an order already in progress wrong (here, 'the rental has to come with drivers'), raise it as a real alert on the Today tab and give a way to amend or cancel that order. Today it's '0 alerts', the warning is buried in the request page, and there is no cancel. That is the core skill this tutorial is trying to teach, and it's the place the interface fails hardest."

## Fable's reading

The request page (D36) did its job: the player bought water, forklifts and waste removal from it without getting lost, and never mentioned the scroll boxes or hidden buttons of the earlier plays. The order-by hint (D35) held. What remains is about what the game says after the player learns something.

1. The player learned on day 4 that the forklifts need drivers, while its own call without them was in work. The game knew too: the next quote said in amber that the new buy sat on top of the call. Today said "0 alerts", the request's standing said only who was writing the call, and there was no way to stop it. In the tutorial nothing is lost, because Harbor declines every call on days 4 to 8, but the player cannot know that, and the lesson it took away was "never order before the answer is in". DESK-103 makes the standing say it. Stopping unsigned work is a new action, so it is FOR-NICK #23, recommended yes.
2. The waste summary told the player not to buy what the commander had just tasked (note 24). It is the same summary that saved it on day 5, so a player who trusts it is punished. DESK-102.
3. The debrief still does not say what the player did right. Two players in two days have said so. D38 has the debrief say, for each line of the bar, what carried it and the decision behind it, in a sentence each, from records the Sim already keeps.

## Sim pass

`batch tutorial --seeds 200`, medians (min to max). Unchanged from 17 Nov in every column: nothing since then moved a draw.

| Policy | Mission output | Forklifts down days | Waste down days | Water down days | Food down days | Confusion days |
| --- | --- | --- | --- | --- | --- | --- |
| do-nothing | 0.796 (0.778 to 0.796) | 9 | 4 | 6 (6 to 7) | 6 (6 to 8) | 0 |
| greedy | 0.975 (0.943 to 1) | 1 (0 to 2) | 0 (0 to 1) | 0 (0 to 1) | 0 (0 to 3) | 1 (0 to 6) |
| textbook | 0.997 (0.973 to 1) | 0 | 0 | 0 (0 to 1) | 0 (0 to 2) | 1 (0 to 4) |

- Textbook meets every pass line in every seed and beats greedy on mission output.
- No policy reads the buy summary, so note 24 moves no number; a player who obeys the amber warning on day 7 loses the latrine line, which is the tutorial's one zero-tolerance line.
- The player's 64 shop hours against textbook's 103 to 118: textbook surveys and meets vendors the vendor list already covered. Not a finding for the player.
