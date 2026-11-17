# Claude plays the tutorial, 17 Nov 2026

The day's play (PLAY-03, docs/PLAYTEST-CLAUDE.md). A fresh agent got only the brief in section 3 and the driver, plus the two run notes section 4 asks for (a notes file of its own; do not click Menu, since DESK-68 is not merged). It never read the repository.

- Build: `main` at 23f51f9 (PR 297, after DESK-71 and DESK-80), Godot 4.6.1 .NET, 1280 by 800, Xvfb, Dummy audio.
- Seed: 20261117.
- How far: the end of day 16 and the debrief, in about 280 commands. It was never stuck for ten steps.
- Logs: [commands](plays/2026-11-17/commands.txt) (the number is the screenshot it produced) and [input log](plays/2026-11-17/input-log.json). The harness replays the log to the same game (`run tutorial --log`, state hash c412d969).
- Pictures: four of the 276 the play took, all from the same game.

## The result, from the replay

Every pass line was met with no gaps, mission output 100%, on 44 shop hours. Prep was all five choices (9 of 14 days). Water went on the card to Island Mart on day 1 and ran from day 3; the site's two days of water stock covered day 2. The player asked SMSgt Dunn on day 4, added the operators he named on day 5, and wrote a fast-and-good purchase order with Coastal Rentals (TSgt Pratt as COR) that started on day 8. It never called the Harbor Equipment BPA, so it never met the decline. Waste removal went on a fast-and-good purchase order with Main Island Sanitation on day 9 and started on day 13. It joined the staff meeting on day 10, after the waste request told it the meeting existed, and answered the dining hall's slips (days 11 and 12) with a cure notice on day 13. Days 14 to 16 were End the day three times.

## The first two minutes

The preparation page read well: each choice says what it changes. All five fit in 9 of the 14 days, so the player ticked them all and noted there was no choice to make. The turnover was clear on the goal. Two things went wrong in it: Mills says "I'm here three more days" and two lines later "the outgoing contracting officer is here for your first three days", and his vendor page says three vendors' "prices are estimates" right after the player brought the vendor list that filled those prices in.

On Today on day 1 the water request was on screen three times (new since yesterday, "MSgt Carla Ortiz is here", Where things stand) and once more in the Inbox. Where things stand said "Order today to start on day 2: a card buy from Island Mart starts 1 day after it goes in" ([picture](plays/2026-11-17/order-today-day-2.png)). The player did exactly that, and the buy started on day 3.

## Its notes, sorted

The sort follows section 5: defect, design, known, model. A row named here is new in this PR unless it says otherwise.

| # | Day, screen | What the player said (shortened) | Sort | Row |
| --- | --- | --- | --- | --- |
| 1 | Prep | All five choices fit (9 of 14); "pick what will help most" is not a choice | Design, no row. In the tutorial every choice now does something (D32) and they all fit; Spoke B's do not. The tutorial is meant to be easy to start | none |
| 2 | Turnover | Mills switches to "the outgoing contracting officer" two lines after "I'm here" | Defect, scenario text. DESK-60 fixed his greeting and missed the `due` line | DESK-87 |
| 3 | Turnover | Vendor prices called "estimates" after the vendor list filled them in | Defect, Sim text: `Session.Turnover` says "estimates" by the scenario's info tier and ignores the prep choice that made the terms known (DESK-76) | DESK-87 |
| 4 | Turnover | "Let's meet them" met nobody; it opened the desk | Defect, label | DESK-89 |
| 5 | Day 1, Today | The same request three times on Today and again in Inbox | Design: the morning brief (D29) lists a new request as new, as the lead and in Where things stand. New since yesterday and Where things stand are enough | DESK-89 |
| 6 | Day 1, Today | "1 open requests"; day 4 "1 alerts"; Team "1 hours of unfinished work" | Defect, plurals | DESK-89 |
| 7 | Day 1, Today | "Order today to start on day 2" was followed exactly; the buy started day 3; the form had said "day 2 or 3" | Defect in the Sim view, the most important one. `OrderBy` dates the order from the best roll and states it as fact. Nothing was lost only because the stock covered day 2, which no screen said until day 2 ([picture](plays/2026-11-17/order-today-day-2.png)) | DESK-85 (D35) |
| 8 | Day 1, Vendors | Island Mart's notice is 0 days, yet the card buy starts 2 days out | Model. The card buy goes in on day 1 and the vendor's first day is the next one, with a day of slack. Covered by DESK-85's window | DESK-85 |
| 9 | Day 1, editor | $0.30 × 4,500 × 7 = $9,450, but the quote was $9,734 | Design: Island Mart's 3% card surcharge is in the price and named nowhere | DESK-86 |
| 10 | Day 1, editor | Prices only after choosing each supplier; no side-by-side | Known: D15 and D34 (the supplier list gives each one's reason); no row | none |
| 11 | Day 1, editor | "Harbor Bottling does not accept the card" shown with Island Mart chosen | Defect, a stray line from the other supplier | DESK-90 |
| 12 | Day 1, own initiative | Could not set a quantity or one day of water; $18,000 against a $15,000 box | Design: the quantity, start and days fields are about five scrolls below the choice, under pick two and the COR, so the player never found them on day 1 | DESK-90 |
| 13 | Day 1, Payments | O&M still $576,000 after the $9,734 card buy | Design: the card is its own program and does not draw the O&M line; Payments does not say so | DESK-89 |
| 14 | Day 1, results | Asking Ortiz confirmed what was known | Known: D25 (an ask that finds nothing says what it confirmed) | none |
| 15 | Day 2, Today | "The water stock on hand should cover day 2": first word of any stock, a day late | Design: the stock that covers the days before a start is said only once a buy is placed. DESK-85 puts it in the order-by line | DESK-85 |
| 16 | Day 2, Today | "People to reach" was a picker with no label ([picture](plays/2026-11-17/people-to-reach-blank.png)); on day 10 it showed Finance | Defect: the picker shows nothing until one is chosen, so the player dismissed it | DESK-89 |
| 17 | Day 4, request | The hint names Coastal Rentals on the card, not the Harbor Equipment BPA | Model, mostly. The BPA call is two days' lead and the card one; the hint names the fastest route. Under DESK-85's rule (a route with written terms when one still makes the day) it names the purchase order with Coastal Rentals, since a 13-day card buy carries the no-written-terms warning | DESK-85 |
| 18 | Day 4, header | "1 alerts" and no alert to be found | Defect, plural (6); what counts as an alert is DESK-69's heading | DESK-89 |
| 19 | Day 4 to 5, request | Drivers were hidden until Dunn was asked | Known: the lesson (D3, D25) | none |
| 20 | Day 5, request | After Add what you learned the pane lost its scroll bar and its buttons and its text ran past the window's foot; again after Ask on day 9; a tab switch fixed it | Defect, game layout ([picture](plays/2026-11-17/request-pane-spills.png)) | DESK-88 |
| 21 | Day 5, editor | Harbor Equipment "Available today: 5" the morning the news said it was short of forklifts | Design: "Available today" reads as a fact about today when it is the vendor's usual capacity; the decline is the lesson, so the label should say what it is | DESK-89 |
| 22 | Day 5, editor | Choosing BPA call greys Coastal; the pickers constrain each other both ways | Known: D15 (each greyed line says why); no row | none |
| 23 | Day 5, editor | "Pick two of fast, cheap and good" in red before the toggles were in view | Defect, the error refers to controls below the fold | DESK-90 |
| 24 | Day 5, editor | No screen said whether the order covered the operators; only the debrief log did ("Forklifts, Operators, Maintenance") | Design: the buy summary (D33) names a part left out (D34) but not the parts a full buy covers | DESK-86 |
| 25 | Day 5, editor | The day's wait for finance to certify the funds showed only on the day's plan | Design: the summary says when it starts, not why | DESK-86 |
| 26 | Day 5, driver text | Start day and Duration both read "Name" at some scroll positions | Defect: the fields' accessible names; check in the game, not only the driver | DESK-90 |
| 27 | Day 5 and 9, editor | "Day 8 or 9", "day 13 or 14" against pass lines that allow no missed day; an earlier start day only widened the window | Design, no row of its own. The window is honest and says its cause (writing hours, the vendor's day); the only lever is ordering earlier, which DESK-85's order-by date gives | DESK-85 |
| 28 | Day 5, Team | Offered "7 hours", then "6.5 hours left" | Model: half an hour of the work was done before the hand-over | none |
| 29 | Day 9, request | The waste request reached it on day 9 because "the shop was not in the room" on day 6; nobody had said there was a room | Design. Mills never mentions the staff meeting, so missing it is not a choice the player made. A note from him on day 1 or 2 makes it one | DESK-87 |
| 30 | Day 9, request | "Order by day 11: a cash buy from Main Island Sanitation", against Mills' "a written contract and a COR, not a run of cash buys" ([picture](plays/2026-11-17/waste-cash-buy-hint.png)) | Defect in the Sim view, the second most important. The order-by line names the fastest route even when its own quote warns against it; the game contradicts its mentor | DESK-85 (D35) |
| 31 | Day 10, staff meeting | "If we run short, Hub comes first" means nothing with one site | Model; the line is Col Ward's standing priority and true in every scenario | none |
| 32 | Day 11 to 12, Inbox | COR reported a missed delivery; Vendor performance still said "No performance issues reported" on day 12 | Model: an issue opens after the second report, on day 12's night; the decision was there on day 13 | none |
| 33 | Day 12, Payments | The dining contract offers only "End this contract…"; Inbox offers cure notice and re-source | Design: the contract's own page should offer the same decisions while an issue is open | DESK-89 |
| 34 | Day 13, Today | The issue was text on Today; the buttons were at the foot of Inbox | Known | DESK-83 |
| 35 | Day 13, Inbox | "Decision: no decision" heading; "Cure notice the performance issue…" | Defect, labels | DESK-89 |
| 36 | Debrief | The grid shows food service at 100% on days 11 and 12, the days the COR said it missed | Design, and the third most important. The dining hall holds three days of food stock, so the slips never reach the people; the record says "missed today's delivery" and nothing says the stock covered it, so the COR report and the grid read as a contradiction and the cure notice looks wasted | DESK-87 |
| 37 | Debrief | The cure notice ends on day 23, after the tutorial; nothing said what came of it | Design: the debrief says the cure period is running and what decides it | DESK-87 |
| 38 | Debrief | Obligated $644,925 against $576,000 of O&M | Design: the total includes the inherited dining and lodging calls (and the card); the debrief does not say so | DESK-87 |
| 39 | Debrief | A scorecard, not a lesson | Known: D28 (the verdict), DESK-58 (who knew); still the weakest page, kept for the DESK gate | none |
| 40 | Days 14 to 16 | Three End the days with nothing to do | Known: D31; 16 days is the length the forklift lesson needs (DESK-80) | none |
| 41 | Whole play | The cash box it was warned about twice was never touched | Design, no row. Cash bites only on the Harbor Bottling path; a player who takes the card never meets it, which is the point of Mills' note | none |

## Its four answers, in its words

- **Who were you?** "A first-time player who knows government contracting (micro-purchase card vs SF 44 vs written PO vs BPA call, CORs, cure notices, fund cert) but had never seen this game."
- **What were you trying to do?** "Keep the commander's four standards for 16 days: water never out more than a day, forklifts never out more than a day, latrines pumped every day with zero misses, sorties at least 95%."
- **Did you manage it?** "Yes, all four Met with zero days out and 0 gaps." Then: "a good share of that was luck: the day-2 water gap was saved by a stock nobody told me about, both PO start dates were shown as 'day X or X+1' against zero-tolerance standards, and I never learned whether the PO actually covered operators until the post-game log."
- **One change?** "Make the buy form tell the truth in one place, before I commit: a single summary block at the top of Arrange support that shows which parts of the request this choice covers, the firm start day and why (fund certification day, writing hours, vendor notice, existing stock on hand), the price breakdown, and the editable quantity/start/duration fields." Runners-up: tell the player about the staff meeting on day 1; make the debrief explain what each decision prevented.

## Fable's reading

This is the first play that won cleanly, and the player still called a fair share of it luck. The game did not mislead it about the scenario, as it did on 16 Nov. It misled it about the game's own hints, which is worse in a tutorial, because the hint is where a new player looks.

1. The last day to order is a best case stated as fact (note 7), and it names a route the mentor told the player not to use (note 30). Both are in one Sim view, so one row fixes both (DESK-85, D35).
2. Three things are true on one screen and contradicted on another: the food the COR says was missed and the grid says was served (note 36), the vendor prices called estimates after the player bought them (note 3), and the staff meeting the player is blamed for missing and was never told of (note 29). DESK-87.
3. The buy form has the facts and puts them in the wrong order: what the buy covers, why it starts when it does and the fields that size it are below the choices that depend on them (notes 12, 24, 25). DESK-86 gives the summary the words; DESK-90 moves the fields.

The request pane spilling past the window after an action (note 20) is the one layout defect, and it is on the path every player takes on day 5.

## Sim pass

`batch tutorial --seeds 200`, medians (min to max):

| Policy | Mission output | Forklifts down days | Waste down days | Water down days | Food down days | Confusion days |
| --- | --- | --- | --- | --- | --- | --- |
| do-nothing | 0.796 (0.778 to 0.796) | 9 | 4 | 6 | 6 (6 to 8) | 0 |
| greedy | 0.975 (0.943 to 1) | 1 (0 to 2) | 0 (0 to 1) | 0 (0 to 1) | 0 (0 to 3) | 1 (0 to 6) |
| textbook | 0.997 (0.973 to 1) | 0 | 0 | 0 (0 to 1) | 0 (0 to 2) | 1 (0 to 4) |

- Textbook meets every pass line in every seed and beats greedy on mission output (DESK-61 and DESK-80 hold).
- Greedy's forklift buy without drivers is out at least one day in most seeds, so it fails the forklift line; the lesson is priced.
- Food is out 0 days in the median under every policy that buys: the dining hall's two slips land on three days of stock. That is note 36: the decision is met in every seed, and its effect is invisible. DESK-87 says so in words; it does not move a number.
- Every policy obligates more than the O&M line holds (do-nothing $624,000) because the inherited dining and lodging calls count. Note 38.
