# Claude plays the tutorial, 16 Nov 2026

The first daily play (PLAY-03, docs/PLAYTEST-CLAUDE.md). A fresh agent got only the brief in section 3 and the driver, and never read the repository.

- Build: `main` at e669382 (PR 267), Godot 4.6.1 .NET, 1280 by 800, Xvfb, Dummy audio.
- Seed: 20261116.
- How far: the end of day 20 and the debrief, in 351 commands. It was never stuck for ten steps; the worst was five, on the preparation screen.
- Logs: [commands](plays/2026-11-16/commands.txt) (the number is the screenshot it produced) and [input log](plays/2026-11-16/input-log.json). The harness replays the log to the same game (`run tutorial --log`, state hash 70bb7111).
- Pictures: four of the 352 the play took, all from the same game.

## The result, from the replay

Every pass criterion was met. Mission output was 99%. Prep was cards, cash and a paying agent, system access, existing vehicles, vendor lists and partner agreements (14 of 14 days). Water came from Island Mart on the card from day 3. The forklifts went to a BPA call with Harbor Equipment on day 4, which Harbor Equipment declined on day 5. They then went on a written purchase order to Coastal Rentals on day 6 and ran from day 8. Waste removal went on a written order to Main Island Sanitation from day 13.

The gaps were lodging at 50% on day 4 (an inherited vendor failure, unforeseeable) and forklifts at 50% on days 17 and 19, with Coastal Rentals' equipment down "no operators". The debrief tags those two "Avoidable".

## The first two minutes

The driver opens on the preparation list, already inside New game, so the player first went Back to the title and started again. That is the driver's doing, not the game's.

On the preparation list it ticked items until it had planned 14 of 14 days. At that point "Your team", "Back" and "I'm ready to leave" vanished, and the list said there was nothing more below ([picture](plays/2026-11-16/prep-buttons-gone.png)). Scrolling, Escape and clicking by name did nothing. It got out only by ticking a fifteenth day. That brought the buttons back with "I'm ready to leave" greyed, under the line "prep choices use 15 prep-days; the scenario allows 14". It then unticked one item and left.

The turnover and the first request read clearly. The first decision ("Go to market" or "Buy on your own initiative") did not.

## Its notes, sorted

The sort follows section 5: defect, design, known, model. A row named here is new in this PR unless it says otherwise.

| # | Day, screen | What the player said (shortened) | Sort | Row |
| --- | --- | --- | --- | --- |
| 1 | Prep | At 14 of 14 days the footer buttons vanished and nothing scrolled; stuck five steps | Defect. Seen in its text file: the footer is gone and the scroll reports nothing below | DESK-62 |
| 2 | Prep | A fifteenth tick was allowed and answered with "prep choices use 15 prep-days; the scenario allows 14" | Defect. A raw validation string; an item that does not fit should be greyed with its reason | DESK-62 |
| 3 | Prep | The last ticked item had white text on a pale background | Defect: the pressed item loses its frame ([picture](plays/2026-11-16/prep-buttons-gone.png), the templates row) | DESK-62 |
| 4 | Prep | Nothing says what each choice does later | Known: D7 and the prep briefing; the debrief names what a choice cost | none |
| 5 | Turnover | Mills says "ask me anything" and there is no way to ask; the speech switches to a narrator | Defect, scenario text | DESK-60 |
| 6 | Turnover | Calling 4,500 gal a day for a week "small" | Model. The card buy came to $9,734, under the threshold, which is Mills' point | none |
| 7 | Day 1, Today | "Decisions · 0 unresolved" while a request waits | Design: the count only counts your decisions | DESK-57 |
| 8 | Day 1, Requests | "Buy on your own initiative" against "Go to market" was unclear | Design: the labels do not say what each is for | DESK-63 |
| 9 | Day 1, Requests | The ask and the vendor meeting went on its own hours; handing work over was found by accident | Known: D17 (who is free where work is handed out); the hand-over worked | none |
| 10 | Day 1, editor | The card buy asked for day 2 and starts day 3; day 2 was never mentioned again | Design. Water was full on day 2: the site holds two days of stock. Nothing on screen says the stock covered the late start, so a correct day reads as a hidden gap | DESK-60 |
| 11 | Day 1, editor | After Arrange support the form stayed open and showed the new buy as a red refusal; the only confirmation was "bought" in small type | Defect. On day 4 the same: "Harbor Equipment Co has 2 forklifts left on those days, after 3 already committed" for its own call, and "reassigned" as the status line ([picture](plays/2026-11-16/after-arrange.png)) | DESK-63 |
| 12 | Day 1, editor | Bottled water warned "Without plant operators, it fails more often" and "Without maintenance" | Defect in the Sim. Island Mart sells bottled water; it offers no operators or maintenance, yet the buy carries both failure risks and both warnings. That taught the player to ignore the warning that later mattered | DESK-59 (D26) |
| 13 | Day 1, editor | Island Mart showed "(unavailable)" with no reason the second time | Known: DESK-32 refusals say what to do; the picker label does not carry the reason yet | DESK-63 |
| 14 | Day 1 and 9, asks | Ortiz and Ruiz both said "Nothing I can add"; asking looked worthless | Design. Both answers are right (nothing was missing), but they teach the opposite of D3 just before the one ask that mattered (Dunn on the forklifts) | DESK-58 (D25) |
| 15 | Day 3 on, Today | "Find finance" sat at the foot of every Today page | Defect: the button belongs to People to reach and stays pinned while that section is closed | DESK-64 |
| 16 | Day 3, Vendors | "Not known yet" on vendor cards after two prep days on vendor lists | Known: vendor cards fill at a meeting (D10); prep lists bring vetting, not quotes | none |
| 17 | Day 4, editor | "Vehicle: Choose a vehicle…" meant the BPA, beside a "vehicle for the contracting office" | Defect, label | DESK-63 |
| 18 | Day 4, editor | Pick two of fast, cheap and good on a call against set BPA prices; fast added 15% | Design: pick two was written for open-market buys | DESK-65 (D27) |
| 19 | Day 4, plan | "One more job is not ready until a later day" with an empty queue and no reason | Defect in the Sim view: the call waits for finance to certify the funds on day 5, which the request's state line says but the day's plan does not | DESK-60 |
| 20 | Day 5, Today | Lodging at 50%; "Open" only lit the map; there was nothing to do and nothing said so | Known: D18 says why a shortfall is short; it does not yet say "nothing for you to do" when the vendor is fixing it | DESK-57 |
| 21 | Day 5 and 6, results | Harbor Equipment declined the call; day 5 said "done by MSgt Joan Avery" and "everything was supplied in full"; found on day 6 at the foot of the request | Defect, the most important one. The Sim records the decline on day 5; the results page drops it ([picture](plays/2026-11-16/day5-declined-hidden.png)) | DESK-57 |
| 22 | Day 6, plan | Right after the purchase order, the plan said nothing was arranged or in work for forklifts | Defect in the Sim view, confirmed in its text file: the order waits for finance to certify the funds, and "Still open" counts waiting work as nothing (the same on day 10 for waste removal) | DESK-60 |
| 23 | Day 9, request | "The shop was not in the room" at the staff meeting; it never found how to attend | Design: attending is in People to reach, behind a picker; the request that blames you for missing it does not say where it is | DESK-64 |
| 24 | Day 17 to 20, Today | "supply 1 forklifts against demand 3" at 50%; "Nobody has told you why yet" | Defect: the count rounds 1.5 down and does not match the percent; "1 forklifts" | DESK-60 |
| 25 | Day 19, editor | A backup card buy quoted "earliest start day 20–21" with no hours | Model. One day's notice from day 19 is day 20 | none |
| 26 | Day 20, results | Opened at 50% yet "everything was supplied in full" | Model. Day 19 was short; day 20 was restored to 100% | none |
| 27 | Debrief | The forklift gap "Avoidable: what you needed to know was in front of you", though the order form never offered operators | Design, and the most important one after 21. Sgt Dunn knew nobody was licensed (the request's `knownBy`); the drivers are added by asking him (D3). The editor said "Without operators" ([picture](plays/2026-11-16/operators-no-route.png)) but not who to ask, and the debrief calls an unasked question "in front of you" | DESK-58 (D25) |
| 28 | Debrief | "Two more gaps" and three entries | Model. The sentence names the worst gap and then "two more" | none |
| 29 | Debrief | "days 17–17" | Defect, label | DESK-63 |

## Its four answers, in its words

- **Who were you?** "A contracting officer new to this game, taking over the shop at a 300-person hub, with two staff, $15,000 in cash, purchase cards and $576,000 in O&M money."
- **What were you trying to do?** "Keep the hub supplied: water while the water point was down, three forklifts when the host nation took theirs back, and honey trucks when the host nation pulled theirs, using the right kind of buy for each."
- **Did you manage it?** "Mostly." It names the forklifts at a third on days 17, 19 and 20 with no way to add operators, and two lost days after the declined call was hidden behind "done… supplied in full". It also names water on day 2, which was not a gap (note 10).
- **One change?** "Make the end-of-day report tell the truth and say what to do about it. A declined call, a start that slipped past the day it was wanted, or a shortfall should appear as a red 'needs you' item on Today that opens the request with the actions to fix it."

## Fable's reading

The player understood the job, won the scenario, and was misled three times by the game, never by the scenario.

1. The results page hid a declined call (note 21).
2. The editor attached warnings to a purchase where they cannot apply (note 12).
3. The debrief blamed it for a question nothing pointed it toward (note 27).

These three are the game teaching the wrong lesson, which is worse than being unclear. They go first. The preparation screen (note 1) is the first thing every player meets, and its stuck state is the second most urgent fix for Codex.

## Sim pass

`batch tutorial --seeds 200`, medians (min to max):

| Policy | Mission output | Forklifts down days | Waste down days | Water down days | Shop hours |
| --- | --- | --- | --- | --- | --- |
| do-nothing | 0.787 (0.774 to 0.787) | 13 | 8 | 6 | 21 |
| greedy | 0.983 (0.947 to 1) | 0 (0 to 2) | 0 (0 to 1) | 0 (0 to 2) | 37 |
| textbook | 0.956 (0.922 to 0.962) | 3 (3 to 3) | 2 (1 to 4) | 0 (0 to 2) | 81 |

- Greedy beats textbook on mission output in the tutorial.
- Textbook misses two of the tutorial's own pass criteria in every seed: forklifts are out three days, and waste removal is out one to four days.
- No policy calls the BPA, so declined calls (`work.bpaDeclines`) are 0 for all three. The path the player took on day 4 is the one no policy measures.

All of this is DESK-61.
