# Claude plays the tutorial, 20 Nov 2026

The day's play (PLAY-03, docs/PLAYTEST-CLAUDE.md). A fresh agent got only the brief in section 3 and the driver, plus a notes file of its own. It never read the repository.

- Build: `main` at 9c28818 (after DESK-111 to DESK-116: the order-by line and the vendor card take in what the shop has heard, the quote's warning carries "Add what you learned", one label for capacity), Godot 4.6.1 .NET, 1280 by 800, Xvfb, Dummy audio.
- Seed: 20261120.
- How far: the morning of day 8, in 151 commands. It stopped there, stuck, and the tutorial could not be won from that state (note 20).
- Logs: [commands](plays/2026-11-20/commands.txt) (the number is the screenshot it produced) and [input log](plays/2026-11-20/input-log.json). The harness replays the log to the same game (`run tutorial --log`, state hash a2d94e89).
- Pictures: three of the 152 the play took, all from the same game.

## The result, from the replay

Played on to the end with nothing more done, the replay fails one pass line: waste removal at Hub is down days 13 to 16. Water, forklifts and mission output (99.7%) are met. Prep was all five choices (9 of 14 days). Water went on the card to Island Mart on day 1. The shop joined the staff meeting on day 3. On day 4 the player asked SMSgt Dunn and placed a BPA call with Harbor Equipment, handed to Avery from the team panel. Dunn's answer that night said the rental needs drivers, and Harbor's shortage was in the news. On day 5 it added the operators, put Coastal Rentals on the card from day 8, and stopped Harbor's call before it was signed. Capt Ruiz brought the waste request on day 7; the player asked her and built a purchase order with Main Island Sanitation, and from then on every buy of anything was refused.

This is the first play to use Stop this work (DESK-105), and it found the defect behind note 20 at once.

## The first two minutes

The preparation page again read as a list of good things to tick: all five fit in 9 of 14 days (D32). The turnover's four pages were read in full; the pass lines were found on the last page and again as the commander's bar on Today, which the player said settles them. On day 1 the water request's order-by line told it what to do and it did it. The buy form cleared after Arrange support with no word of what had just been bought (the decision is listed lower on the request page and on Today); the player said it would be easy to buy twice.

## Its notes, sorted

The sort follows section 5: defect, design, known, model. A row named here is new in this PR unless it says otherwise.

| # | Day, screen | What the player said (shortened) | Sort | Row |
| --- | --- | --- | --- | --- |
| 1 | Prep | All five fit in 9 of 14 days; not a real choice | Known: D32 | none |
| 2 | Prep | The fourth choice is cut off and its label reads empty in the screen text | Model: the driver prints only fully visible words; the box is whole after a scroll | none |
| 3 | Prep | Not told how to win until the turnover's last page | Known: D28, the bar on Today | none |
| 4 | Turnover | Page 2 says the host nation supplies forklifts; page 3 says a BPA with Harbor does. Which? | Model, mostly: the host nation now, the BPA as the backup; the day 3 news settles it, as the tutorial means it to | none |
| 5 | Day 1, request | Bought before Ortiz's answer came; did not know if waiting mattered | Known: D25 | none |
| 6 | Day 1, request | Picking Island Mart greyed the SF 44, so the two routes could not be compared side by side | Model: the supplier list says who takes cash; no row | none |
| 7 | Day 1, request | No micro-purchase threshold anywhere; is $9,734 under it by design? | Model: the card limit shows in the refusal when a buy is over it | none |
| 8 | Day 1, request | After Arrange support the form reset with no summary of what was bought | Design, small: the decision is on the request page below the fold and on Today; noted for Nick's eye, no row | none |
| 9 | Day 1, Today | "1 open request" after the water was arranged; "Open request →" reads as a state | Model, mostly: the state line under it says what is arranged | none |
| 10 | Day 2 | The plan said Island Mart starts water today; the map button said "Nothing moves today" | Model: a card vendor delivers itself; the map shows shop trips and barges | none |
| 11 | Day 3 | The staff meeting cost 2 hours and gave "Nothing for contracting" | Known: it paid on day 6, as Mills said it would | none |
| 12 | Day 4, strip | "1 of 8 h assigned" before anything was done; nothing said it was the daily meeting | Known: D23 (the meeting is an hour a day); the day's plan names it ("Staff meeting: 1 hour today") | none |
| 13 | Day 4, request | No way to say who writes the buy on the buy form; found the hand-over by clicking Avery's name, "by luck" | Defect, game, against D17 ("wherever work is assigned (any buy, ...)"): the buy form has no picker for who writes it, though the Sim takes `AwardAction.Assignee` and `AssigneeChoices` covers buys | DESK-121 |
| 14 | Day 4, team panel and request | The picker said Avery's job "takes them 7 hours"; her queue then said 6.5; the request page said "then about 7 hours of work" above "then about 6.5 hours of work" ([picture](plays/2026-11-20/day4-seven-and-six-and-a-half.png)) | Defect, Sim views: the request's standing and the hand-over picker round a teammate's hours up to the hour, the team view and the decision line to the half hour (DESK-14) | DESK-120 |
| 15 | Day 4 | Finance certifies on day 5, so nothing can start sooner | Model: true and intended | none |
| 16 | Day 5, request | The fix is "Add what you learned" in the left column; the quote on the right showed no warning, and the left column's "The quote flags this part" was false when read | Design, the second most important: the warning and its button sit under the quote's figures, below the fold of the buy column (DESK-113 as built), while the summary pinned above Arrange support, the one paragraph always in view, said nothing of the drivers | D40; DESK-119 |
| 17 | Day 5, request | Coastal's purchase order quote said "It is on top of the BPA call being written with Harbor Equipment Co. Every day it would run is already covered.", in the warning colour ([picture](plays/2026-11-20/day5-already-covered.png)) | Defect in the Sim view, part of 16: the summary called the right vendor a double buy of the wrong one; both leave out the drivers until the player adds them, and the summary did not say so | D40; DESK-119 |
| 18 | Day 5, request | Pick two has no word on what each changes | Known: the trade sentence (DESK-26) is in the quote, under the figures, below the fold of the buy column; the same place as note 16 | none |
| 19 | Day 5, request | Card for a 13-day service with drivers, after Mills said services want a written contract; no warning on the card quote | Known: D35 as built; the line names the card with "no written terms". The debrief says what it cost | none |
| 20 | Day 7 and 8, request | Every buy, for any request, refused with "Harbor Equipment Co material handling equipment at Hub already exists" ([picture](plays/2026-11-20/day8-blocked.png)); tried five routes, the own-initiative list and a day's wait | Defect, the most important, and the player's one change. The buy form names each buy `player-award-N`, skipping names in use by an arrangement or by work still in progress; the Sim refuses a name used by any buy's work, finished or stopped. The stopped Harbor call was `player-award-2`, so every later quote reused that name and was refused, with a sentence that names the stopped call | DESK-117, DESK-118 |
| 21 | Day 7 | "Buy on your own initiative" offers a vehicle for the office; nothing had said so | Known: D5, DESK-29; a choice, not a lesson | none |
| 22 | Day 7 | The first COR report, on lodging, read clearly | Praise, earned | none |

## Its four answers, in its words

- **Who were you?** "A government-contracting person new to this game, playing the Tutorial as the incoming contracting officer at the Hub, with MSgt Avery (fast) and SrA Novak (new) under me."
- **What were you trying to do?** "Keep the commander's bar for 16 days: water and forklifts never out more than a day, latrines pumped every day, 95% mission output."
- **Did you manage it?** "Partly, then no. Water was covered on day 1 by card. Forklifts were covered from day 8: I learned the BPA vendor couldn't supply operators, stopped that call, and moved to the backup vendor. Waste removal was impossible. Every way of buying it was blocked by an unrelated error, apparently left over from the BPA call I had stopped. The tutorial can't be finished from that state."
- **One change?** "Fix the blocker. A stopped, never-signed BPA call for forklifts must not block an unrelated waste-removal purchase. Any time Arrange support is greyed, the reason should name the actual problem on this buy and how to fix it. Second, close behind: put 'Add what you learned' and 'hand this to a teammate' next to the quote and the Arrange button, instead of burying them in the left pane and behind a click on a teammate's name."

## Fable's reading

1. **A stopped buy breaks every buy after it.** Stop this work (DESK-105, 18 Nov) leaves the stopped work in the Sim's record with its buy's name, as it should: the decisions list and the debrief name it. The buy form picks the next free name by asking the Sim whether a name is in an arrangement or in work still in progress, which a stopped buy is not; the Sim's award check refuses any name used by a buy's work at all. The two disagree only after a stop, and then every buy in the game is refused for the rest of the run. The tutorial teaches the stop on day 5 (a BPA call without drivers, from a vendor short of forklifts), so any player who learns the lesson loses the waste request on day 8. The refusal names the stopped call, which reads as a contradiction. Fable builds the Sim half this run (DESK-117: one view call that answers exactly what the award check asks); Codex's half is one line in the buy form and a smoke test that stops a buy and arranges another (DESK-118). Until DESK-118 merges, Nick should not stop a buy in a play he cares about.
2. **The advice is right, and still off screen.** D39's warning and button under the quote are there, below the quote's figures in a column that scrolls; the paragraph always in view above Arrange support called Coastal's purchase order a double buy of Harbor's call. D40 puts the part someone said is needed into that paragraph.
3. **Who writes the buy is chosen where it is bought.** D17 says so; the buy form never got the picker. DESK-121.

## Sim pass

`batch tutorial --seeds 200`, medians (min to max). Unchanged from 19 Nov in every column: DESK-111 to DESK-116 are read only, and no policy stops work, which is also why no batch could have found note 20.

| Policy | Mission output | Forklifts down days | Waste down days | Water down days | Food down days | Confusion days |
| --- | --- | --- | --- | --- | --- | --- |
| do-nothing | 0.796 (0.778 to 0.796) | 9 | 4 | 6 (6 to 7) | 6 (6 to 8) | 0 |
| greedy | 0.975 (0.943 to 1) | 1 (0 to 2) | 0 (0 to 1) | 0 (0 to 1) | 0 (0 to 3) | 1 (0 to 6) |
| textbook | 0.997 (0.973 to 1) | 0 | 0 | 0 (0 to 1) | 0 (0 to 2) | 1 (0 to 4) |

- Textbook meets every pass line in every seed and beats greedy on mission output.
- The policies never stop work and never reuse a name, so DESK-117's test replays this play's log instead.
