# Claude play, 22 Nov 2026

The verification play due once the twelfth play's rows landed: Fable's DESK-201 (D53, say when a night's wait costs nothing, PR 489) and DESK-202 (a vendor that brings a part at the same price says so, PR 490), and Codex's DESK-203 (the last day's summary over the desk, PR 491) (PLAY-03, docs/PLAYTEST-CLAUDE.md section 2). Ending a contract (DESK-196) has still not been through a play, so two fresh agents played side by side: one with the section 3 brief, one with the section 3b second brief (change your mind twice). Each had the driver and a notes file of its own; neither read the repository.

- Build: `main` at 34ec792 (PR 491 merged). Godot 4.6.1 .NET, 1280 by 800, Xvfb, Dummy audio.
- Seed: 20261122, a new seed for a new day. Unlike 20261121, it brings confusion days on buys with no written terms, so the debrief had them to grade for the first time.
- How far: both to the debrief after day 16, never stuck, all four pass lines met with 0 days out everywhere; mission output 97% both. 187 and 243 commands (the first command of each log is the runner's `look` before the player started).
- Logs: [commands, first brief](plays/2026-11-22/commands-1.txt), [input log](plays/2026-11-22/input-log-1.json); [commands, second brief](plays/2026-11-22/commands-2.txt), [input log](plays/2026-11-22/input-log-2.json). The harness replays both logs to every line met (`run tutorial --log`); four gaps each, all tagged avoidable: water and food on day 3, forklifts on days 12 and 15, every one a confusion day on a card buy.
- Pictures: two of 430: [day 16, the debrief grading a card buy's confusion day "Avoidable" with nothing on what would have avoided it](plays/2026-11-22/day16-debrief-avoidable.png), [day 4, the water contract page counting a half-delivered day at its full price, with a Debrief tab in the middle of the game](plays/2026-11-22/day4-half-day-full-cost.png).

## What the merged rows did in play

- DESK-201 (D53): verified on day 7. The first player's waste quote read "Capt Ana Ruiz's answer about this request comes in tonight ... Ordered tomorrow, after the answer, it still starts on day 13.", and the player: "Exactly what I needed". On day 4 the forklift quote's third sentence was the leaves-out sentence ("SrA Eli Novak is asking SMSgt Ray Dunn, answer due day 4"), so D53's sentence went to the notes below the fold (D46), and that player waited a night for Dunn anyway.
- DESK-202: verified. After Dunn's answer the Coastal card quote ended "It leaves out operators, which SMSgt Ray Dunn said the request needs: add what you learned first; Coastal Rentals brings them at the same price." and, once added, opened "Coastal Rentals, material handling equipment for Hub with operators: $8,580 and no shop hours." Neither player asked whether the price covered drivers.
- DESK-203: verified. Both closed day 16's results with "On to the debrief" and only then saw the verdict; neither remarked on it.
- DESK-195 to DESK-197, DESK-199: not reached. Nobody bought on their own initiative, ended a contract or changed a COR.

## The first two minutes

The same for both and the same as every play: preparation with two options ticked and all five fitting in the fourteen days; Mills's four turnover pages with the commander's lines on the last; then the desk with one water request whose lead says "Order today: a card buy from Island Mart". Known (sixth play notes 1, 2).

## The plays in one paragraph each

First brief: water on the card from Island Mart on day 1 with Novak asking Ortiz; staff meeting on day 2; water ran at half on day 3 (a confusion day) and food service with it; waited a night for Dunn on day 4; on day 5 added operators and bought Coastal on the card, choosing it over a written order whose start might put forklifts out a day; waste on a written purchase order with Pratt on day 7; cure notice on the dining contract to Avery on day 13; forklifts at half on days 12 and 15. Won with nothing out and 97%.

Second brief: the same water buy on day 1; a BPA call with Harbor on day 4; on day 5, after Dunn's answer and Harbor's short week, stopped it ("Stop this work", nothing owed) and bought Coastal on the card (change of mind 1); priced moving water (day 4) and forklifts (day 10) onto written orders and dropped both; waste on a written order with Pratt on day 7; on day 13 gave the cure notice to Novak, then handed it to Avery from the team panel (change of mind 2). Won with nothing out and 97%.

## Notes, sorted

P1 is the first brief, P2 the second.

| Note | Where | What the player said | Sort | Row |
| --- | --- | --- | --- | --- |
| P1-1, P2-1 | Preparation | Two options pre-ticked; all five fit; the spare days have no use | Known (sixth play notes 1, 2) | none |
| P2-2 | Turnover | Who supplies forklifts on day 1: the host nation "for the whole deployment", the Harbor BPA or Coastal | Known (eleventh play P1-2) | none |
| P1-2, P2-3 | Day 1, request | The lead names the buy ("Order today: a card buy from Island Mart"), so the tutorial hands over the answer | Watched (D29: the request says its last day to order and what does it; this is the tutorial) | none |
| P2-4 | Day 1, Today | Water's tile "100% fully supported" while the water point is down until day 9 | Watched (stock on hand covers day 2; DESK-44 puts the reason beside a shortfall, not beside a covered day) | none |
| P1-3, P2-5 | Day 1, Buy | After a buy the blank form under it invites a second | Known (eleventh play P1-6) | none |
| P1-4, P2-6 | Day 1 night | Walk-in small buys first heard of at End the day, no counter | Known (DESK-149 c) | none |
| P2-7 | Day 1 night | Mills's note says to get the shop into Col Ward's staff meeting; the player had joined that morning ("the advice arrives after the decision it advises on") | Defect (scenario words) | DESK-207 |
| P2-8 | Day 2 | "1 request" counted the water request already arranged | Watched (the request is on the desk until its days end) | none |
| P1-5, P2-9 | Day 3 night | Food service went to half with water; nothing had said food service needs water ("No dependency checklist had shown that link") | Design (Sim words: the water request does not name what depends on it) | DESK-208 |
| P1-6, P2-10 | Day 3 to 4, contract page | A half-delivered day counted at its full price ("cost of support received $1,390.50" after one half day; Payments later the full $9,733.50) with no word on why ([picture](plays/2026-11-22/day4-half-day-full-cost.png)) | Defect (Sim words: the money line says nothing about the disputed day) | DESK-205 |
| P2-11 | Day 4, contract page | A Debrief tab in the contract page's tabs on day 4 ([picture](plays/2026-11-22/day4-half-day-full-cost.png)) | Defect (game: the contract page shows the Debrief tab before the game ends) | DESK-209 |
| P2-12 | Day 4, top bar | Avery "0 of 8 h assigned" beside the picker's "6.5 hours queued" | Watched (today's hours against work that starts tomorrow, after finance) | none |
| P1-7 | Day 4 | D53's sentence was not in the forklift summary; it was in the notes below the fold | Watched (D46: the leaves-out sentence names the answer due and takes the place) | none |
| P1-8 | Day 4 and 5 | Clicks and typing on fields scrolled out of the Buy pane fail silently | Known (tenth play P1-5) | none |
| P1-9 | Day 5, Buy | "Who writes it" labels change after a pick-two toggle, so the old label misses | Model (the driver matches whole labels) | none |
| P2-13 | Day 7 | The writer's quote said "6.5 to 13 hours"; the request then said "about 13 hours" | Watched (the plan forecasts the long end) | none |
| P2-14 | Day 7, request | An empty "Dependency checklist" heading for waste | Model (the heading sat at the foot of the pane, its line below) | none |
| P2-15 | Day 7, COR | What a report two days late means against a line that allows no day missed | Watched | none |
| P2-16 | Day 9 | The heading said "2 requests" while Requests listed three; water's request days were over, its card buy still delivering | Defect (Sim count: the heading counts requests whose days are not over, the list every request still being supplied) | DESK-206 |
| P2-17 | Day 9 | No way to confirm the water point returns on day 9; open sections close when clicked again | Watched | none |
| P2-18 | Day 10 | The staff meeting cannot be handed over; the COR cannot be changed once the order is placed | Known (twelfth play P2-12; the COR changes on the contract page, DESK-197) | none |
| P1-10 | Day 13 | "Open vendor decision" lands in the Inbox; ending is on the contract page | Known (DESK-153, DESK-173) | none |
| P2-19 | Day 13 | Handing work over only from the team panel; the cure notice 4 hours in the Inbox, 4.5 in the team panel | Watched (the hand-over row was the seventh play's; 4.5 not found in the play's screens, as in the twelfth play) | none |
| P1-11 | Night 13 | The day's plan showed "Deliveries and support due today" empty while the results showed waste starting | Model (the start line was half under the dialog's fold, with its scroll bar showing) | none |
| P1-12, P2-20 | Debrief | Every gap "Avoidable: what you needed to know was in front of you" on the card buys the game's own lead recommended, with no move that would have avoided them; "you could have avoided all of them" ("the main lesson reads as: you followed our advice, and that was your mistake") ([picture](plays/2026-11-22/day16-debrief-avoidable.png)) | Design (D54) | DESK-204 |
| P1-13, P2-21 | Debrief | "There were two more gaps" with three more in the list | Watched (food service is named as going down with water; the count is of the others) | none |
| P2-22 | Debrief | "The best screen in the game" | none | none |

## D54

The confusion rule (a service with no written terms runs at half on a day the vendor and the unit disagree) came up for the first time in a play: on this seed both players' card buys had three such days. D52 put the warning at the commit, and both players read it and chose the card anyway, because it was the trade the tutorial offers: the water lead said "Order today: a card buy from Island Mart", and the only written order started too late and put water out three days; the written forklift order might start a day late. The debrief then graded every half day "Avoidable: what you needed to know was in front of you" and the paragraph said "you could have avoided all of them". Both players read it as blame for following the game's advice, and both named the same change: say the move that would have avoided it. D54 in the design doc: a gap that is the known price of a buy with no written terms reads as a trade the player took, and says what written terms would have cost. Left out: computing a best route, and any change to tags or scoring.

## The four end answers

First brief. Who: the contracting officer for a 300-person hub with Avery and Novak, told so only on the first turnover page. Trying: Col Ward's four lines while the host nation pulled its support piece by piece. Managed: yes, "It was easy, because the summaries mostly tell you what to buy." One change: "Make the debrief, and the quote at the moment you buy, name the better choice ... As it stands, the main lesson reads as 'you followed our advice, and that was your mistake.'"

Second brief. Who: the hub's contracting officer with a fast MSgt, a new SrA, a card, cash, the O&M line and two inherited BPAs. Trying: the four lines. Managed: yes, "but I only held because half-delivered days counted as short, not out." One change: "For each 'avoidable' gap, the debrief (and ideally the quote) should show the move that would have avoided it ... Every quote pushed me to the card because the written orders started too late, and then the game blamed me for the card's predictable dispute days." Runner-up: hand work over from the task itself.

## Sim pass

`batch tutorial --seeds 200`: mission output median 99.7% textbook, 97.45% greedy, 79.6% do-nothing, as in the twelfth play; textbook's worst seed 97.3%. Textbook has up to four confusion days (median one), which is the trade D54 words.
