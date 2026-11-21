# Claude play, 21 Nov 2026 (eighth)

The play due after the 21 Nov 14:45 run merged Codex's DESK-171 (the map label opens what it names; desk tabs on the contract page, PR 454), DESK-173 (from a request to the contract you would cancel, PR 455) and DESK-177 (monitor odds in the picker; the cancel keeps who has it, PR 456), Fable's DESK-170 (a covered host-nation failure in New since yesterday, PR 453, merged just before), DESK-172 (D48, PR 457), DESK-174 (Harbor's short week, PR 458), DESK-175 (D49, PR 459) and DESK-176 (the hand-over's done day, PR 460) (PLAY-03, docs/PLAYTEST-CLAUDE.md section 2). DESK-176 changes what a hand-over forecasts and DESK-177 what an ending says, so two fresh agents played side by side: one with the section 3 brief, one with the section 3b second brief (change your mind twice). Each had the driver and a notes file of its own; neither read the repository.

- Build: `fable/DESK-176` at 3ed1c22, the tree that merged as PR 460 onto `main`. Godot 4.6.1 .NET, 1280 by 800, Xvfb, Dummy audio.
- Seed: 20261121, as the earlier plays today.
- How far: both to the debrief after day 16, never stuck, all four pass lines met (water 0 days out, forklifts 0, waste 0, mission output 100%). 199 and 239 commands.
- Logs: [commands, first brief](plays/2026-11-21-8/commands-1.txt), [input log](plays/2026-11-21-8/input-log-1.json); [commands, second brief](plays/2026-11-21-8/commands-2.txt), [input log](plays/2026-11-21-8/input-log-2.json). The harness replays both logs to every line met (`run tutorial --log`, state hashes ac5f93f3 and 24e7aebc).
- Pictures: three of 440: [day 5, the call's quote says it starts on day 8](plays/2026-11-21-8/day5-call-quote-starts-day-8.png), [day 5, the card says Harbor has nothing to spare through day 8](plays/2026-11-21-8/day5-card-harbor-short.png), [day 8, "8 of 8 hours planned" over 1 and 7.5 hours](plays/2026-11-21-8/day8-plan-8-of-8.png).

## What the merged rows did in play

- DESK-172 (D48): the first player's day-4 call quote said in amber "It leaves out operators; SMSgt Ray Dunn has not been asked", and it asked Dunn the same hour; the second player read the same sentence and asked too. Both added operators before buying. Verified: neither bought blind, which the sixth play did.
- DESK-174: the second player saw the card refused with Harbor's shortage on day 5. Verified for the card, and it exposed a contradiction: the same day the BPA call's quote still said Harbor "starts on day 8" (note P2-10, a defect this row created, DESK-178).
- DESK-175 (D49): after adding operators the second player read "Harbor Equipment Co does not bring operators, which the request includes; without them it fails more often every day it runs" on the call and concluded the shop's BPA vendor was the wrong one. Verified.
- DESK-176: the second player handed the waste order from itself to Avery on day 7 (same finish day 9, which it was) and the cure notice from Novak to Avery on day 13; both forecasts held. Verified, with two half-hour roundings that do not add up (note P2-15, DESK-179).
- DESK-177: the first player read the monitor picker's odds ("Pratt catches 80% but reports 2 days late; Lane catches 40% the same day") and called it "a real, well-explained trade-off". Verified. The in-work cancel was not reached.
- DESK-171, DESK-173, DESK-170: not reached in a way either player remarked on.

## The first two minutes

The same for both: preparation with two options ticked, Mills's four turnover pages, then the desk with one water request. Both checked every preparation option because all five fit and nothing says what an unused day is worth, and the first found the commander's four lines only on the fourth page (Known: the sixth play's notes 1, 2, 4).

## The plays in one paragraph each

First brief: pulled the agreements and ticked everything; water on the card from Island Mart on day 1; sent Novak to the staff meeting; asked Dunn on day 4 when the call's summary named operators unconfirmed; added operators on day 5, bought Coastal on the card and stopped the Harbor call; waste on a written purchase order (fast and good, Pratt) on day 7; a cure notice on day 13; nothing to do on days 9 to 12. Waste ran at 50% on day 15 with Pratt's report due day 17, after the end. Second brief: the same opening; on day 5 it saw the call quote say day 8 and the card say Harbor had nothing until day 9, read D49 on the call, and bought Coastal with operators. It changed its mind on day 7 (the waste order from itself to Avery) and on day 13 (the cure notice from Novak to Avery), and tried on day 9 to replace the card buy with a written order from Coastal, which Coastal's capacity refused because the card buy holds the forklifts.

## Notes, sorted

P1 is the first brief, P2 the second.

| Note | Day, screen | What the player said | Sort | Row |
|---|---|---|---|---|
| P1-1, P1-2, P2-1, P2-2 | Preparation | Two options pre-ticked; all five fit; what is an unused day worth | Known (sixth play notes 1, 2) | none |
| P1-3 | Preparation | Paying agent, vendor cards, the deployment's length unexplained | Known (sixth play note 4) | none |
| P1-4 | Turnover 2 | Harbor's BPA promised in prep, named a page later | Watched | none |
| P1-6, P1-7, P1-10 | Turnover 4 | The goals in the last paragraph of page 4; "short" not in the goals | Known (sixth play note 4; D-series turnover) | none |
| P1-8 | Turnover 4 | Cash refill "every few days" against prep's "3 days after it runs low" | Watched (both true: the prep choice shortens it) | none |
| P1-9, P2-4 | Today | "Open request" scrolls out of view; the card below becomes the target | Watched (two ways in, both open the same page) | none |
| P1-11, P1-16 | Today | Water "100% fully supported" the morning the water point is down; no stock shown | Watched (the stock covers day 1, as the hint says) | none |
| P1-12, P2-6 | Buy | Quotes transparent; the no-written-terms trade clear | Watched | none |
| P1-13, P2-12 | Buy | The arrangement picker, pick two and the monitor scroll out of view while "Pick two of fast, cheap and good" is in view | Defect (the greyed reason names a control the player cannot see) | DESK-180 |
| P1-14, P2-5 | Buy | Every writer "done day unavailable" until pick two, with no reason; the labels then change under the click | Defect | DESK-180 |
| P1-23, P1-38 | Night 2, Payments | Mills says "COR", the buy form says "Contract monitor" | Defect (one name on both) | DESK-180 |
| P1-17, P1-18, P1-19, P2-7 | Day 1 | Idle hours and walk-in buys learned only on the day's plan | Known (DESK-149 c) | none |
| P1-20 | Night 1 | Mills's staff-meeting tip comes the night after | Watched (the meeting starts day 2; the tip is in time for it) | none |
| P2-3 | Request | Who does it beside Ask: the choice shows only in the line under | Watched | none |
| P2-8, P2-20 | Vendors | The tab reopens scrolled to the bottom of the last card; the picker is off screen and clicks on vendor names fail | Defect | DESK-182 |
| P1-24 | Vendors | No way to check ahead whether Harbor would fill a call | Watched (the BPA's not-binding note and Mills's warning are the check) | none |
| P1-26 | Day 4, buy | Amber "leaves out operators"; silent on maintenance, which the call brings | Watched (true: the call brings it) | none |
| P1-27 | Day 4, buy | Monitor trade-off "real, well-explained" | Verified (DESK-177 a) | none |
| P1-28 | Day 4, buy | The call waits for finance on day 5; could have waited for Dunn | Known (D47 says it in the summary's start) | none |
| P1-29 | Day 4, buy | "Through the end of the month" against 13 days, no calendar | Watched | none |
| P2-9 | Day 4, request | The request lists forklifts and maintenance; operators only in the quote's amber | Watched (D48's purpose: ask before buying) | none |
| P2-10 | Day 5, buy | The call's quote says Harbor starts day 8; the card says Harbor has nothing through day 8 | Defect (DESK-174 refused the card but left a heard call quoting a start; the call is declined at award) | DESK-178 |
| P1-31 | Day 5, buy | "Add what you learned" found deep in the buy pane by accident | Known (it is on the request page too, DESK-119) | none |
| P1-32 | Day 5, request | "Stop this work" below the fold; first click missed | Model (the driver's click on an off-screen line) | none |
| P1-33, P1-34 | Day 5, buy | The card beat the written order; 13 days of service on a card, 0 shop hours, nothing questions it | Watched (the no-written-terms note is the question; D45 in the debrief) | none |
| P2-11 | Day 5, buy | Clicking "MSgt Joan Avery" opened the team page, not the writer | Model (the driver matched the top-bar name; a mouse would not) | none |
| P1-36, P2-13 (a) | Day 7, buy | "8 to 16 hours of your time"; whose hours | Known (the picker's line names whose) | none |
| P2-13 (b), P2-17 | Day 7, request; day 13, Inbox | The request page has Stop this work but no hand-over; the vendor decision has two of three answers, ending is on the contract page | Defect (the hand-over and the end are where the work is) | DESK-181 |
| P2-14 | Day 7, team | The hand-over worked; same-label buttons per task | Verified (DESK-176) | none |
| P2-15, P2-18 | Night 8 plan; day 13 | "8 of 8 hours planned" over 1 and 7.5 hours; 13 hours became 7.5 and 6; 4 hours on the decision, 4.5 on the team page | Defect (half-hour rounding per line, not per person) | DESK-179 |
| P2-16 | Day 9, buy | A written order from Coastal to replace its own card buy is refused: the card buy holds the forklifts | Watched (true by capacity; the replacement from the same vendor is a cancel then a rebuy, or another vendor; left out: a conversion of a card buy, an administration step Nick does not want) | none |
| P1-39 | Day 9, Payments | Island Camp Services commitments above the O&M line | Model (the inherited BPAs were obligated before the player arrived; the line shows what is left) | none |
| P1-40, P1-41, P1-42 | Payments, Inbox | Invoices mine?; no consequence per cure option; the link switches to Inbox | Watched | none |
| P1-43, P1-45, P2-19 | Day 15, 16 | Waste at 50% on day 15, Pratt's report due day 17, after the end; no lever to chase the vendor | Watched (the debrief calls the day unforeseeable and says a same-day monitor would not have changed it; the player chose the late monitor knowingly) | none |
| P1-44 | Day 16, bar | 50% "short, not out. Met." against "not one day missed" | Known (D-series: the bar's own sentence says it) | none |
| P1-46, P1-47, P1-48 | Debrief | "Tutorial" first said here; mission output carried by the lines; 33 walk-in buys | Watched | none |
| P2-21 | Days 9 to 16 | End the day twice a day on quiet days | Known (Skip ahead) | none |
| P2-22 | Debrief | Silent on the hand-overs and the refused replacement | Watched (nothing went short because of them) | none |

## The end answers

First brief. Who: a contracting officer at the Hub, 300 people, a 16-day tour, Avery and Novak. Trying: keep Col Ward's four lines. Managed: yes, all four. "I came close to failing on forklifts: the BPA on Harbor Equipment would have had no drivers, and that vendor was short that week. I only switched in time because I asked the requester." One change: put the commander's four goals and how they are scored on one labelled screen on day 1, and give each of Mills's tips before the day it applies.

Second brief. Who: the new contracting officer at the Hub. Trying: the four lines while the host nation withdrew support. Managed: yes, zero days out, one short day on waste; "the tutorial was easy once you read the quotes, and about half the days needed no decisions at all". One change: every open contract or task page offers what you would want to do with it (hand it over, reach the vendor or monitor, replace it), including ordering the replacement before releasing the current one.

## Fable's reading

1. **The parts sentences work (D48, D49).** Both players asked Dunn because the summary said operators were unconfirmed, and the second rejected the shop's own BPA vendor on D49's sentence. The sixth play's lesson is now taught by the screen it is read on.
2. **DESK-174 left a contradiction (P2-10).** The card is refused for Harbor's short week, but a call quoted after the shop has heard of the shortage still promises day 8, and is then declined at award. Once the news is out, the call is refused with the same shortage (DESK-178, Fable). Before the news the call goes in and is declined, as the tutorial teaches.
3. **Forecast hours that do not add up (P2-15, P2-18).** Every line rounds up to the half hour on its own, so an experienced person's 0.75-hour meeting and 7.25 hours of writing read 1 and 7.5 under "8 of 8 planned". Small, and it made the player "distrust the forecasts" (DESK-179, Fable).
4. **Changing your mind is spread over three pages (P2-13, P2-17).** Stop is on the request page, hand-over on the team page, ending on the contract page. DESK-181 (Codex) puts the hand-over where Stop is and the contract link on the vendor decision. Left out: converting a card buy into a written order (P2-16), which is an administration step.
5. **The buy form still hides what its reason names (P1-13, P2-12, P1-14, P2-5).** DESK-180 (Codex).

## Sim pass

`batch tutorial --seeds 200`, three policies, on this tree: identical to the run before DESK-174 (textbook median mission output 0.997, every pass line median met; greedy 0.9745 with forklifts out a median 1 day; do-nothing 0.796). No policy beats textbook; no measure is unmoved by every policy.
