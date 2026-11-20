# Claude plays the tutorial, 20 Nov 2026 (third play: verification of DESK-124, DESK-125 and DESK-127)

The verification play after the fixes from the day's second play (PLAY-03, docs/PLAYTEST-CLAUDE.md section 2: fixes on the tutorial's path and changes on screen no play had seen). One fresh agent played with the section 3 brief, the driver and a notes file of its own; it did not read the repository. The second brief was not used: no action a play has not exercised changed (DESK-127 adds a picker to asks and the meeting; handing over was exercised by the second play).

- Build: `main` at 59a1477 (after DESK-124: pick two explained beside its toggles; DESK-125: Payments names the start day, the Inbox names COR reports on file; DESK-127: Who does it on asks and the staff meeting). Godot 4.6.1 .NET, 1280 by 800, Xvfb, Dummy audio.
- Seed: 20261120.
- How far: to the debrief after day 16, never stuck. All four pass lines met: water 0 days out, forklifts 1 day out (the most the line allows), waste 0, mission output 98.1%. 236 commands.
- Logs: [commands](plays/2026-11-20-3/commands.txt) (the number is the screenshot it produced; the first two looks are Fable's check that the game was up) and the [input log](plays/2026-11-20-3/input-log.json). The harness replays it to the same game (`run tutorial --log`, state hash 9d5a0aff).
- Pictures: four of 237.

## The fixes, verified

- DESK-127: the player gave the day 1 ask to Novak and the day 3 staff meeting to Novak from the new picker, and both went to him. It did not find the picker hard to use. It did find that the picker, the top strip and the request disagreed on how long the ask takes (note 3).
- DESK-124: the pick-two trade sentence sat under the toggles; the player compared three quotes on day 5 by it and called that "the first decision that felt like contracting". It still said nothing tells it what Good buys in game terms (note 15).
- DESK-125: Payments was read on day 10 without confusion; the Inbox line was not reached (the COR reports came on day 11 and the decision on day 12, and the player went to the decision).

## The first two minutes

The preparation page opened with two choices ticked; all five fit in 9 of 14 days, so the player ticked them all and said the choice was empty, as both of the second play's players did. The turnover was read in full and the pass lines found on its last page. On day 1 the order-by line said what to do and the player did it, then bought water on the card for $9,733.50.

## Its notes, sorted

The sort follows section 5: defect, design, known, model. A row named here is new in this PR unless it says otherwise.

| # | Day, screen | What the player said (shortened) | Sort | Row |
| --- | --- | --- | --- | --- |
| 1 | Day 5 buy form; day 9 Today; debrief | Read "never out more than a day" as a limit on each outage, not one day in all. Took a written PO quoted "day 8 or 9" over a card buy quoted "day 8"; it started day 9 and used the whole allowance. "The screen should have said a day-9 start uses your 1 allowed forklift outage day." ([quote](plays/2026-11-20-3/day5-start-day-8-or-9.png), [day 9](plays/2026-11-20-3/day9-out-1-day-so-far.png)) | Design, the most important: D43 | DESK-129 |
| 2 | Debrief | The forklift gap is blamed on the host nation withdrawal with nothing behind it; the player's real cause was choosing the later start. The gap reads "Avoidable" and then "Discoverable" two lines apart: which is it? | Design, under D43: the gap says the chosen buy was quoted to start late; one verdict, then its reasons | DESK-130 |
| 3 | Day 1 request | Novak's ask: the picker said 2 hours, the strip then said 1.5 of 8 h, the request said "about 1 shop hour". Day 7: quoted "7 to 13 hours" of Avery's time, then "about 6.5 hours" ([picture](plays/2026-11-20-3/day1-two-hours-one-and-a-half-one.png)) | Defect, Sim views: quotes for new work round a named person's hours up to whole hours (DESK-120 kept that) while the strip and work list say half hours, and the request's in-work line says shop hours, not the person's | DESK-131 |
| 4 | Day 5 day's plan | The plan said Novak's staff meeting is "one job" that "should finish today"; the results said "nothing finished" | Defect, Sim views: the standing meeting is not a job that finishes | DESK-132 |
| 5 | Day 7 buy form | The COR picker said Pratt "watches no other contract" two days after the player named him on the Coastal Rentals PO, still being written | Defect, Sim views: the line counts the buys being written that name him | DESK-132 |
| 6 | Day 9 Today | "Alert · VendorNotice", "Info · Report": a code word on screen ([picture](plays/2026-11-20-3/day9-vendornotice.png)) | Defect, game: the board prints the category's code name | DESK-133 |
| 7 | Day 16 results | The last day's button says "Back to the debrief"; the player had never been to the debrief | Defect, game | DESK-133 |
| 8 | Day 13 decision | The cure notice went onto the player's hours with no choice of who writes it; every other job had one | Design, under D17 ("wherever work is assigned"): the decision takes the Who does it picker | DESK-133 |
| 9 | Day 2 Today | "1 open request" while the water was arranged and delivering | Defect, game text: the count is requests whose days are not over (DESK-78), so "open" says the wrong thing | DESK-133 |
| 10 | Turnover; day 1 | Mills calls water "something that small" for the card; nothing says the card's limit, and the player guessed $10,000 (it is $20,000 in the tutorial) | Defect, scenario text: Mills' money line names the card's limit for one buy | DESK-128 (scope widened) |
| 11 | Day 4 Today | "Order by day 5: a BPA call starts 3 days after it goes in" beside a vendor card saying Harbor gives 1 day's notice | Design, small: the order-by days are the shop's work, the funds and the notice together. Listed; a row if another play stumbles on it | none |
| 12 | Day 3 and 4 | Joining the staff meeting costs Novak 1.5 hours every day after, and nothing said so | Known: the meeting's line says its hours today; D17's team page shows the standing hours. Listed | none |
| 13 | Day 1 and 7 | Asking Ortiz and Ruiz confirmed what the request said; "asking feels like a ritual" | Known: D25 (an ask that finds nothing says what it confirmed), and the lesson is Dunn's answer on day 4 | none |
| 14 | Preparation | Two choices start ticked; all five fit, so there is nothing to give up | Known: the second play said it too; D32 made each choice do something, not cost something. The tutorial teaches what each does; the trade-off belongs to Spoke B. Listed | none |
| 15 | Day 1 buy form | Nothing says what Good buys in game terms; "It is on top of Main Island Utilities" means nothing | Known: DESK-124 put the trade sentence under the toggles; the "on top of" line is D40's. Listed | none |
| 16 | Days 10, 11, 14 to 16 | Long stretches of End the day | Known: D30 (Skip ahead was offered on days 10, 11, 14 and 15; the player pressed End the day each time) and D31 | none |
| 17 | Debrief | Could it have bought forklifts on day 3, before any request? Nothing said buying ahead was an option worth using | Known: D5 (Buy on your own initiative) is on the desk; the debrief's discoverable line is what points to it | none |
| 18 | Day 12 People to reach | "Office vehicle: none running today" with no word on why it would want one | Known: D5, DESK-29 | none |
| 19 | Day 9 results | "Nothing supplies forklifts until Coastal starts on day 9" read as stale on day 9 | Model: it was the results page for day 8 | none |
| 20 | Day 1, 3, 5, 12 | Empty labels and pickers scrolled out of view; "Other updates and sites" and "The full record" seemed to do nothing | Model: the driver prints only words fully in view, and both disclosures open below the fold | none |
| 21 | Day 1 | The buy form stays open after a buy, inviting a second one | Model: the request turns to arranged and Today says so at once | none |
| 22 | Day 7 | $1,325 for 8 days of pumping beside $660 a day for forklifts | Model: scenario prices, not a defect; listed for the next price pass | none |
| 23 | Day 6 | The staff meeting paid off with the waste request a day early | Praise, earned | none |
| 24 | Debrief | Each line says who supplied it and who wrote it; "the best screen in the game" | Praise, earned (D38) | none |

## Its four answers, in its words (shortened)

- **Who were you?** "The contracting officer sent in to run contracting for a 300-person hub on the main island ... and two staff: MSgt Avery (fast, experienced) and SrA Novak (new)."
- **What were you trying to do?** "Keep the commander's four lines met for 16 days ... cover the water point outage, replace forklifts when the host nation withdrew them, replace honey trucks, and deal with a camp vendor under-delivering food service."
- **Did you manage it?** "Yes ... The one gap was my doing: on day 5 I chose a fast and good written PO (start day 8 or 9) over a card buy (start day 8) because I read never out more than a day as a limit per outage, not a one-day budget for the whole deployment ... I passed, but with no margin."
- **One change?** "Show the deadline cost on the quote itself, in the commander's terms ... The game knew that, and the debrief judged me on it. During play the target was worded in a way I misread, and the quote never tied the start-date risk to it. Everything else is polish. This one changed my decision."

## Fable's reading

1. **The fixes hold.** Who does it on asks and the meeting worked as the player expected, and the pick-two sentence was used to choose between three quotes.
2. **The pass lines are a budget and nothing on screen says so.** The commander says "never out more than a day", Today counts "out 1 day so far", and the buy form says "starts on day 8 or 9; it is wanted from day 8" without joining the two. A careful player made a reasonable choice from a misreading the game allowed, and the debrief then blamed something else. D43 says the line as a total and has the buy say what its late end costs against it. It does not choose a route for the player: the PO was a fair choice once its cost is stated.
3. **Hours are said three ways for one job.** DESK-120 left new-work quotes in whole hours on purpose; with a person picked, the quote is that person's hours and should read like the strip beside it.

## Sim pass

`batch tutorial --seeds 200`: textbook meets every pass line in every seed (mission output 0.973 / 0.997 / 1 min, median, max; forklifts, waste and water 0 days down at the median, water at most 1). Greedy 0.943 / 0.9745 / 1 with forklifts out up to 2 days; do-nothing 0.778 / 0.796 / 0.796. No policy beats textbook, and the merged rows did not change what a policy does.
