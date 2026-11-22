# Claude play, 22 Nov 2026 (second)

The verification play due once the first 22 Nov play's rows landed: Fable's DESK-204 (D54, landed with PR 494), DESK-205 (the contract page says what a disputed day cost, PR 496), DESK-206 (the heading's request count, PR 497), DESK-207 (Mills's staff meeting note only to a shop not in it, PR 498), DESK-208 (a request names what depends on it, PR 499), and Codex's DESK-209 (no Debrief tab before the end, PR 495) (PLAY-03, docs/PLAYTEST-CLAUDE.md section 2). Ending a contract (DESK-196) had still not been through a play, so two fresh agents played side by side again: one with the section 3 brief, one with the section 3b second brief. Each had the driver and a notes file of its own; neither read the repository.

- Build: `main` at 18a34a4 (PR 499 merged). Godot 4.6.1 .NET, 1280 by 800, Xvfb, Dummy audio.
- Seed: 20261122, the day's seed, so the first 22 Nov play's confusion day on the water card buy came back and the fixes met the same situation.
- How far: both to the debrief after day 16, never stuck, all four pass lines met with 0 days out everywhere; mission output 99.4% both (97% in the first play). 175 and 211 commands (the first command of each log is the runner's `look`).
- Logs: [commands, first brief](plays/2026-11-22-2/commands-1.txt), [input log](plays/2026-11-22-2/input-log-1.json); [commands, second brief](plays/2026-11-22-2/commands-2.txt), [input log](plays/2026-11-22-2/input-log-2.json). The harness replays both logs to every line met (`run tutorial --log`); two gaps each, water and food service on day 3, water's a confusion day on the card buy.
- Pictures: three of 388: [day 4, the water contract page saying what the disputed day cost](plays/2026-11-22-2/day4-disputed-day-explained.png), [day 1, the water quote with its no-written-terms warning below the fold](plays/2026-11-22-2/day1-water-quote-no-terms-below.png), [day 16, the debrief naming written terms that could not have started in time](plays/2026-11-22-2/day16-debrief-written-terms.png).

## What the merged rows did in play

- DESK-205: verified. The first player's day-4 water page read "Day 3 was disputed and half arrived; the card had already paid for the whole day, and with nothing written there is no claim." under $1,390.50 received. The player's note: "There is no way to put terms in writing or make a claim", which is what the line says; no note asked why the whole day was paid.
- DESK-206: verified. Both day-9 headings read "3 requests", matching the list.
- DESK-207: verified both ways. The second player joined the staff meeting on day 1 and got only Mills's agreements note; the first joined on day 2 and got "Capt Mills on the staff meeting" with its Join action that morning.
- DESK-208: verified on screen (both players' water requests showed "Food service at Hub needs water: when water runs short, food service runs short with it."). Neither was surprised by food service at half on day 3; the first play's P1-5 and P2-9 did not recur.
- DESK-209: verified. No contract page showed a Debrief tab before the end.
- DESK-204 (D54): shown as built ("Avoidable: a risk you took; the quote said there were no written terms.", and the written-terms sentence), and still not accepted: see P1-14 and P2-15 and D55.
- DESK-195 to DESK-197, DESK-199: not reached. The second player cancelled a card buy (the card path, DESK-94) and stopped a BPA call in work, but nobody ended a written contract, bought on their own initiative or changed a COR.

## The first two minutes

The same for both and the same as every play: preparation with two options ticked and all five fitting in the fourteen days; Mills's four turnover pages with the commander's lines on the last; then the desk with one water request whose lead says "Order today: a card buy from Island Mart". Known (sixth play notes 1, 2).

## The plays in one paragraph each

First brief: water on the card from Island Mart on day 1 with Novak asking Ortiz; joined the staff meeting on day 2; water at half on day 3 (a confusion day) and food service with it; on day 5, after Dunn's answer, a fast-and-good purchase order with Coastal Rentals written by Avery with Pratt as COR, passing over the lead's card buy; waste on a written order on day 8; cure notice on the dining contract to Avery on day 13. Won with nothing out and 99.4%.

Second brief: the same water buy and staff meeting on day 1; on day 4 an SF 44 bridge from Harbor Bottling for days 7 to 10 and a BPA call with Harbor Equipment for forklifts; on day 5 stopped the call ("Stop this work", nothing owed) and bought a two-day Coastal card bridge plus a written Coastal order from day 10 (change of mind 1); on day 6 handed Novak the call to cancel the Island Mart card buy (change of mind 2); on day 7 handed the waste order's writing to Avery (change of mind 3); cure notice through Novak on day 13. Won with nothing out and 99.4%.

## Notes, sorted

P1 is the first brief, P2 the second.

| Note | When, where | What the player saw | Sort | Row |
| --- | --- | --- | --- | --- |
| P1-1, P2-1 | Preparation | Two options ticked, all five fit in 14 days: no trade-off | Known (sixth play note 1) | none |
| P1-2 | Preparation | "SCO" never spelled out; the team page calls Avery an experienced contracting officer, the turnover MSgt | Watched | none |
| P1-3, P2-2 | Turnover | The four lines at the end of page 4; nothing links "jets keep flying" to anything you buy | Known (sixth play note 2) | none |
| P2-3 | Turnover | Page 2 says the host nation has material handling equipment "for the whole deployment", then the yard needs forklifts | Watched (the agreement's paper position; Mills foreshadows the withdrawal, which is the lesson) | none |
| P1-4 | Turnover | Honey trucks, waste removal and latrines name one service | Watched | none |
| P1-5, P2-4 | Day 1, desk | No pointer to tabs or map; the lead hands over the buy | Watched (D29) | none |
| P1-6, P2-5 | Day 1 onward | Clicks and typing on controls scrolled out of a pane fail silently | Known (tenth play P1-5) | none |
| P1-7 | Day 1, Buy | Pick two row partly under "Who writes it"; a ticked box lost its frame | Watched (one screenshot each, not seen in the second play) | none |
| P2-6 | Day 1, Buy | After an order the form keeps its quantity, so ordering twice is easy | Known (eleventh play P1-6) | none |
| P1-8, P2-7 | Day 1 night | Walk-in small buys first heard of at End the day; the count only in the debrief | Known (DESK-149 c) | none |
| P2-8 | Day 1, water quote | The no-written-terms warning on the water card buy was not in the quote's three sentences; on day 5 the Coastal quote said it in full ([picture](plays/2026-11-22-2/day1-water-quote-no-terms-below.png)) | Defect (Sim words: on a quote with both, the answer-tonight sentence took the third place and the D52 warning went below the fold) | DESK-212 |
| P1-9 | Day 2 | Meeting the Harbor Equipment rep says it adds nothing | Watched (the meeting's preview says so) | none |
| P1-10 | Day 4, contract page | The only action is Cancel; no way to write terms or claim | Watched (DESK-205's line says there is no claim; DESK-97 modifies a written contract) | none |
| P1-11 | Day 5, Requests | The lead pushed a card buy from Coastal, the same no-terms trap that had just cost water, against Mills's advice; the written order, quoted "day 8 or 9", started day 8 | Design (D55 b) | DESK-211 |
| P1-12, P2-9 | Day 5, Buy | "Add what you learned" appears only after a supplier is picked | Watched (DESK-77, DESK-202) | none |
| P2-10 | Day 4 to 5 | Vendor card "starts 1 day after a signed order", request "starts 3 days after it goes in" | Watched (the request counts writing and certification; the vendor card does not) | none |
| P2-11 | Day 6, request | Two "Open its contract page →" buttons with no supplier name; the second could not be told apart | Defect (game: the label names whose contract) | DESK-214 |
| P1-13 | Day 12 | Skip ahead said day 13 and stopped on day 12 | Model (the line says "Stops when anything needs you"; the COR's report came on day 12) | none |
| P2-12 | Day 11 to 12 | The COR reported short staffing while the day said everything was supplied in full | Watched (stock on hand covered both missed days, as the records say) | none |
| P1-14, P2-15 | Debrief | "Written terms settle a dispute: a purchase order or a BPA call" on water, where the day-1 purchase order started day 4 to 6 (water out up to three days) and there was no water BPA ("the debrief should only call something avoidable if an option existed that would have worked") ([picture](plays/2026-11-22-2/day16-debrief-written-terms.png)) | Design (D55 a) | DESK-211 |
| P1-15 | Debrief | Food service counted as a second avoidable mistake, though it is the same water failure | Design (D55 c) | DESK-211 |
| P2-16 | Debrief | The evidence trace says "water on hand ran out" where the day said Island Mart delivered half | Defect (Sim words: the shortfall reason names the stock, not the half delivery) | DESK-213 |
| P1-16 | Debrief | "Trace the evidence" from the shortage back to the day-1 buy | none (praise) | none |

## D55

Two plays on this seed now read the debrief's water gap the same way: the game recommended the card, its own quote showed the written order failing the line, and the debrief named written terms as the fix. D54 made the tag line a trade, which both players saw, but the factor sentence still names "a purchase order or a BPA call" as if either were on offer. D55 in the design doc: the written-terms sentence says what the written route would have cost on that request, from the same quote the player saw at the buy, or that none could have started in time; the lead, for a recurring service, names the written order when its quote keeps the line; and a knock-on of a trade reads as part of it. Left out, as in D54: changing the tag, scoring or the confusion rule, and computing a best route.

## The four end answers

First brief. Who: the new contracting officer at a 300-person hub with Avery (fast, experienced) and Novak (new). Trying: the commander's four lines for 16 days while the host nation pulled forklifts and the honey trucks. Managed: yes, one day of water at half from the card buy. One change: "make the hints, the quotes and the debrief agree on what the right move was ... the debrief should only call something avoidable if an option existed that would have worked."

Second brief. Who: the incoming contracting officer for the hub, with Avery, Novak, $15,000 cash, the card and the O&M line. Trying: the four lines through the water point outage, the forklift withdrawal and the honey trucks. Managed: yes, 99%, one blemish on day 3. One change: "make every quote spell out the risk of its arrangement as loudly as the day-5 Coastal quote did"; close second, fields that ignore typing when scrolled away and identical "Open its contract page →" labels.

## Sim pass

`batch tutorial --seeds 200` on main at 18a34a4: as in the first 22 Nov play except events.fired (7 for do-nothing and greedy, since DESK-207 split Mills's note). Mission output median 99.7% textbook, 97.45% greedy, 79.6% do-nothing; textbook's worst seed 97.3%. No policy beats textbook on a pass line.
