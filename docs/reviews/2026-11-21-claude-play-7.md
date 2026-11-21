# Claude play, 21 Nov 2026 (seventh)

The play due after the 21 Nov 12:45 run merged Codex's DESK-148 (the writer pinned beside the buy summary, PR 442), DESK-162 (a hand-over waiting for a pick is not red, PR 443), DESK-163 (Open site details opens them, PR 444), DESK-164 (stop the other buy on the request page, PR 445) and DESK-167 (what a vendor meeting brings, PR 449), and Fable's DESK-168 (the debrief names a cancelled buy, PR 450) (PLAY-03, docs/PLAYTEST-CLAUDE.md section 2). DESK-162 and DESK-164 change the hand-over and stop actions, so two fresh agents played side by side: one with the section 3 brief, one with the section 3b second brief (change your mind twice). Each had the driver and a notes file of its own; neither read the repository.

- Build: `fable/DESK-168` at 8a34479, the tree that merged as PR 450 onto `main` at edaf644. Godot 4.6.1 .NET, 1280 by 800, Xvfb, Dummy audio.
- Seed: 20261121, as the earlier plays today.
- How far: both to the debrief after day 16, never stuck, all four pass lines met (water 0 days out, forklifts 0, waste 0, mission output 100%). 193 and 305 commands.
- Logs: [commands, first brief](plays/2026-11-21-7/commands-1.txt), [input log](plays/2026-11-21-7/input-log-1.json); [commands, second brief](plays/2026-11-21-7/commands-2.txt), [input log](plays/2026-11-21-7/input-log-2.json). The harness replays both logs to every line met (`run tutorial --log`, state hashes 2eee0169 and d7ee366b).
- Pictures: four of 500: [day 5, stop on the request page](plays/2026-11-21-7/day5-stop-on-request-page.png), [day 8, the hand-over says done day 8](plays/2026-11-21-7/day8-handover-done-day-8.png), [day 10, the summary says only "already covered"](plays/2026-11-21-7/day10-summary-already-covered.png), [day 15, "Termination" of a card buy](plays/2026-11-21-7/day15-cancel-termination-in-work.png).

## What the merged rows did in play

- DESK-148: both players chose the writer in the pinned footer and read the summary beside Arrange support; the second player decided its day-10 replacement from that summary alone (note P2-14, below). Verified, and it raises the stakes of what the summary says.
- DESK-162: the second player handed the waste order to itself and back on day 8; both hand-overs worked first time and nothing read as an error. Verified.
- DESK-163: the first player clicked Today's "Open site details" on day 16 and the map's callout for waste removal opened. Verified.
- DESK-164: on day 5 the first player bought Coastal on the card and stopped Avery's Harbor BPA call with the Stop this work beside it on the same request page. "Clean." Verified.
- DESK-167: both players read Harbor's "Meet Mr. Tomas Reyna" as adding nothing, since the terms are on file, and skipped it; the second said it "saved me 2 hours". Verified.
- DESK-168: not reached. The second player cancelled a card buy, but on the day its replacement started, so no day went short.

## The first two minutes

The same for both: preparation with two options ticked, Mills's four turnover pages, then the desk with one water request. Both found the commander's four lines only on the fourth page and both checked every preparation option because nothing said what an unused day is worth (Known, the sixth play's notes 1, 2, 4).

## The plays in one paragraph each

First brief: water on the card on day 1, the staff meeting from day 2, the Harbor BPA call on day 4 with an ask of Dunn the same hour; Dunn's answer and the alert on day 5 led it to add operators, buy Coastal on the card and stop the call on the request page. Waste on a written purchase order (fast and good, Pratt as monitor) on day 7, a cure notice to Island Camp Services on day 13, nothing to do on days 9 to 12. Second brief: the same opening, but it let Avery's call go in on day 5 and lost it to the decline, bought Coastal with operators on day 6, and then changed its mind twice: on day 8 it handed the waste order from Avery to itself and back, because two screens disagreed about when it would be done; on day 10 it ordered a written purchase order from Harbor for days 13 to 20 to replace the card buy without written terms, and cancelled the card buy on day 15 when Harbor started. The Harbor order came without operators, which nothing on the screen it read said; Harbor's equipment was down for want of operators on its first day, day 15, while the cancelled card buy still covered it, and the line held.

## Notes, sorted

P1 is the first brief, P2 the second.

| # | Day, screen | What the player said | Sort | Row |
| --- | --- | --- | --- | --- |
| P1-1, P2-1 | Prep | Two options ticked; unused days worth nothing, so take everything | Known (sixth play, notes 1, 2) | none |
| P1-2, P2-2 | Turnover 4 | The four lines buried in prose | Known (DESK-70 puts them on Today; both found them there) | none |
| P1-3, P2-3 | Turnover 2, 3 | Host nation has the forklifts, yet a BPA for them | Known (the lesson: the host nation withdraws) | none |
| P1-4 | Day 1, request | "Who does it" did not look like a choice until highlighted | Watched | none |
| P1-5, P2-5 | Day 1, buy | Pickers and boxes scrolled out of the Buy column; clicks missed | Model (the driver; the pinned footer holds the writer, DESK-148) | none |
| P1-6, P2-6 | Day 1, buy | "Pick two of fast, cheap and good below." with the boxes above | Known | DESK-169 (b) |
| P1-7, P2-7 | Day 1 | An ask is answered only that night, so asking first costs a day | Known (the lesson; the summary says when the answer comes, D42) | none |
| P1-8, P2-8 | Day 1, plan | Walk-in small buys first heard of in the day's plan | Known (sixth play, note 10) | none |
| P1-9 | Day 1, plan | Two buttons labelled End the day, one over the other | Watched (no player has misclicked it) | none |
| P2-9 | Day 2, 3 | "Every day it would run is already covered" on a buy on its own initiative; is a duplicate penalised? | Watched (it only costs money; the sentence is the warning) | none |
| P1-10 | Day 3 | "Nothing moves today" greyed all game | Known (sixth play, note 16) | none |
| P1-11 | Day 4, buy | Does a BPA call need a monitor? | Watched | none |
| P2-11 | Day 5, quote | Coastal's card quote counted the Harbor call still being written as cover while Harbor was "short this week" | Known (an award in work counts as cover, D33; the short week holding for any buy is DESK-174) | DESK-174 |
| P1-12 | Day 5 | Had to add what Dunn said to the request itself | Known (D34, the lesson) | none |
| P1-13 | Day 5, request | Stopped Avery's call beside the new buy | Watched (DESK-164 worked) | none |
| P1-14, P2-12 | Day 7, 10, buy | The monitor picker lists names only; catch rate and report lag appear in the quote below after one is picked | Defect (game: the Sim's `CorSummary` is there to put in each choice) | DESK-177 (a) |
| P1-15 | Day 7, request | "Starts 3 to 5 days after it goes in" and the quote "starts on day 13" | Model (the quote waits for the day it is wanted; both are true) | none |
| P2-13 | Day 8, team | Hand-over to itself: "You: 7 hours free today ... This takes you 8 hours; it would be done day 8." The day's plan then said 7 hours today and 1 left. It handed the task back. ([picture](plays/2026-11-21-7/day8-handover-done-day-8.png)) | Defect (Sim: the hand-over preview counts all 8 of the day's hours, not the 7 left after the standing staff meeting) | DESK-176 |
| P2-14 | Day 10, buy | The replacement purchase order with Harbor: the pinned summary said "Every day it would run is already covered." and nothing of operators, which the request includes and Harbor does not bring. Harbor's equipment was down for want of operators on its first day, day 15; the card buy still covered it. ([picture](plays/2026-11-21-7/day10-summary-already-covered.png)) | Design (D49): the summary names a part the request includes that the buy leaves out, ahead of "already covered" | DESK-175 |
| P2-15 | Day 10, request | No cancel for the running card buy on the request page; found it under Payments | Known | DESK-173 |
| P2-16 | Day 10, quote | Coastal could not take a written order overlapping its own card buy ("1 forklift left") | Watched (true: its capacity) | none |
| P2-17 | Day 10, quote | Naming Pratt here lowers his watch on the waste contract: good, but deep in the quote | Watched (with DESK-177 a the choice says it) | none |
| P1-16 | Days 9 to 12 | Nothing to do for four days | Known (the tutorial's quiet days; Skip ahead, DESK-75) | none |
| P1-17 | Day 10, Payments | The inherited BPA calls total more than the O&M line | Known (DESK-91, the inherited sentence) | none |
| P1-18 | Header | "2 requests" while Requests lists 3 | Watched (first time noted) | none |
| P1-19, P2-18 | Day 13 | Food service is not on the bar, so the cure notice's stakes were unclear | Watched (a decision with no bar line is the lesson's quiet one) | none |
| P2-19 | Day 15, contract page | After Cancel this card buy with Novak, the page said "Termination of Coastal Rentals ... is already in work" in red, the result "termination in work", and Who does it went back to You with "1 hour of your time" under the greyed button. ([picture](plays/2026-11-21-7/day15-cancel-termination-in-work.png)) | Defect: the Sim's two words fixed in this PR ("cancelling the card buy for ... is already in work", "cancelling the card buy in work"); the page showing You is the game's | fixed here; DESK-177 (b) |
| P2-20 | Day 15, cancel | The confirm does not say whether anything covers the days after | Watched (the Harbor order started that day; the request page said so) | none |
| P1-20, P2-21 | Days 15, 16 | Waste at 50% on day 15, Pratt's report due day 17, nothing to do | Known (the first play of 21 Nov, note 9; DESK-145 says once what counts as out) | none |
| P1-21, P2-22 | Day 16, quote | A cash top-up from Main Island Sanitation could not start before day 18; the card says 1 day after a signed order | Known | DESK-169 (a) |
| P1-22 | Debrief | The day 16 results dialog over the debrief behind it | Watched | none |
| P2-23 | Debrief | The forklift line shows Harbor "with maintenance": no operators | Watched (true, and the debrief says it) | DESK-175 |

## The players' end answers (in their words, shortened)

First brief:

- Who: a contracting officer new to the game, with Avery (fast) and Novak (new), for a 300-person hub.
- Trying to: hold the commander's four lines for sixteen days as the host nation's support fell away.
- Managed it: yes, all four; "I was never stuck. The good parts were real: the quotes state consequences in commander's-bar terms, greyed options explain themselves, and the hints from Mills and the staff meeting paid off."
- One change: "Put the rules on screen before day 1 as a short checklist": the four lines and what counts as out, that an ask is answered overnight, that you add what you learned yourself, that idle hours go to walk-in buys.

Second brief:

- Who: the same.
- Trying to: the same, changing its mind twice.
- Managed it: yes, "but it was closer than the score looks": the 50% waste day it could not answer, "my replacement forklift order apparently came without the operators the requester had said were required, and nothing warned me", and a first change of mind that "achieved nothing because the screens disagreed about the timing."
- One change: "Make each request's page the one place to act on everything tied to it": every buy with its stop or cancel, each quote checked against what the requester said is needed, each COR's report speed in the picker, one finish day that matches the day's plan. "Most of my confusion and nearly all my risk came from facts that were true somewhere in the game but not shown where I was deciding."

## What Fable makes of it

1. **The pinned summary is now what the player reads, so it must carry the part a buy leaves out (P2-14).** DESK-148 worked: the second player decided its replacement from the footer without scrolling the quote. The quote's note that Harbor brings forklifts without operators was above, unseen; the summary said "Every day it would run is already covered", true and beside the point. D49 (DESK-175) puts the left-out part first. It is the same rule as D40 (told, not added) and D48 (not yet confirmed) for the third case: added to the request, and the vendor cannot bring it.
2. **Two numbers for one hand-over (P2-13).** The picker said done day 8; the day's plan said one hour carries to day 9. With a zero-miss waste line the player undid its own decision. DESK-176 makes the picker count the hours already planned today.
3. **Ending a card buy still speaks of termination (P2-19).** The Sim's words are fixed here; the contract page keeps the person who has the cancel in Who does it (DESK-177 b).
4. **The monitor choice is made blind (P1-14, P2-12).** Both players picked a monitor before seeing what each one catches. DESK-177 (a) puts it in the choice.

The first player's one change (a rules checklist before day 1) is Known: Today shows the four lines from day 1 (DESK-70), and each of the other three facts is said where it applies (D42, D34, the day's plan). Left out: a rules page before play; the tutorial teaches each where it bites.

## Sim pass

`batch tutorial --seeds 200` on `main` after PR 450: textbook mission output 97.3% to 100% (median 99.7%), water out at most 1 day, forklifts and waste never out; greedy 94.3% to 100%; do-nothing 77.8% to 79.6%. Unchanged from the sixth play in every column (PR 450 and this PR change words only); textbook beats both on every pass line.
