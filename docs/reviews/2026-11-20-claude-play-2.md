# Claude plays the tutorial, 20 Nov 2026 (second play: the verification play)

The verification play after DESK-117 to DESK-121 (PLAY-03, docs/PLAYTEST-CLAUDE.md section 2: a fix for a defect the morning's play found, and a change on screen no play had seen). Two fresh agents played at once, each with only its brief and the driver and a notes file of its own; neither read the repository. Player 1 got the section 3 brief. Player 2 got it with the section 3b sentence added (change your mind at least twice and use what the game offers to stop, hand over or replace).

- Build: `main` at 320581d (after DESK-118: the buy form never reuses a stopped buy's name; and DESK-121: Who writes it on the buy form), Godot 4.6.1 .NET, 1280 by 800, Xvfb, Dummy audio.
- Seed: 20261120 for both.
- How far: both played to the debrief on day 16 and met every pass line (water, forklifts and waste 0 days out; mission output 99.7%). Player 1 took 194 commands, player 2 took 219. Neither was stuck for ten steps.
- Logs: commands ([player 1](plays/2026-11-20-2/commands-play1.txt), [player 2](plays/2026-11-20-2/commands-play2.txt); the number is the screenshot it produced) and input logs ([1](plays/2026-11-20-2/input-log-play1.json), [2](plays/2026-11-20-2/input-log-play2.json)). The harness replays both to the same games (`run tutorial --log`; player 1's state hash 252e5d46, player 2's 3a6a1890).
- Pictures: two of player 2's 219.

## The fix, verified

The morning's play lost the tutorial on day 7 because a stopped buy blocked every buy after it (DESK-117, DESK-118). Player 2 stopped Avery's BPA call with Harbor Equipment on day 5 ([picture](plays/2026-11-20-2/day5-stopped.png): "Stopped on day 5 before it was signed; nothing was bought."), arranged a written purchase order with Coastal Rentals the same morning ([picture](plays/2026-11-20-2/day5-replacement-accepted.png)), and bought waste removal on day 7. Every buy after the stop was accepted. Its first change of mind, handing the staff meeting from itself to Novak on day 3, also worked as it expected. Player 1 found Who writes it on the day 7 buy form (DESK-121) and asked why every quote does not have it.

## The first two minutes

Both players read the preparation page as a list to tick: all five choices fit in 9 of 14 days. Both read the turnover's four pages in full and found the pass lines on the last page. On day 1 the water request's order-by line told them what to do and they did it. Player 1 took two days to learn that the names in the top strip open the team page, where work is handed out. Player 2 opened the cheaper water vendor on the Vendors tab on day 2 and felt the order-by line had railroaded it to the card.

## Their notes, sorted

The sort follows section 5: defect, design, known, model. A row named here is new in this PR unless it says otherwise. P1 and P2 are the two players.

| # | Who, day, screen | What the player said (shortened) | Sort | Row |
| --- | --- | --- | --- | --- |
| 1 | P2 day 4 and 5; P1 day 7 | Bought while the ask about the same request was still out; nothing said an answer was coming that might change the buy. P2: "Warn me when I'm about to buy while a question about that request is still unanswered." P1: waiting a day for each answer costs lead time | Design, the most important: D42 | DESK-123 |
| 2 | P1 day 6 and 8 | Fast, cheap and good have no word on what each does; the explanation is two scrolls below the toggles. Also: a contract vehicle picker with one entry still has to be picked | Defect, game, the second time (20 Nov morning play note 18 called it known): the trade sentence belongs under the toggles | DESK-124 |
| 3 | P2 day 2; P1 day 9 | Payments says "Being arranged" for water that starts that morning (Island Mart on day 2; the host nation water point on day 9), while Today says Island Mart "is delivering". P1 had to dig in Payments to learn whether day 9 was covered | Defect, game: the Payments status names the start day | DESK-125 |
| 4 | P1 and P2 day 12 | The Inbox says "No performance issues reported" beside a COR report that Island Camp Services missed deliveries on days 11 and 12; P2 saw only "End this contract" on the contract page until the issue opened that night | Defect, game, for the Inbox line; the timing of the issue is known (DESK-83, D29) | DESK-125 |
| 5 | P2 day 7 | The decision reads "3,600 gal for 8 days" where the request says 3,600 gal a day | Defect, Sim text: waste is continuous with a volume unit, so the rate drops "/day" | DESK-126 |
| 6 | P1 day 3 and 7; P2 day 3 | Handing out work: found only by clicking a name in the strip; the staff meeting button always lands on You; "why isn't Who writes it on every quote?" | Design, under D17 ("wherever work is assigned"): asks and the staff meeting take the same picker as the buy form | DESK-127 |
| 7 | P2 day 2 to 16; P1 day 9 | Mills says twice the cash box is the thing to watch; neither player spent a cent of cash | Defect, scenario text: true only for cash buys; the line says so | DESK-128 |
| 8 | P2 day 2 | Ending a $9,734 card buy costs 10 hours, as much as ending a contract | Known: DESK-94 (Fable's next Sim depth rows) | none |
| 9 | P1 and P2 days 13 to 16 | Nothing happens in the last days | Known: D31; the length is set by the forklift lesson (DESK-80) | none |
| 10 | P1 debrief | The debrief says nothing about a 13-day service with drivers on the card | Model, mostly: $8,580 is under the card limit, and the debrief reports what it cost; Coastal Rentals did not fail | none |
| 11 | P1 day 7 | Novak is not on the Who writes it list | Model: he is the third choice, below the fold of the buy column (the scroll bar shows more); the summary is pinned under the column | none |
| 12 | P1 and P2, prep, team page, toggles, picker | Buttons with empty labels in the screen text | Model: the driver prints only words fully in view | none |
| 13 | P1 day 13 | The cure notice text names terminate but no button does | Model: the player corrected it; the button was below the fold | none |
| 14 | P2 day 1 | The quote said "day 2 or 3", the request then said "starts on day 2" | Model: the start is drawn when the buy goes in | none |
| 15 | P2 day 4 | Arrange support was enabled with no COR named on a service | Known: the quote's soft gap names the missing COR (D8); no row | none |
| 16 | P2 day 4 | Arrangement and supplier grey each other, so the order you pick in matters | Model: each greyed line says why | none |
| 17 | P1 day 4 and 5; P2 day 5 | The BPA quote warned of nothing; the operators warning showed only on a later quote | Known: D25 and D39; the part was unknown until Dunn answered. Note 1 is what is new | none |
| 18 | P2 day 4 | The COR picker explains each choice well; "best screen so far" | Praise, earned | none |
| 19 | P1 and P2 day 6 | The staff meeting paid off on day 6 with the waste request | Praise, earned | none |
| 20 | P2 debrief | The debrief names who ordered what and why ("after SMSgt Ray Dunn said the request needs operators") | Praise, earned (D38) | none |

## Their four answers, in their words (shortened)

Player 1 (section 3 brief):

- **Who were you?** "The newly arrived contracting officer at the hub on the main island ... two staff, MSgt Avery (experienced and fast) and SrA Novak (new and slower), plus a $15,000 cash box, a purchase card and $576K of O&M money."
- **What were you trying to do?** "Meet the commander's bar for 16 days."
- **Did you manage it?** "Yes, all four were met ... Honestly, it was easy. The only real trap was the forklift one, and I escaped it partly by luck: I happened to quote a backup and saw the operators warning there."
- **One change?** "The tutorial needs to teach out loud and to judge what I did ... One short line of guidance at each first-time moment, plus a debrief that marks good and bad contracting decisions."

Player 2 (section 3b brief):

- **Who were you?** "The contracting officer for the Hub on the main island: 300 people, already open, host nation support in place."
- **What were you trying to do?** "Meet the commander's four goals ... use the right type of contract for each and put a COR on the services."
- **Did you manage it?** "Yes ... I nearly got the forklifts wrong. I placed the BPA call before Dunn answered, and the call had no operators. I recovered by stopping the call on day 5 and switching to a written purchase order with Coastal Rentals."
- **One change?** "Warn me when I'm about to buy while a question about that request is still unanswered ... It would also teach the lesson the game clearly wants to teach: ask first, then buy. As it stands, you learn that lesson only by getting burned."

## Fable's reading

1. **The blocker is gone.** Stop this work leaves the shop free to buy again, the same day and after. The tutorial can be won by a player who changes their mind, which the morning's play could not do.
2. **Ask first, then buy, is still learned by getting burned.** Both players asked and bought in the same morning, and the game let them without a word. D42 has the buy summary say an answer is still coming. It does not block the buy, since buying before an answer is sometimes right, and P2 used that on purpose on day 7, when Stop this work made it safe.
3. **Player 1's one change, a tutorial that teaches out loud and grades choices, is not taken.** The turnover, the order-by line and the morning notes are the teaching, and the debrief reports outcomes and who knew what. A grade on contract type would mark a legal card buy as wrong. Notes 1, 2 and 6 are the places its complaint is fair, and each has a row.

## Sim pass

`batch tutorial --seeds 200`: textbook meets every pass line in every seed (mission output median 0.997, forklifts, waste and water 0 days down at the median), the same as the morning's report. DESK-117 to DESK-121 do not change what a policy does.
