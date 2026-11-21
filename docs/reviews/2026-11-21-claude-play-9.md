# Claude play, 21 Nov 2026 (ninth)

The play due after the 21 Nov 16:45 run merged Codex's DESK-180 (the buy form's greyed reason brings its control into view; "done day after you pick two"; "Contract monitor (COR)", PR 462), DESK-181 (hand over and reach the contract page from where the work is, PR 463), DESK-182 (Vendors opens with its picker in view, PR 464), and Fable's DESK-178 (a heard short week refuses the call too, PR 465) and DESK-179 (forecast hours add up, PR 466) (PLAY-03, docs/PLAYTEST-CLAUDE.md section 2). DESK-181 changes where a hand-over is made, so two fresh agents played side by side: one with the section 3 brief, one with the section 3b second brief.

- Build: `fable/DESK-179` at 16917a2, the tree that merged as PR 466 onto `main` (8489fd3). Godot 4.6.1 .NET, 1280 by 800, Xvfb, Dummy audio.
- Seed: 20261121, as the earlier plays today.
- How far: both to the debrief after day 16, never stuck, all four pass lines met (first: water, forklifts and waste 0 days out, mission output 99%; second: all 0 out, 100%). 251 and 228 commands.
- Logs: [commands, first brief](plays/2026-11-21-9/commands-1.txt), [input log](plays/2026-11-21-9/input-log-1.json); [commands, second brief](plays/2026-11-21-9/commands-2.txt), [input log](plays/2026-11-21-9/input-log-2.json). The harness replays both logs to every line met (`run tutorial --log`).
- Pictures: three of 479: [day 8, an SF 44 bridge quoted at "no shop hours"](plays/2026-11-21-9/day8-sf44-no-shop-hours.png), [day 15, the waste failure warning opens Payments at the forklift contracts](plays/2026-11-21-9/day15-failure-opens-other-contract.png), [day 5, the COR picker half under the pinned footer](plays/2026-11-21-9/day5-cor-under-pinned-footer.png).

## What the merged rows did in play

- DESK-178: both players stopped the day-4 Harbor call on day 5 after Dunn's answer and the news; neither tried a new call during the short week, so the refusal was not reached. Not verified in play; covered by ShortWeekTests.
- DESK-179: neither player remarked on the plan's hours this time (the eighth play's second player said the roundings made it distrust the forecasts). Watched, not proven.
- DESK-180: the second player read "Pick two of fast, cheap and good" as the greyed reason and clicked it (command 85); the pick-two row came into view. Verified once. The COR picker in the same column still sat half under the pinned footer (note P2-19).
- DESK-181: the second player stopped the Harbor call from the request page and cancelled the vehicle from its contract page; neither handed work over. The Inbox's "Open its contract page" was not used. Not reached.
- DESK-182: not remarked on.

## The first two minutes

The same for both, and the same as every play today: preparation with two options ticked and all five fitting in the fourteen days, so both ticked everything; Mills's four turnover pages, with the commander's four lines on the last; then the desk with one water request and no "do this first". Known (sixth play notes 1, 2; D-rows on the turnover are open).

## The plays in one paragraph each

First brief: water on the card from Island Mart on day 1; joined the staff meeting from Mills's card on day 2; called Harbor on day 4 and asked Dunn; stopped the call on day 5 and split the forklifts into a two-day card buy and an eleven-day written order from Coastal; waste on a written order (fast and good) on day 7, then a two-day SF 44 bridge from the same vendor for days 13 and 14, quoted at "$192 and no shop hours", which then took two of its hours each delivery day; a cure notice on day 13. Day 15 morning: Today said 0 alerts while a warning said Main Island Sanitation had equipment down and the map ringed the Hub; the warning opened Payments at the Coastal forklift contracts. The bridge had covered the failure; the debrief never said so. Forklifts were at 50% on day 15 (unforeseeable). Won.

Second brief: water on the card on day 1; joined the staff meeting on day 2; bought a vehicle for the office on the card on day 3; called Harbor on day 4 and asked Dunn; stopped the call on day 5 (change of mind 1) and bought Coastal on the card for 13 days with operators, against Mills's advice, after finding the written order's COR row half hidden; waste on a written order with Pratt on day 7; tried on day 8 to replace the card buy with a written order from the same vendor (refused on capacity; left out on purpose in section 30); cancelled the unused vehicle on day 10 (change of mind 2); cure notice on day 13. Won with nothing out.

## Notes, sorted

P1 is the first brief, P2 the second.

| Note | Where | What the player said | Sort | Row |
| --- | --- | --- | --- | --- |
| P1-1, P1-2, P2-1, P2-2 | Preparation | Two options pre-ticked; all five fit; "may be working" against "works from day 3" | Known (sixth play notes 1, 2) | none |
| P1-3, P2-3 | Turnover | The commander's lines on the last page; host nation forklifts and a BPA for forklifts | Known | none |
| P1-5, P2-5 | Day 1, desk | "Open request" inside the card missed once | Watched (the driver's matching, the card is the button) | none |
| P1-7, P2-6, P2-13, P2-26 | Days 1, 4, 7 | The ask is answered tonight; the buy is due today | Known (D47 says it in the summary's start) | none |
| P1-8 | Day 1, buy | "Day 2 or 3" became day 3 at commit | Watched | none |
| P1-11, P2-8 | Day 1 night | Walk-in small buys first heard of at End the day | Known (DESK-149 c) | none |
| P1-12 | End the day | Dialog button and desk button share a label | Watched | none |
| P1-15 | Day 3, Vendors | "a vendor you meet does raise any contract of theirs this office has left unfinished" does not parse | Defect (a sentence) | DESK-183 |
| P1-17, P2-14 | Day 4 | "8 hours free, 6.5 queued"; the strip shows only today | Watched | none |
| P1-18, P2-18 | Day 5, buy | "Add what you learned" appears only once a supplier is picked; the half-supply penalty for no written terms is in the notes | Known (D46, D48) | none |
| P2-19 | Day 5, buy | After Fast and Good, the COR picker sits half under the pinned writers and quote; the supplier row scrolled away | Defect (layout; DESK-148 kept the footer to half the column, and the COR row is the one cut) | DESK-187 |
| P1-20 | Day 5, buy | The written order's quote warned "out 2 days" before the card buy beside it existed | Model (the order was quoted first, and the warning was true then) | none |
| P1-21 | Day 5, buy | A 2-day card buy has no "no written terms" warning; 7 and 13 days do | Watched (the threshold is the Sim's; the note appears where it bites) | none |
| P2-21 | Day 5, plan | "No deliveries or support starts or ends are due today" on the day the office vehicle started | Defect (the plan leaves out the shop's own support) | DESK-185 |
| P1-23 | Day 7, buy | The waste order is written by day 9 and starts "day 13" as if certain | Watched (it did start on day 13) | none |
| P1-24, P2-24 | Day 7, COR picker | Lane "catches about 40%" here and "about 85%" on Payments for food service; nothing says why | Design (the Sim's reason, "does not work with waste removal", is not on the choice) | DESK-186 |
| P1-26 | Day 8, buy | The SF 44 bridge quoted "$192 and no shop hours"; each delivery then took 2 of the player's hours | Defect (the quote leaves out the trips) | DESK-183 |
| P1-27 | Day 13 | Terminate is not offered on the vendor decision | Known (DESK-181 b links the contract page) | none |
| P1-29, P1-35 | Day 15, Today; debrief | 0 alerts and "met so far" beside "had equipment down" and a red ring; the debrief never mentions the failure the bridge covered | Design (D50) | DESK-184 |
| P1-30 | Day 15, Today | The failure warning opens Payments at the Coastal forklift contracts, not the failed waste order | Defect | DESK-187 |
| P1-31, P1-33, P2-35 | Days 15, 16 | Every stopgap starts after the tour ends; Coastal's card says 0 days after a signed order | Known (eighth play P1-43, watched); the card's "0 days" is the written order's lead, the card buy's is a day or two | none |
| P1-36, P2-38 | Debrief | The full record is dated a day after the events (the morning a result is read) | Watched | none |
| P2-9 | Day 1 night | Where Join the staff meeting lives | Known (Mills's card carries it) | none |
| P2-11 | Day 3 | "Nothing moves today" on a delivery day | Watched (the map moves boats; a card delivery on the main island has none) | none |
| P2-27 | Day 8, contract page | A Debrief tab on the contract page's tab row | Watched (DESK-171 put the desk tabs there; it says when the debrief comes) | none |
| P2-28 | Day 8, buy | A written order from the vendor already on the card is refused on capacity | Known (section 30: left out on purpose) | none |
| P2-31, P2-36, P1-34 | Day 10; debrief | The vehicle's hours saved never shown; the COR choice "would not have changed that day" | Watched | none |
| P2-37 | Debrief | The 13-day card buy with no written terms was never graded | Watched (it never bit; the debrief grades what happened) | none |
| P2-39 | Debrief | "Trace the evidence" off-screen | Watched | none |

## What is new

1. **A covered failure reads as an unanswered one (D50, DESK-184).** On day 15 the first player's Today said 0 alerts and waste "met so far" while a warning said Main Island Sanitation had equipment down and the map ringed the Hub. The Sim had it right: the SF 44 bridge the player bought on day 8 covered the day, so nothing was short. Nothing on screen said so, and the debrief left it out. The player could not tell whether the alarm was false or its bridge had worked. This is the vendor case of DESK-170 (a covered host-nation failure).
2. **An SF 44 quote leaves out its trips (DESK-183).** "$192 and no shop hours" for two deliveries, each a two-hour trip for the player and the paying agent. The record of the buy says it; the quote does not.
3. **Small defects.** The failure warning opens Payments at another contract (DESK-187 a); the COR row sits half under the pinned footer once pick two is shown (DESK-187 b); the day's plan leaves out the office's own support starting (DESK-185); a vendor-meeting sentence that does not parse (DESK-183); the COR choice does not carry the reason its rate differs between contracts (DESK-186).

## End answers

First brief. Who: the new contracting officer at a 300-person island hub, with Avery and Novak. Trying: keep the commander's four lines for 16 days. Managed: yes, all four; "I did it by guessing my way through a lot of the interface". One change: "Make the quotes and alerts tell the truth about the whole picture. Each quote should account for my other arrangements on the same request and carry every cost, including the hidden 2 hours a day for SF 44 delivery. A real failure should be at the top of Today as an alert, not tucked under 'Other updates and sites' while the header says '0 alerts'."

Second brief. Who: the newly arrived contracting officer for the hub. Trying: the four lines. Managed: yes, nothing out, 100%; "it was easy. Every crisis came with a day or more of warning, and the quotes all but told me the answer ... So I won without learning whether I'd played well." One change: "Put every consequence that matters on the quote's headline line, before I commit": the half-supply penalty for no written terms, an answer due after the order deadline, a COR whose report lands after the tour.

## Sim pass

`batch tutorial --seeds 200`, three policies: identical to the 21 Nov 16:45 run's before-and-after (DESK-178 moved nothing; DESK-179 is words only). Textbook meets every pass line as before. The second player's "it was easy" is the same note as the earlier plays today: the tutorial teaches one scenario and its pressure is mild by design; Spoke B carries the depth (M3).
