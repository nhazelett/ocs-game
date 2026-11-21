# Claude plays the tutorial, 21 Nov 2026, second play (DESK-98, DESK-136, DESK-138, DESK-140, DESK-141)

The play due after the 21 Nov 02:45 run merged Codex's DESK-98 (Add to this contract, PR 405), DESK-136 (Cancel this card buy, PR 406), DESK-140 (PR 412, tests only), DESK-141 (one verdict per debrief gap, PR 413) and DESK-138 (four small screen points, PR 414) (PLAY-03, docs/PLAYTEST-CLAUDE.md section 2). Add to this contract and Cancel this card buy are actions no play had used, so two fresh agents played: one with the section 3 brief, one with the section 3b brief (change your mind twice). Neither read the repository.

- Build: `main` at 97344b0 (after PRs 405, 406, 412, 413 and 414; the play ran on the same tree before PR 413's merge commit). Godot 4.6.1 .NET, 1280 by 800, Xvfb, Dummy audio.
- Seed: 20261121 for both.
- How far: both to the debrief after day 16, never stuck, all four pass lines met (water 0 days out, forklifts 0, waste 0, mission output 100%). First player 184 commands, second 196.
- Logs: [first player's commands](plays/2026-11-21-2/commands-1.txt) and [input log](plays/2026-11-21-2/input-log-1.json); [second player's commands](plays/2026-11-21-2/commands-2.txt) and [input log](plays/2026-11-21-2/input-log-2.json). The number is the screenshot each command produced. The harness replays both logs to the same games (`run tutorial --log`, every line met; state hashes 035a869b and 573a154a).
- Pictures: three of 381.

## What the merged rows did in play

- DESK-141: the debrief's one gap (waste removal at Hub, day 15) showed one verdict, "Unforeseeable", and no second tag. Verified.
- DESK-138: Today's heading counted the dining hall decision on day 13; the vendor card said "Starts 0 days after a signed order"; Peak demand read "3 forklifts". The new buy-column receipt ("Arranged: ...") is the play's main new defect: it is a snapshot, so after the player handed the BPA call to Avery it still said "given to you", and it repeats "Arranged:" ([picture](plays/2026-11-21-2/day4-arranged-given-to-you.png)). DESK-147.
- DESK-98 and DESK-136: not reached. The second player changed its mind twice (handed the BPA call to Avery; stopped it and bought Coastal Rentals with drivers on the card instead) through the team screen's Stop this work, because the BPA call was still being written. Nothing either player bought ever needed a part added or a card buy cancelled. Both rows stand on their smoke tests; the second brief does not steer a player to them, and it should not.

## The first two minutes

Both players ticked all five preparation choices (9 of 14 days; known, every play), read the turnover's four pages and copied the pass lines by hand. Both asked Ortiz and bought water on the card on day 1, and both said the answer and Mills's card-or-cash note came after the buy (DESK-144, next).

## Their notes, sorted

P1 is the first player, P2 the second. Notes are shortened.

| # | Player, day, screen | What the player said | Sort | Row |
| --- | --- | --- | --- | --- |
| 1 | P1 19, P2 17, 18, 30; day 4 and day 7 buy forms | The main finding, and both players' one change. The buy said "8 hours of your time" and offered no way to choose who does it; both found the hand-over only on the team screen, after committing, every time. The Who writes it picker (DESK-121) is on the form, but under Contract monitor and its reasons, below the fold, while the summary and Arrange support are pinned in view ([picture](plays/2026-11-21-2/day4-who-writes-it-below.png)) | Design: D45, what the pinned summary asks for sits beside it | DESK-148 |
| 2 | P1 23, P2 23; day 5 buy form | "Add what you learned first", says the summary; the button is at the bottom of the quote, three scrolls down | Design: D45 | DESK-148 |
| 3 | P1 20, 32, P2 21; days 4 and 7 buy column | After the hand-over the buy column still said "Arranged: Day 4: ... given to you" | Defect, game (DESK-138's receipt is a snapshot) | DESK-147 |
| 4 | P2 34; day 13 Inbox | After the cure notice went to Avery, the line under the greyed "Cure notice in work" said "4 hours of your time" ([picture](plays/2026-11-21-2/day13-cure-notice-your-time.png)) | Defect, game | DESK-147 |
| 5 | P1 10, P2 5, 19, 34; days 1, 4, 13 | Novak's card said 1.5 hours, the line under Ask said 2 | Known | DESK-142 |
| 6 | P1 9, 15, 22, 30, P2 6, 10; days 1, 4, 7 | Answers and Mills's advice arrived the night after the buy they were about; the buy said the answer "may change what this buy needs" and nothing said when | Known (D44) | DESK-144 |
| 7 | P1 40, 41, P2 35; days 15, 16 | Waste removal at 50% on day 15 while the bar said "out 0 days ... Met so far" | Known | DESK-145 (a) |
| 8 | P1 45, P2 39; debrief | "Mission output 100%" then "first dipped on day 15" | Known | DESK-145 (b) |
| 9 | P1 46, P2 40; debrief | The day 15 failure is "Unforeseeable", its report "due day 17"; the monitor the player chose (Pratt, 80%, two days late) made no visible difference, so the one tense moment taught nothing | Design, no decision needed: the debrief links a report that arrived too late to the monitor's reporting lag, and says what the faster monitor would have shown | DESK-149 (b) |
| 10 | P1 44, P2 41; debrief | No "you won" or "Tutorial complete"; four Met lines and Play again | Design, no decision needed: the debrief's first line says the tutorial is done and whether the commander's lines held | DESK-149 (a) |
| 11 | P1 14, P2 9, 28; day 1 plan | "Idle hours go to walk-in small buys": good, bad or neutral? (the third play in a row to ask) | Design, no decision needed: the plan says once what walk-in buys are for | DESK-149 (c) |
| 12 | P1 25, P2 23; day 5 quote | Coastal Rentals cost the same $8,580 with or without drivers: "were operators free?" | Design, no decision needed: Coastal's price includes its drivers; the quote says so when the part added costs nothing more | DESK-149 (d) |
| 13 | P1 12, P2 16; vendor card | "Starts 0 days after a signed order" against a quote of day 2 or 3 | Known | DESK-145 (d) |
| 14 | P1 36, P2 33; days 11, 12 | Dining hall short twice and no action until the decision the next morning | Known | DESK-145 (f) |
| 15 | P1 38; day 13 | "Open vendor decision" opened the Inbox scrolled past its context | Known | DESK-146 (b) |
| 16 | P1 39; day 13 report | "Cure notice the performance issue ..." | Known | DESK-145 (f) |
| 17 | P2 2; preparation | The fourth choice read as an empty button until scrolled | Known | DESK-146 (e) |
| 18 | P1 1, 2, 4, P2 1; preparation | Nothing says Tutorial; all five choices fit | Known (every play); listed | none |
| 19 | P1 5, P2 3; turnover | The pass lines are prose on the fourth page | Known: D38's bar shows them every morning; listed | none |
| 20 | P1 6, P2 4; turnover | The host nation provides the forklifts, yet there is a forklift BPA | Model: the turnover is right; the host nation pulls them after day 7, which is the lesson | none |
| 21 | P1 35, P2 12; map | The map never changed and "Nothing moves today" was grey | Model: one island, no lanes in the tutorial (as 18 Nov) | none |
| 22 | P2 25; day 5 team screen | Stop this work acted at once with no confirmation | Model: nothing had been spent and the buy can be arranged again; ending a contract asks | none |
| 23 | P1 8, P2 14, 27; request and vendor panes | Open request missed after a scroll; a half-hidden picker could not be clicked by name; the vendor picker scrolls away | Model: the driver clicks only fully visible labels | none |
| 24 | P1 24, 29, P2 7, 31; pick two | The trade sentence appears only once two are picked | Known: DESK-124; listed | none |
| 25 | P1 21, 26, P2 20, 26; monitor | "COR" in the notes, "Contract monitor" on the form; which contracts Lane watches; how bad execution confusion is | Known: listed in earlier plays; no change | none |
| 26 | P1 18, 28, P2 13, 15, 29; staff meeting | The clearest cause and effect in the game; "best 2 hours I spent" | Praise, earned (D35) | none |
| 27 | P2 38; debrief | "Traces each line back to who ordered what and when. The best screen in the game" | Praise, earned (D38) | none |

## Their four answers

First player:
- **Who?** "A contracting officer who knows government contracting but had never seen this game", running a three-person shop at a 300-person hub for 16 days.
- **What?** Keep the commander's four lines met while the host nation's support fell away.
- **Managed?** "Yes, all four were Met ... But the win came from my own contracting sense more than from the game teaching me": an order placed before the answer arrived, the hidden Add what you learned, the buried hand-off.
- **One change?** "Before every purchase, show one plain checklist on the buy screen": what was learned but not added, whether an answer is pending and the last safe day to wait, and who will do the work, "with a picker right there."

Second player:
- **Who?** The new contracting officer for the Hub, with Avery (experienced, fast) and Novak (new, slow).
- **What?** Meet the commander's bar over 16 days.
- **Managed?** Yes, all four met; the one blemish, a 50% day on day 15, "the game called it unforeseeable, and it gave me no way to respond."
- **One change?** "Let me choose who does the work on the buy form itself, and have the time and who's doing it text agree everywhere."

## Fable's read

1. **What the pinned summary asks for is not beside it (notes 1, 2).** DESK-121 put Who writes it on the buy form "above the summary", but the summary and Arrange support are pinned at the foot of the column while the picker scrolls with the quote, so the summary ("8 hours of your time", "add what you learned first") is always in view and the two controls it names are not: they sit in the scrolling quote under the pickers and the monitor's reasons. Both players committed every written buy to themselves and moved it on the team screen afterwards. D45 puts both controls in the pinned footer, above the summary, only when they apply.
2. **The new receipt is a snapshot (notes 3, 4).** DESK-138 (a) shows the committed decision's words as they were at commit, so a hand-over makes it wrong; it should read the decision as it stands, like the decision list on the request page does, without a second "Arranged:".
3. **The ending teaches nothing when the one thing that goes wrong comes too late to answer (notes 9, 10).** Both plays (same seed as this morning's play) ended on a day 15 failure whose report was due day 17. That is fair play, not a defect, but the debrief can make the monitor choice visible: the report came too late because of the two-day lag the player chose, and Lane would have reported it the same day.
4. Answers arriving after the choice is again both players' most frequent note; DESK-144 is Fable's next row.

## Sim pass

`batch tutorial --seeds 200`: mission output textbook 0.973/0.997/1, greedy 0.943/0.9745/1, do-nothing 0.778/0.796/0.796 (min, median, max). The same as this morning's play: nothing merged since changed the Sim.
