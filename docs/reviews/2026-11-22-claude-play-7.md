# Claude play, 22 Nov 2026 (seventh)

The verification play due once the sixth 22 Nov play's rows landed: Fable's DESK-223 (a request names what takes over from a bridge, PR 517) and DESK-224 (the day summary's start line sets off its amount, PR 518) (PLAY-03, docs/PLAYTEST-CLAUDE.md section 2). Changing a COR and the other stop, hand over and end actions had still not all been through a play, so two fresh agents played side by side again: one with the section 3 brief, one with the section 3b second brief. Each had the driver and a notes file of its own; neither read the repository.

- Build: `main` at 1c42890 (PR 518 merged). Godot 4.6.1 .NET, 1280 by 800, Xvfb, Dummy audio.
- Seed: 20261122, the day's seed for the seventh time.
- How far: both to the debrief after day 16, never stuck, all four pass lines met with 0 days out everywhere; mission output 99.4% both. 198 and 233 commands (the first `look` of each log is the runner's).
- Logs: [commands, first brief](plays/2026-11-22-7/commands-1.txt), [input log](plays/2026-11-22-7/input-log-1.json); [commands, second brief](plays/2026-11-22-7/commands-2.txt), [input log](plays/2026-11-22-7/input-log-2.json). The harness replays both logs to every line met (`run tutorial --log`).
- Pictures: four of 431: [day 7, the forklift request naming the order Avery is writing](plays/2026-11-22-7/day7-avery-writing-takes-over-day-10.png), [day 8, "started material handling equipment at Hub: 3 forklifts."](plays/2026-11-22-7/day8-started-at-hub-colon-3-forklifts.png), [day 14, Payments' Designate COR: Lane "good" at 40%, Pratt "fair" at 65%, nothing under the button](plays/2026-11-22-7/day14-designate-cor-good-40-percent.png), [day 11, "everything was supplied in full" over a COR report of a missed delivery](plays/2026-11-22-7/day11-supplied-in-full-and-missed.png).

## What the merged rows did in play

- DESK-223: verified. The first player bridged forklifts with a two-day Coastal card buy (days 8 and 9) and a written order from day 10. On day 7 the request read "Coastal Rentals starts on day 8: 3 forklifts through day 9; the purchase order with Coastal Rentals that MSgt Joan Avery is writing takes over on day 10.", then "...; the purchase order from Coastal Rentals takes over on day 10." once signed, and "Coastal Rentals is delivering 3 forklifts through day 9; ..." while it ran. Nobody read the forklifts as covered for one or two days only: "Forklift bridge (card) and PO handoff on day 10 worked as planned."
- DESK-224: verified. Both day summaries read "Coastal Rentals started material handling equipment at Hub: 3 forklifts." (and "Island Mart started water at Hub: 4,500 gal/day.").
- Reached for the first time: changing a COR on a running contract. The second player swapped the waste contract's COR from Pratt to Lane on day 14 through Payments' Designate COR. It worked, and it is the finding of this play (P2-22 below).
- Used again: a hand-over (Novak to Avery, day 8), clean; the End this contract page read and not used (day 10).
- DESK-195, DESK-197 (on screen), DESK-199 and DESK-217 (a, d, e): still not reached in the game's screens. The second player's COR swap wrote DESK-197's record ("SSgt Lane designated COR on Main Island Sanitation waste removal at Hub, replacing TSgt Pratt (good: reports the same day, about 40% of problems on this contract).") but neither player opened the full record.

## The first two minutes

The same for both and the same as every play: preparation with two options ticked and all five fitting in the fourteen days; Mills's four turnover pages with the commander's lines on the last; then the desk with one water request whose lead says "Order today: a card buy from Island Mart". Known (sixth 21 Nov play notes 1, 2).

## The plays in one paragraph each

First brief: asked Ortiz and bought water on the card from Island Mart on day 1; joined the staff meeting itself on day 2 and sat down with Harbor's rep; water and food service at half on day 3; asked Dunn on day 4 and waited the night on the quote's "it still starts on day 8"; on day 5, with Harbor's call blocked, a two-day Coastal card bridge for days 8 and 9 and a fast-and-good Coastal purchase order from day 10 for 11 days (Pratt as COR), Avery writing; waste on a purchase order on day 7 (Pratt as COR); cure notice through Avery on day 13. Won with nothing out and 99.4%.

Second brief: asked Ortiz through Novak and bought the same card water on day 1; Novak into the staff meeting on day 2; on day 4 priced a written water replacement and kept the card ("Every day it would run is already covered."); on day 5 a fast-and-good Coastal purchase order from day 8 (Pratt as COR), Avery writing, with no card bridge possible (Coastal had one forklift left); waste on day 8 given to Novak, then handed to Avery when the quote said day 13 or 14 (change of mind 1); weighed ending Coastal for a cheaper Harbor call on day 10 and kept it; cure notice through Avery on day 13; on day 14 swapped the waste COR from Pratt to Lane (change of mind 2). Won with nothing out and 99.4%.

## Notes and how they sort

P1 is the first brief, P2 the second.

| Note | Where | What the player said | Sort | Row |
| --- | --- | --- | --- | --- |
| P1-1, P1-2, P1-3, P2-1 | Preparation | Two options ticked with no reason; all five fit, so no choice; nothing says "Tutorial"; who am I | Known (sixth 21 Nov play notes 1, 2) | none |
| P1-5, P1-7, P1-9, P2-2, P2-3 | Turnover | Host nation forklifts "for the whole deployment" beside a forklift BPA; goals only in the last page's prose; the jets | Known (third 21 Nov play P1-2, P1-3) | none |
| P1-8 | Turnover | The vendor paragraph as a wall of names | Watched (one player) | none |
| P1-12, P2-4 | Day 1 | "Open request" inside a card did nothing; the card is the button | Known (tenth play P1-5) | none |
| P1-13, P2-5 | Day 1 | The ask's answer comes tonight while the lead says order today | Watched (sixth play P1-10; D51 says what waiting costs where the answer is named) | none |
| P1-14, P2-6 | Day 1 | "done day after you pick two"; "Who writes it" labels change under the pointer | Known (DESK-180 b; sixth play P2-23) | none |
| P2-6, P2-14, P2-18 | Days 1, 5, 8 | The arrangement picker scrolls out of the Buy column after every change | Known (tenth play P1-5) | none |
| P1-16, P2-10 | Day 1 | The card's half day: short or out, and how likely | Watched (D56: a percentage would invite playing the roll) | none |
| P1-17 | Day 1 | A COR line cut off mid-sentence in the picker | Known (DESK-186) | none |
| P1-18 | Day 1 | The form resets to a blank one after the buy | Watched (sixth play P1-10) | none |
| P1-20, P2-11 | Day 1 night | Walk-in small buys first heard of at End the day; never counted on the desk | Known (DESK-149 c) | none |
| P1-21 | Day 1 night | Ortiz's answer told nothing new | Watched (asking is cheap; one player) | none |
| P1-23 | Day 2 | A click on Novak for the staff meeting did not stick with the card half scrolled | Known (tenth play P1-5) | none |
| P1-24 | Day 2 | People to reach is six scrolls down the Today pane | Watched (one player) | none |
| P1-25 | Day 2 | Spent two hours meeting Reyna after the screen said it "adds nothing to their card" | Model (it read the sentence and went anyway) | none |
| P1-27, P2-12 | Days 1 to 3 | "Nothing moves today" greyed and never explained | Watched (two players; the map plays a day's movements when there are any) | none |
| P1-30, P2-15 | Day 4 | "Ordered tomorrow, after the answer, it still starts on day 8" | none (DESK-218 seen again; both called it what they needed) | none |
| P1-31 | Day 4 | The request's parts sit below the buy form | Watched (one player) | none |
| P1-32 | Day 4 | Only Cancel on a misbehaving card buy | Known (DESK-205 says why there is no claim) | none |
| P1-33 | Day 4 | The tab strip moves and Back names change between pages | Watched (one player) | none |
| P1-34, P2-17 | Day 5 | "Add what you learned" found only inside the buy form | Watched (DESK-77, DESK-202; sixth play P1-18, P2-20) | none |
| P1-35 | Day 5 | Typing into Start day and Duration "silently did not take" | Model (the fields were scrolled out of the pane; the driver said "No visible type target ... Nothing changed." both times) | none |
| P1-36 | Day 5 | A two-day card quote with no "no written terms" warning | Watched (one player) | none |
| P1-37 | Day 5 | The request's "Who does it" list stays under a committed buy | Known (DESK-127, DESK-181) | none |
| P2-19 | Day 5 | A UCA quoted slower than a purchase order | Watched (the approval lag is on the quote; one player) | none |
| P1-38, P2-20 | Day 6 | Mission output at 98% with nothing saying why | Known (third play P2-8; DESK-208) | none |
| P1-39, P2-20 | Days 6, 7 | The staff meeting paid off: waste a day early | none | none |
| P1-40, P2-24 | Days 7, 8 | What a COR catching a problem does for the player; which matters more, odds or lag | Watched (fourth play P1-25) | none |
| P1-41 | Day 7 | The quote's 6.5 to 13 hours, then "about 13 hours" once committed | Watched (one player) | none |
| P2-21 | Day 8 | Hand-over from Novak to Avery clean; the request no longer says when the order starts | Watched (DESK-181; one player) | none |
| P2-25 | Debrief | The full record's "Avery took over ... 7 hours left" against the 6.5 hours the request showed | Defect (Sim words: the record rounds up to the hour, every screen to the half hour, DESK-14) | DESK-225 |
| P2-23 | Day 10 | Payments is where contracts and CORs live; nothing says so | Watched (one player) | none |
| P2-26 | Days 11, 12 | "Day 11: nothing finished and everything was supplied in full." over "COR report (SSgt Lane): Island Camp Services food service at Hub missed its delivery on day 11; the stock on hand covered it." ([picture](plays/2026-11-22-7/day11-supplied-in-full-and-missed.png)) | Defect (Sim words; the second player in two plays today, after the second play's P2-12) | DESK-226 |
| P1-43, P2-27 | Day 13 | "Open vendor decision" lands partway down the Inbox; Re-source greyed with its reason; the cure notice the only real choice | Known (DESK-216, DESK-217 c) | none |
| P2-22 | Day 14 | The Designate COR picker in Payments: Lane "good" at 40%, Pratt "fair" at 65%, with no fit words; two identical pickers in one list with "on this contract" naming no contract; no word that naming Lane lowers her watch on food service, the contract under a cure notice, where the buy form says so; the only result the word "designated" under the map ([picture](plays/2026-11-22-7/day14-designate-cor-good-40-percent.png)) | Defect (the buy form's COR choice was fixed by DESK-186 and DESK-112; Payments still reads the bare grade) | DESK-227 (Sim), DESK-228 (Codex) |
| P1-42, P1-44 | Days 8 to 16 | Long days that ask nothing after the cure notice | Known (D31) | none |
| P1-45, P2-28 | Debrief | The best screen; no verdict on the bridge, the meeting, the premium, the hand-over or the COR swap | Watched (sixth play P1-26, P2-32) | none |
| P1-46 | Debrief | "Confusion on the ground." as a bare line under water | Watched (one player) | none |
| P1-47, P2-29 | Debrief | 33 walk-in small buys counted only at the end | Known (first play) | none |

## The four rows

DESK-225: a hand-over's record says the hours left to the half hour, as the request page, the picker and the strip do since DESK-14. The second player's record read "7 hours left" where it had just read 6.5.

DESK-226: the day summary's first line does not say "everything was supplied in full" on a day the office was told of a missed delivery that nothing fell short for. It reads "Day 11: nothing finished and nothing was short, though one missed delivery was reported." The report says the rest, as now. The second 22 Nov play's P2-12 was watched for this; a second player has now read it as a contradiction.

DESK-227 and DESK-228: the second player's COR swap is the first time a play has changed a COR on a running contract. The buy form's COR choice says why each rate is what it is (DESK-186) and what naming a busy COR does to the contracts they already watch (DESK-112). Payments' Designate COR says neither: it leads with the COR's grade ("good"), so 40% beside "good" and 65% beside "fair" reads backwards, and it says nothing of food service, which Lane also watched and which was under a cure notice. DESK-227 gives the Sim's side: the COR's line on a contract leads with the fit words ("SSgt Lane · 3 FSS · does not work with waste removal: about 40% of problems on this contract, reported the same day"), and `SessionView.CorDesignationWhy` gives the buy form's sentence for a running contract, with what the change would lower. DESK-228 shows it under Payments' picker, names the contract on the picker, and says the result in a sentence.

No decision is needed: these are words on screens the decisions already cover.

## The four end answers

First brief: "The contracting officer for a 300-person hub on a main island, taking over from Capt Mills ... MSgt Avery (experienced and fast) and SrA Novak (new) ... a $15,000 cash box, a purchase card and a $576,000 O&M line." Trying to keep the four commander's lines for 16 days. Managed it: "Yes. All four lines held: water 0 days out, forklifts 0, latrines 0 missed, mission output 99%." One change: "Show every rule the moment it matters, inside the purchase quote. The quote was by far the best teaching tool in the game ... Alongside that, show the four goals as a pinned checklist from the first desk screen."

Second brief: "The new contracting officer at the main-island hub, about 300 people, running a three-person shop ... for 16 days." Trying to keep Col Ward's four lines. Managed it: "Yes, all four held." One change: "Show every quote's consequences, and especially the start day and the effect on other contracts, at the moment I commit or change something, wherever I do it ... The day-14 COR swap gave no warning about Lane's other contracts and no before-and-after numbers." That is DESK-227 and DESK-228.

Both one-change answers ask for the quote everywhere. Where a quote exists, both players trusted it and won by it; the screens that still lack one are the COR swap (DESK-228) and the hand-over's start day (P2-21, watched). The pinned goals are known (third 21 Nov play P1-3).

## Sim pass

`batch tutorial --seeds 200` on main at 1c42890: identical to the sixth 22 Nov play's (DESK-223 and DESK-224 move words, not outcomes). Mission output median 99.7% textbook, 97.45% greedy, 79.6% do-nothing; textbook's worst seed 97.3%. No policy beats textbook on a pass line.
