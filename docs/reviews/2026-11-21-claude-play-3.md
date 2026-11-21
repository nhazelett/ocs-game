# Claude plays the tutorial, 21 Nov 2026, third play (DESK-142, DESK-144, DESK-145, DESK-146, DESK-149)

The verification play due after the 21 Nov runs merged Codex's DESK-142 (a person's hours to the half hour, PR 417), DESK-146 (five small screen points, PR 420) and DESK-143 (CI only, PR 419), and Fable's DESK-144 (answers and advice before the choice, PR 416), DESK-145 (six Sim sentences, PR 418), DESK-149 (four Sim sentences, PR 422) and DESK-134 (a UCA has its reason, PR 423; Spoke B only, nothing in the tutorial) (PLAY-03, docs/PLAYTEST-CLAUDE.md section 2). One fresh agent played with the section 3 brief, the driver and a notes file of its own; it did not read the repository. The second brief was not needed: no stopping, handing-over or ending action changed.

- Build: `main` at 21b870b (PR 423's merge; the play ran on its head, 5ea9dcd, the same tree). Godot 4.6.1 .NET, 1280 by 800, Xvfb, Dummy audio.
- Seed: 20261121, as the two earlier plays today.
- How far: to the debrief after day 16, never stuck, all four pass lines met (water 0 days out, forklifts 0, waste 0 with one day at 50%, mission output 100%). 166 commands.
- Logs: [commands](plays/2026-11-21-3/commands.txt) (the number is the screenshot each command produced) and [input log](plays/2026-11-21-3/input-log.json). The harness replays the log to the same game (`run tutorial --log`, every line met, state hash 6455658a).
- Pictures: two of 167.

## What the merged rows did in play

- DESK-144: the ask about water said its answer "comes in tonight, after you end the day", and Mills's card-or-cash and staff-meeting notes arrived before the choices they are about; the player joined the meeting on day 2 and heard about the forklifts early. Verified.
- DESK-145: "A day at 50% counts as short, not out.", "Nobody has told you why yet; TSgt Pratt's report is due on day 17." and "mission output never dipped" all showed where meant. Verified, though the player wanted the first of them before it happened (note 28).
- DESK-142: "6.5 hours", "5.5 hours of SrA Eli Novak's time" on the strip, the pickers and the cost lines. Verified.
- DESK-146: (b) Open vendor decision opens the Inbox on the decision, but its heading lands at the bottom edge of the pane and the choices are three scrolls below ([picture](plays/2026-11-21-3/day13-decision-at-the-bottom.png)): not fixed in play (DESK-153). (e) The driver now drops unreadable controls, but names the buy form's three number fields "Name" (DESK-154). (a), (c), (d) not reached.
- DESK-149: (c) the walk-in sentence showed on day 1's plan and was read and understood. (b) showed in the debrief and reads as a contradiction beside "Unforeseeable: nobody could have known." (note 29, DESK-155 a). (a) waits for Codex's DESK-152. (d) not reached: the player added no part.

## The first two minutes

The player ticked every preparation choice (9 of 14 days; known, every play), read the turnover's four pages as a wall of text it could not reread, found the commander's lines at the end of the last page, then found them again, labelled, on Today's bar ("good"). It asked Ortiz about water and bought on the card on day 1 before the answer came, which the quote had said would happen.

## Their notes, sorted

Notes are shortened; the step is the screenshot.

| # | Day, screen, step | What the player said | Sort | Row |
| --- | --- | --- | --- | --- |
| 1 | Preparation, 2 | Two choices pre-ticked with no reason; nothing says what the tutorial teaches; "SCO" never spelled out | Known (preparation has no trade-off at 14 days, every play) | none |
| 2 | Preparation, 3 to 6 | Five choices fit 14 days, so no trade-off; "1.25 times as long as on the system" assumes I know the base | Known | none |
| 3 | Turnover, 7 to 9 | A wall of text; nothing says it can be read again | Known (D28: the bar on Today carries what matters) | none |
| 4 | Turnover, 8 and 9 | Page 2 says the host nation provides forklifts for the whole deployment, page 3 names a forklift BPA; "honey trucks" unexplained | Model and by design: the last rotation believed the host nation; it withdraws on day 3 | none |
| 5 | Turnover, 10 | The win conditions are offhand dialogue on the last page | Known (D28: Today's bar labels them) | none |
| 6 | Today and Requests, 14, 17 | "Open request" and the request title did not respond; only the whole card did | Model: the card is one button whose label wraps | none |
| 7 | Water request, 18 to 22 | The answer comes overnight, so I bought first; the quote warned | Known, by design (D42, D44) | none |
| 8 | Buy pickers, 23 to 30 | Arrangement before supplier feels backwards | Known (D36, DESK-90 order) | none |
| 9 | Day plan, 35 | The walk-in sentence was the first mention of the backlog; its size is never shown | DESK-149 (c) verified; the size is left out on purpose (no administration game) | none |
| 10 | Night 1, 37 | Ortiz's answer added nothing; nothing said where to join the staff meeting | Model (the button is on Today under People to reach) | none |
| 11 | Vendors, 38 to 52 | Meet the vendor costs 2 hours while "Their terms are already known" | Design, watched: one play, no row yet | none |
| 12 | Vendors, 55 | The supplier picker scrolls away with the pane | Model | none |
| 13 | Night 2, 57 | The staff meeting paid off at once with early word of the forklifts | Praise, earned (D44) | none |
| 14 | Day 3, 58 and 61 | "1 of 8 h assigned" with no task: the daily meeting hour; joining never said it costs an hour every day | Defect: the join line says only "2 hours of your time" | DESK-155 (b) |
| 15 | Day 3, 63 and 64 | The forklift withdrawal matched the meeting's early word | Praise | none |
| 16 | Day 4, forklift request, 66 to 70 | "Starts 3 days after it goes in" against the card's "1 day after a signed order" | Known | DESK-150 |
| 17 | Day 4, Team, 71 to 73 | Handing work over is buried in the Team screen | Known (D45) | DESK-148 |
| 18 | Day 4, request page, 74 | Left pane: Avery has it, 6.5 hours; buy column: "given to you", 8 hours | Known | DESK-147 |
| 19 | Days 4 and 5, 77 to 84 | The news said Harbor was short of forklifts; the request page did not flag the call; Coastal's quote saying "on top of" the call was "excellent" | Design, watched; praise for DESK-110 | none |
| 20 | Days 5 and 6, 87 to 91 | Coastal starts "1 or 2 days" after ordering, its card says "0 days"; Avery's hours on the refused call were wasted | Known (DESK-150); the refusal is the BPA lesson Mills named | DESK-150 |
| 21 | Day 7, waste order, 94 to 114 | Fast, cheap and good explain themselves only after a pair is picked; the COR trade text was "excellent" | Design, no decision needed: before any pick, the trade line names all three pairs | DESK-155 (d) |
| 22 | Day 7, buy column, 106 to 108 | Clicking Cheap did nothing: the toggles were gone from view after the pinned summary grew to six lines ([picture](plays/2026-11-21-3/day7-summary-hides-pick-two.png)) | Design: D46 | DESK-156 |
| 23 | Day 7, buy column, 109 | The number fields read "Name" #1 to #3 | Defect, driver | DESK-154 |
| 24 | Day 8, Today, 121 to 132 | About ten scrolls to reach the sites; the water point's return on day 9 shown only in the plan | Known (Today's length, DESK-108) | none |
| 25 | Days 8 and 9, 133 to 138 | Everything took over on time | Praise | none |
| 26 | Days 10 to 12, 140 to 145 | Skip ahead says where it stops; Lane, the COR I turned down, reported the dining hall | Praise, earned | none |
| 27 | Day 13, Inbox, 146 to 152 | Open vendor decision lands partway; the choices are three scrolls down; Terminate only through the contract page; "4 hours of your time" after giving it to Novak | Defect for the landing (DESK-146 b not met); the cost line did change to Novak's 5.5 hours once he was picked (step 151), so that part is Model; Terminate through the contract page is by design | DESK-153 |
| 28 | Days 14 to 16, 157 to 161 | The rule that a 50% day is short, not out, first appeared after it happened | Design, no decision needed: the commander's words say once, on the turnover's last page, what counts as out | DESK-155 (c) |
| 29 | Debrief, 163 to 166 | "Pratt reports problems 2 days late; Lane reports the same day" beside "Unforeseeable: nobody could have known." contradicts itself | Defect in DESK-149 (b)'s words: the lag is about when you would have heard, not whether it could be foreseen | DESK-155 (a) |
| 30 | Debrief, 166 | Stopped at the end | none | none |

## Their four answers

- Who: the incoming contracting officer for a 300-person hub on a host-nation-supported island, with two NCOs, $15,000 of cash, a card, a $576,000 O&M line and two inherited BPAs.
- What: water and forklifts out no more than a day each, latrines never missed, mission output at 95% or more, for 16 days while the host nation pulled support.
- Did it: yes, all four.
- One change: "tell me each rule at the moment it applies, on the action itself": the daily meeting hour, the walk-in backlog, what fast, cheap and good each mean before I pick, and that a 50% day is short, not out.

## Fable's read

1. **The pinned summary is crowding out the form (note 22).** On a written buy with an ask out and a late end to the quote, the summary pinned above Arrange support ran to six lines, and the scrolling part of the buy column shrank until pick two was out of reach. D45 (DESK-148) is about to put more in that footer. D46 caps the summary at three sentences; the rest stays in the quote above.
2. **Three rules arrived after the moment they mattered (notes 14, 21, 28),** which is the player's one change. Each is a sentence the Sim already knows: DESK-155.
3. **DESK-149 (b) reads wrong (note 29).** The verdict is about the failure; the lag is about when the office hears. DESK-155 (a) says it that way.
4. **DESK-146 (b) is not met in play (note 27).** The decision is in view, at the bottom edge. DESK-153.

## Sim pass

`batch tutorial --seeds 200` on this build: textbook mission output 97.3% to 100% (median 99.7%), water out at most 1 day, forklifts and waste never out; greedy 94.3% to 100% with forklifts out up to 2 days; do-nothing 77.8% to 79.6%. Textbook beats both on every pass line; no measure stands still.
