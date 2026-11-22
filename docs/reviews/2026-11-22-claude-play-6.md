# Claude play, 22 Nov 2026 (sixth)

The verification play due once the fifth 22 Nov play's rows landed: Fable's DESK-221 (a start or end record says its day, PR 514) and DESK-222 (D58: the gap count says how many avoidable gaps were risks worth taking, PR 515) (PLAY-03, docs/PLAYTEST-CLAUDE.md section 2). Ending a written contract, buying on your own initiative and changing a COR (DESK-195, DESK-197, DESK-199) had still not been through a play, so two fresh agents played side by side again: one with the section 3 brief, one with the section 3b second brief. Each had the driver and a notes file of its own; neither read the repository.

- Build: `main` at 7b6566a (PR 515 merged). Godot 4.6.1 .NET, 1280 by 800, Xvfb, Dummy audio.
- Seed: 20261122, the day's seed for the sixth time.
- How far: both to the debrief after day 16, never stuck, all four pass lines met with 0 days out everywhere; mission output 99.4% both. 186 and 332 commands (the first two `look`s of the first log are the runner's).
- Logs: [commands, first brief](plays/2026-11-22-6/commands-1.txt), [input log](plays/2026-11-22-6/input-log-1.json); [commands, second brief](plays/2026-11-22-6/commands-2.txt), [input log](plays/2026-11-22-6/input-log-2.json). The harness replays both logs to every line met (`run tutorial --log`); two gaps each, water and food service on day 3.
- Pictures: four of 518: [day 4, the quote's "ordered tomorrow, after the answer, it still starts on day 8"](plays/2026-11-22-6/day4-ordered-tomorrow-still-day-8.png), [day 6, the forklift request naming only the one-day bridge](plays/2026-11-22-6/day6-forklifts-through-day-8-only.png), [day 8, "started material handling equipment at Hub 3 forklifts."](plays/2026-11-22-6/day8-started-at-hub-3-forklifts.png), [day 16, the debrief's two risks worth taking](plays/2026-11-22-6/day16-debrief-risks-worth-taking.png).

## What the merged rows did in play

- DESK-221 and DESK-222: not reached. Both live in "The full record", and neither player opened it: both read the verdict, Cole's paragraph and the two gap buttons, and stopped. The harness replay of both logs prints the record as DESK-221 says ("Material handling equipment at Hub from Coastal Rentals began on day 8, 3 forklifts/day."; "... ended: its last day was day 7."), and DebriefVerdictSmokeTest covers DESK-222's count on screen. Nobody misread the debrief this time: both called it the clearest screen in the game and took "a risk worth taking" at its word. What was left over is not the count but when the judgement comes (P1-26 below).
- DESK-218 (D57 a): seen again. Both read "ordered tomorrow, after the answer, it still starts on day 8" on day 4 and waited the night. The first player then found Harbor short of forklifts on day 5 and read the sentence as a promise broken (P1-17).
- DESK-216, DESK-217 (c): the cure notice again, Re-source greyed with its reason; read without trouble.
- Not reached before, used now: the second player ended nothing written but cancelled a card buy from its contract page on day 15 ("The confirmation dialog listed what would stop and said the whole $450 would come back. Clear and clean.") and read the End this contract page on day 11 without using it ("spelled out the money clearly"). It found "Buy on your own initiative" only on day 15.
- DESK-195, DESK-197, DESK-199, DESK-217 (a, d, e): still not reached. Nobody added to a contract, changed a COR, or saw an own-initiative buy being written.

## The first two minutes

The same for both and the same as every play: preparation with two options ticked and all five fitting in the fourteen days; Mills's four turnover pages with the commander's lines on the last; then the desk with one water request whose lead says "Order today: a card buy from Island Mart". Known (sixth 21 Nov play notes 1, 2).

## The plays in one paragraph each

First brief: water on the card from Island Mart on day 1; Novak into the staff meeting on day 2; water at half on day 3 (the confusion day) and food service with it; on day 4 looked for a written fix for water and found none in time, then asked Dunn about the forklifts and waited the night on the quote's word; on day 5, with Harbor short and Dunn's answer in, a fast-and-good Coastal purchase order with operators from day 8 (Pratt as COR), Avery writing; waste on a purchase order on day 8 with Lane as COR; cure notice through Avery on day 13. Won with nothing out and 99.4%.

Second brief: the same water buy on day 1; joined the staff meeting itself; on day 4 priced written replacements for the water card and kept it; on day 5 a one-day Coastal card bridge for day 8 and a fast-and-good Coastal purchase order from day 9 (Pratt as COR), Avery writing; waste on day 7 or 8 given to Novak, then handed to Avery (change of mind 1); weighed swapping Coastal for a cheaper Harbor call on day 11 and kept it; cure notice through Avery on day 13; on day 15 put a card buy on an office vehicle and cancelled it before it started (change of mind 2). Won with nothing out and 99.4%. It said itself that the second change was made for the brief: "Nothing in the game forced a real reversal after day 5."

## Notes and how they sort

P1 is the first brief, P2 the second. F is Fable's own read of the screens.

| Note | Where | What the player said | Sort | Row |
| --- | --- | --- | --- | --- |
| P1-1, P1-2, P2-1, P2-2, P2-3 | Preparation | Two options ticked with no reason; all five fit, so no choice; two options below the fold; nothing says "Tutorial" | Known (sixth 21 Nov play notes 1, 2) | none |
| P1-3, P1-4, P2-4, P2-5 | Turnover | Host nation forklifts "for the whole deployment" beside a forklift BPA; goals only on the last page; the jets never mentioned before | Known (third 21 Nov play P1-2, P1-3) | none |
| P1-5, P1-9, P2-6, P2-7, P2-13 | Day 1 onward | "Open request" and pickers scrolled out of the pane cannot be clicked; small scroll steps | Known (tenth play P1-5) | none |
| P1-9, P2-10 | Day 1 | The You, Avery and Novak rows are a picker with nothing saying so; the Ask button below them | Known (DESK-127, DESK-181) | none |
| P1-6 | Day 1 | SF 44 offered for Island Mart until the supplier was picked | Watched (one player) | none |
| P1-7, P2-9 | Day 1 | "Pick two of fast, cheap and good" sits under a greyed Arrange support; "done day after you pick two" | Watched (two players, no wrong buy) | none |
| P1-10 | Day 1 | After the buy the form resets to a blank one, which invites a duplicate; the buy went in before Ortiz's answer | Watched (DESK-32 names what is in place in the next quote; D51 says what waiting costs) | none |
| P1-11, P2-14 | Day 1 night | Walk-in small buys first heard of at End the day | Known (DESK-149 c) | none |
| P2-11 | Day 1 | Water "100% · fully supported" with the water point down until day 9 | Watched (stock on hand; one player) | none |
| P2-12, P2-15 | Days 1, 3 | What staying away from the staff meeting costs; once joined, no way to hand the hour to someone else | Watched (stop and rejoin is the path; one player) | none |
| P2-16 | Day 3 | The Today pane "briefly rendered every line twice" | Watched (not in its screenshots or screen text) | none |
| P1-13, P2-17 | Day 3 | No odds given for a dispute | Watched (D56: a percentage would invite playing the roll) | none |
| P1-15, P2-18 | Day 4 | Looked for a written fix for the water card and found none in time; the card held Island Mart's capacity | Watched (fourth play P1-17; both quotes answered it) | none |
| P1-14, P2-19 | Day 4 | "Ordered tomorrow, after the answer, it still starts on day 8": both waited | none (DESK-218 seen again) | none |
| P1-17, P1-23 | Days 5, 7 | The day-4 line read as a promise, and Harbor was short on day 5; on day 7 "I no longer trusted it" ([picture](plays/2026-11-22-6/day4-ordered-tomorrow-still-day-8.png)) | Watched (Harbor's shortage is news on day 5; Mills warns on day 2 that a BPA call can be turned down; one player in six plays on this seed) | none |
| P1-18, P2-20 | Day 5 | "Add what you learned" found only inside the buy form | Watched (DESK-77, DESK-202) | none |
| P1-19, P2-21 | Day 5 | The order quoted "day 8 or 9" with no way to cut the risk; P2's order quote still warned "out 1 day" with its own one-day bridge in place | Watched (each quote judges the buy it prices; DESK-32 says what is already in place when the bridge is bought first) | none |
| P1-20 | Day 6 | Mission output at 98% with nothing saying why | Known (third play P2-8; DESK-208) | none |
| P2-22 | Days 6, 7 | The forklift request read "Coastal Rentals starts on day 8: 3 forklifts through day 8." on day 6 and "(and one other arrangement)" on day 7, with the purchase order from day 9 being written and then signed: "it looked as if I had forklifts for one day" ([picture](plays/2026-11-22-6/day6-forklifts-through-day-8-only.png)) | Defect (Sim words: DESK-188 names what takes over for running support only) | DESK-223 |
| F-1 | Day 8 | The day summary read "Coastal Rentals started material handling equipment at Hub 3 forklifts." ([picture](plays/2026-11-22-6/day8-started-at-hub-3-forklifts.png)); no player remarked on it | Defect (Sim words) | DESK-224 |
| P1-21 | Days 6 to 8 | The waste quote looked cheap, with no other supplier | Watched | none |
| P1-22 | Day 7 | What a COR catching a problem does for the player | Watched (fourth play P1-25) | none |
| P2-23 | Day 7 | A click on a "Who writes it" button missed because its label changed with the pick-two choice | Model (it clicked a stale label) | none |
| P2-24 | Day 8 | Hand-over from Novak to Avery clean; the quote's 8 to 16 hours against 16.5 after | Known (DESK-181) | none |
| P2-25 | Day 8 | A quote that warns of a missed day goes in with no confirmation | Watched (one player) | none |
| P2-26 | Day 9 | "1 alert" for the honey trucks after the order was placed | Watched (one player) | none |
| P2-27, P2-28 | Day 11 | Harbor "does not bring operators" only on the quote, not the vendor card; End this contract page clear | Watched | none |
| P1-24, P2-29 | Days 11 to 13 | Re-source greyed with its reason; the cure notice the only real choice | none (DESK-216, DESK-217 c seen again) | none |
| P1-25 | Days 9 to 16 | Long days that ask nothing; the two-step End the day | Known (D31) | none |
| P2-30, P2-31 | Day 15 | The office vehicle found only on day 15; a card buy quoted to start after the tour; cancelled cleanly | Watched | none |
| P1-26 | Debrief | "The best screen in the game", but "nothing during play told me" the day-1 card was right, and day 4 went on hunting a fix that did not exist | Watched (D56 put the judgement in the debrief; both players' day-4 quotes answered the question; if a third play loses the morning after a confusion day the same way, a decision) | none |
| P2-32, P2-33 | Debrief | Nothing about the two reversals; 33 walk-ins counted as a success; the first word "tutorial" | Watched; Known (first play) | none |

## The two rows

DESK-223: the second player covered forklifts the way the game allows and the quotes encourage, a one-day card bridge for day 8 and a written order from day 9, and on days 6 and 7 the request said only the bridge. DESK-188 already names what takes over for support that is running ("Coastal Rentals is delivering 3 forklifts through day 8; the purchase order from Coastal Rentals takes over on day 9." is what the player read on day 8), but not for support still to start, and not for an order still being written. The line now reads "Coastal Rentals starts on day 8: 3 forklifts through day 8; the purchase order with Coastal Rentals that MSgt Joan Avery is writing takes over on day 9." while it is written, and "...; the purchase order from Coastal Rentals takes over on day 9." once signed, in place of "(and one other arrangement)".

DESK-224: the day summary's start line runs the amount into the site, "at Hub 3 forklifts." It reads "Coastal Rentals started material handling equipment at Hub: 3 forklifts."

No decision is needed for either: both are words on a screen the decisions already cover.

## The four end answers

First brief: "The new contracting officer for a 300-person hub on the main island, with MSgt Avery (experienced) and SrA Novak (new), $15,000 cash and a purchase card." Trying to "keep the commander's four lines for 16 days". Managed it: "Yes. All four lines held: water 0 days out (one day at 50%), forklifts 0 out, latrines 0 missed, output 99%." One change: "Put the debrief's judgement into the game while I'm playing ... Also stop stating 'ordered tomorrow it still starts on day X' as a certainty when a vendor can fall through overnight. Without that, a first-timer can't tell a bad decision from bad luck until the game is over."

Second brief: "A first-time contracting officer at an island hub of 300 people, with 16 days on the ground." Trying to keep Col Ward's four lines. Managed it: "Yes ... I won mostly because the quotes tell you the start day and the risk of a missed day before you commit; they did most of the teaching." One change: "Put the commander's four lines and the hidden mechanics on screen from the first minute, and say it is a tutorial ... A close second: the 'Where things stand' summary should show all the cover you have arranged, not just the first buy." The close second is DESK-223.

The first player's change has two halves. The judgement in play is P1-26 (Watched). The "ordered tomorrow" sentence is a forecast like every quote; Harbor's shortage came as news on day 5 and Mills names the risk of a BPA call on day 2. One player in six plays of this seed read it as a promise; it stays as it is unless another does.

## Sim pass

`batch tutorial --seeds 200` on main at 7b6566a: identical to the fifth 22 Nov play's (DESK-221 and DESK-222 move words, not outcomes). Mission output median 99.7% textbook, 97.45% greedy, 79.6% do-nothing; textbook's worst seed 97.3%. No policy beats textbook on a pass line.
