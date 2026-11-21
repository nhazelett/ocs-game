# Claude play, 21 Nov 2026 (sixth)

The play due after the 21 Nov 10:45 run merged Codex's DESK-147 (what was arranged reads as it stands now, PR 441) and Fable's DESK-165 (the debrief closes the threads the player started, PR 446) and DESK-166 (what meeting a vendor brings, PR 447, not yet bound by the game) (PLAY-03, docs/PLAYTEST-CLAUDE.md section 2). One fresh agent played with the section 3 brief, the driver and a notes file of its own; it did not read the repository. The second brief was not needed: no stopping, handing-over or ending action changed since the last play (DESK-147 changes only what a hand-over reads afterwards).

- Build: `main` at 06bca5c (PR 447 merged; the play ran on its head, d919353, the same tree). Godot 4.6.1 .NET, 1280 by 800, Xvfb, Dummy audio.
- Seed: 20261121, as the earlier plays today.
- How far: to the debrief after day 16, never stuck, all four pass lines met (water 0 days out, forklifts 1 of the 1 day allowed, waste 0, mission output 98.4%). 256 commands.
- Logs: [commands](plays/2026-11-21-6/commands.txt) and [input log](plays/2026-11-21-6/input-log.json). The harness replays the log to every line met (`run tutorial --log`, state hash 32dd1e02).
- Pictures: three of 257: [day 6, the pinned summary says nothing of operators](plays/2026-11-21-6/day6-summary-leaves-out-nothing.png), [day 16, the map reports a fuel failure that Today does not](plays/2026-11-21-6/day16-fuel-label-nothing-new.png), [the debrief](plays/2026-11-21-6/debrief-closing-threads.png).

## What the merged rows did in play

- DESK-147 (b): the in-work cure notice read "MSgt Joan Avery has this task, about 3.5 hours of work left." The player gave it to Avery and moved on. Verified. (a) was not reached: the player chose the writer before committing every time, so no receipt changed hands.
- DESK-165 (b), (c): the debrief's paragraph closed with "Your cure notice to Island Camp Services runs to day 23; food service at Hub was never short after it. Spare shop hours went to 33 walk-in small buys for CE; none waited long enough for CE to go to the commander." The player asked about neither, as the fifth player had. Verified. (a) was not reached: no buy picked fast and good failed.
- DESK-166 is not bound yet (Codex DESK-167): the player again asked what a two-hour meeting with a vendor whose terms are known would buy (note 15).

## The first two minutes

Preparation with two options already ticked, then Capt Mills's four turnover pages, then the desk on day 1 with one water request. The player found the commander's four lines only as prose on the fourth page and wrote them down itself (DESK-70 put them on Today; it found them there later).

## The play in one paragraph

The player bought water on the card on day 1, joined the staff meeting on day 2 and heard about forklifts there, called the Harbor BPA on day 4 with Avery writing, and lost it to the declined call on day 5, as Mills warned. On day 6 it asked SMSgt Dunn and, in the same hour, bought three Coastal forklifts on the card to lock the day-8 start, though the quote said to ask about operators first. Dunn's answer that night said the rental needs drivers. On day 7 it added operators, found that Coastal now had only one forklift left after its own buy, cancelled the first buy under Payments and bought one with operators, then two more on day 8 for day 9. Day 8 was out, all the line allows. The rest held; a camp vendor's misses got a cure notice on day 13.

## Notes, sorted

| # | Day, screen | What the player said | Sort | Row |
| --- | --- | --- | --- | --- |
| 1 | Prep | Two options already ticked; was that advice? | Watched (the scenario's defaults; no one else asked) | none |
| 2 | Prep | Unused days are worth nothing, so take everything | Known (left out on purpose in the prep design) | none |
| 3 | Turnover 2, 3 | "Billeting and dining are calls on our camp BPA", then a Harbor BPA | Model (the text never says "only") | none |
| 4 | Turnover 4 | The commander's lines only as prose | Known (DESK-70 shows them on Today) | none |
| 5 | Day 1, Today | Open request vanished after a scroll | Model (driver; the Where things stand line worked) | none |
| 6 | Day 1, buy | Pickers scroll off when reading the quote | Known (DESK-148 pins the summary and writer) | none |
| 7 | Day 1, buy | "Pick two of fast, cheap and good below." while the boxes are above | Defect (since the summary was pinned, the reason sits under the boxes) | DESK-169 |
| 8 | Day 1, buy | The quote named the consequence against the commander's limit | Watched (worked) | none |
| 9 | Day 1, ask | "Informs this request" does not say what you might learn | Known (the answer is the lesson; D25) | none |
| 10 | Day 1, plan | Walk-in small buys first heard of in the plan | Known (M2R; the plan's sentence says what they are) | none |
| 11 | Day 1, night | Mills teaches through a news card that scrolls away | Known (Mills's notes stay the teaching, section 27) | none |
| 12 | Day 2, Today | Does it matter who joins the staff meeting? | Watched | none |
| 13 | Day 2, night | The staff meeting paid off at once | Watched (worked) | none |
| 14 | Day 3 | The turnover said forklift support was for the deployment | Known (the lesson) | none |
| 15 | Day 2, Vendors | Meeting a vendor whose terms are known: what do 2 hours buy? | Known (DESK-166 built; DESK-167 binds it) | DESK-167 |
| 16 | Day 3, map | "Nothing moves today" greyed while water started | Model (nothing crosses water; the button is about boats) | none |
| 17 | Day 4, buy | Monitor trade-off clear; does a BPA call need one? | Watched | none |
| 18 | Day 4, buy | Writer list two scrolls down; Arrange ready before a pick | Known (DESK-148 pins the writer beside the button) | none |
| 19 | Day 5, quote | "2 forklifts left after 3 already committed" counted its own call | Model (true: its own call was in work) | none |
| 20 | Day 6, buy | Abandoned BPA draft still in the form | Watched (drafts are kept on purpose, DESK-63) | none |
| 21 | Day 6, buy | Request says a card buy starts 1 or 2 days after it goes in; the supplier detail says "Starts 0 days after a signed order"; the quote says day 8 | Defect (the vendor's notice line in the buy column is a third number; DESK-145 d explained it and it still confused) | DESK-169 |
| 22 | Day 6, buy | Harbor declined the call as short this week, then offered the same forklifts on the card for day 8 | Defect (the tutorial's authored shortage holds only for the call) | DESK-174 |
| 23 | Day 6, buy | Bought before Dunn answered; the pinned summary said nothing of operators | Design (D48): the summary names a part at risk the buy leaves out while nobody has answered | DESK-172 |
| 24 | Day 7, request | Did not know a buy covers only the parts the request lists when it goes in | Design (D48, the same row) | DESK-172 |
| 25 | Day 7, quote | "Coastal Rentals' price already includes its operators" read as: any buy from them brought operators | Defect (Sim words; fixed in this PR: "Coastal Rentals charges the same with its operators as without, and an order brings only the parts it names.") | fixed here |
| 26 | Day 7, cancel | Cancelling a card buy is five screens away, under Payments | Defect (the request page names the buy and gives no way to it) | DESK-173 |
| 27 | Day 7, Payments | The desk tabs did nothing while a contract page was open | Defect | DESK-173 |
| 28 | Day 7, waste | Asked Ruiz first, learned nothing to add | Watched (the lesson taken) | none |
| 29 | Day 8, quote | Finance's day moved from 8 to 9 by waiting a day | Model (the quote is for an order placed today) | none |
| 30 | Day 8, buy | The confirmation did not say whether the monitor stuck | Watched (the decision names the COR on its contract page) | none |
| 31 | Day 10 | Skip ahead appeared without introduction | Watched | none |
| 32 | Day 13, Inbox | Terminate is not on the decision card | Known (DESK-21: only on the contract's own page) | none |
| 33 | Day 13 | The no-written-terms warning came true, half a day | Watched (worked) | none |
| 34 | Day 16, map | "Hub · Fuel: supplier failure reported", Today "0 alerts, nothing new" | Defect (Sim: a covered failure by the host nation is not in New since yesterday) | DESK-170 |
| 35 | Day 16, map | Clicking the label selected Power at Hub, not fuel | Defect (game) | DESK-171 |
| 36 | Debrief | "Sized for 1 forklifts against demand of 3" named the wrong cause; the cancelled buy is not in it or its evidence | Defect (Sim: the cancelled buy and the unit's count) | DESK-168 |
| 37 | Debrief | The two no-written-terms days avoidable: fair | Watched | none |

## The player's end answers (in its words, shortened)

- Who: a contracting officer new to the game, running a three-person shop for a 300-person hub.
- Trying to: hold the commander's four lines for sixteen days through a host-nation withdrawal, a declined BPA call and a camp vendor's misses.
- Managed it: yes, with no margin on forklifts, "and that was my fault. I bought before asking the requester."
- One change: "Before the first buy, tell the player plainly, on the Buy form itself, that a buy covers only the parts the request lists right now. Show those parts next to the Arrange button, with any part the requester hasn't confirmed flagged."

## What Fable makes of it

1. **The lesson landed, and the screen nearly hid why (notes 23 to 25).** The tutorial teaches by letting the player buy before asking, and it did. But at the moment of the buy the pinned summary said only price and start; the warning sat in the quote above it, and the next day "already includes its operators" told the player the opposite of the lesson. The words are fixed here; D48 puts the part at risk into the summary while nobody has answered (DESK-172). Left out: a parts list beside the button (the request page already has it, and the summary is capped at three sentences, D46).
2. **Undoing a signed buy is far from where the player decided (notes 26, 27).** DESK-164 gives an unsigned buy its stop on the request page; a signed card buy not yet started still needs the contract page. The request's decision line links there (DESK-173). Left out: cancelling from the request page itself (DESK-21 keeps ending a contract on its own page).
3. **The debrief named the wrong cause (note 36).** It said the buy was too small; the player cancelled the right-sized buy because it left out operators and could rebuy only one. DESK-168.
4. **Two smaller truths:** Harbor's authored shortage should hold for any buy that week (DESK-174); a host-nation failure the stock covered should be in New since yesterday as the map already says (DESK-170, DESK-171).

## Sim pass

`batch tutorial --seeds 200` on this PR's tree: textbook mission output 97.3% to 100% (median 99.7%), water out at most 1 day, forklifts and waste never out; greedy 94.3% to 100%; do-nothing 77.8% to 79.6%. Unchanged from the fifth play in every column (DESK-165, DESK-166 and this PR change words only); textbook beats both on every pass line.
