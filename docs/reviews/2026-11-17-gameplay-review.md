# Gameplay review, 17 Nov 2026

Nick asked in chat on 16 Nov (evening, Eastern) for a thorough review of the gameplay, beyond the daily play. This is it. It does not replace the 17 Nov daily play (docs/PLAYTEST-CLAUDE.md), which still runs.

- Build: `main` at 76c69ca (PR 276), Godot 4.6.1 .NET, 1280 by 800, Xvfb, Dummy audio. DESK-63 (PR 277) and DESK-64 (PR 278) merged while the plays ran, so the plays are from before them; where a finding is one they address, it says so.
- Three blind players, each a fresh agent that saw only the screen through the play driver and never read the repository.
- Fable's own pass: every note checked against the Sim and the game code, the three logs replayed through the harness, 200 seeds of every scenario, and a count of what each day asks of the player.
- Logs, the players' own notes and four pictures: [plays/2026-11-17-review/](plays/2026-11-17-review/).

| Play | Brief | Seed | How far | Result from the replay |
| --- | --- | --- | --- | --- |
| Tutorial, careful | The standard brief | 20261117 | Day 20 and the debrief, 579 steps | 100%, every pass line met, no gaps (state hash 53ee8367) |
| Tutorial, hasty | Skims, clicks what is prominent, makes a first-timer's mistakes on purpose | 4471 | Day 20 and the debrief, 464 steps | 94.1%, three of five pass lines missed, 4 gaps (state hash ab1d7d27) |
| Spoke B, cold | The standard brief, no tutorial first | 20261117 | Morning of day 17, 388 steps (the step limit) | Played on to day 30 with no more actions: 90%, every pass line met |

A first attempt at the two tutorial plays ended in under 25 steps each: both players pressed Back on the preparation screen, chose the Tutorial again, and met a preparation screen with no way out (finding 0). They were restarted and told not to press Back.

## The short version

The game teaches well in the moments where something is at stake, and for most of its length nothing is. The five findings that matter most, in order:

1. **Most days ask nothing.** In the tutorial the careful player ended twelve days in a row (9 to 20) with nothing to decide; the hasty player had no decision on eleven days of twenty. In Spoke B the cold player had nothing asked on days 4 to 8 and 10 to 15. The textbook policy acts on 3 days of 20 in the tutorial and 8 of 30 in Spoke B.
2. **The player is never told what they are playing for, or how they did.** The scenario's pass lines are never on screen, during play or after. The careful player won and wrote "I do not believe I earned it"; the hasty player missed three lines of five and was not told. The only statement of the goal is one sentence from the commander, four clicks deep.
3. **What the player pays hours to learn does not reach them.** The answers from asks, vendor meetings, the J4 and the SCO are not on the day's results page. In Spoke B both SCO answers, the J4's answer, the notice that 64 more people were coming and both weather closures never appeared on any screen the player saw.
4. **The first decision of the game is half empty.** Four of the eight preparation choices change nothing in the Sim, and in the tutorial two of the three pre-ticked ones change nothing either. The choice is also made before the player knows where they are going.
5. **Buying misleads at the moment of commitment.** On this build a finished buy read as a refusal, so the hasty player bought water twice (DESK-63, merged since). Costs appear after the click (ratifying took a whole shop day, ending a contract took a whole day, the staff meeting is a standing hour). A warning and the thing that fixes it sit on different screens.

Under those sits one balance finding: in Spoke B, leaving out the operators and maintenance the player was told about costs about two bad days, and the equipment-only buy ($64,584) passes every line where the textbook's full-service answer ($693,900) leaves no money for food. The main scenario's main lesson is almost free to ignore.

What earned its place, by the players' own account: the team page's sentences ("8 hours free today, nothing queued. This takes them 12 hours; it would be done day 2."), the day's plan, the forklift sequence from the day 3 warning to the declined call, the COR whose odds drop because of the player's own earlier contract, the cure notice decision, and the debrief's gap list with its evidence. Those are the game. The work below is mostly about getting the player to them and telling them the score.

## 0. A way to get stuck before day 1 (defect, blocks any replay)

Open the Tutorial, press Back, choose the Tutorial again: the preparation screen has all eight items and no footer. There is no day count, no "Your team", no "Back" and no "I'm ready to leave" ([picture](plays/2026-11-17-review/prep-footer-gone.png)). Scrolling and Escape do nothing. Both tutorial players hit it within five steps and stopped.

A person meets it on any second visit to the same scenario in one sitting, which includes Menu, New game and the same scenario to play again. From the code (not reproduced by hand): the page is rebuilt while hidden, its labels measure tall at zero width, and the panel keeps that height when the second build measures the same. The 16 Nov play's stuck screen was most likely this and not the 14 of 14 count (that player had pressed Back too), and DESK-62 removed the accident that got that player out. The smoke test re-enters only into the other scenario. Row DESK-67, Codex, first.

## 1. Most days ask nothing

| | Days with a new request | Days the careful or textbook player acts | Days with nothing to decide |
| --- | --- | --- | --- |
| Tutorial, 20 days | 1, 4, 9 (heard day 6 at the staff meeting) | Textbook: 1, 4, 7. Careful player: 1, 4, 5, 7, 8 | Careful: 2, 3, 6 and 9 to 20. Hasty: 11 of 20 |
| Spoke B, 30 days | 1 (two), 3 | Textbook: 1, 3, 14, 16, 17, 19, 20, 21 | Textbook: 22 of 30, with ten in a row (4 to 13) and nine in a row (22 to 30) |

The tutorial's content ends on day 13. After that the careful player's Today tab was "letter for letter the same" for six mornings. Each empty day cost about eight actions: close the results, scroll Today to the top, look in the Inbox, end the day, confirm, close.

The game has a way to skip quiet days and no player could use it. "Skip quiet days" is on the title screen, in the row with Sound and Look, and shows only with a game in progress. It is the old Continue button: it ends days until a visit record or a priority item appears. Once the shop attends the staff meeting every day has a visit record, so it stops after one day, every time. The Spoke B player found it by accident (Menu drops to the title with no warning) and used it as a slower End the day.

The hasty player, asked whether they would keep playing past day 5 unprompted: "Honestly, no. Days 2, 3, 5 and 6 were four clicks each and nothing happened; by day 5 I'd have decided the game was a spreadsheet with a nice island on it." The forklift trap on days 7 to 9 is what would have kept them, and it comes after the point they would have left.

Idle time also teaches the wrong thing. Two teammates with nothing to do for a week drew "8 hours go to walk-in small buys" and no other remark; nobody learned what a walk-in is worth.

Decisions D30 and D31. Rows DESK-72, DESK-75, DESK-80.

## 2. Nothing says what you are playing for, or how you did

The tutorial has five pass lines (water never out more than a day, forklifts never out more than a day, waste removal never out, no unauthorized commitments, mission output at least 95%). No screen in the game shows them. The harness report does.

- The careful player's debrief was one sentence and a button ([picture](plays/2026-11-17-review/debrief-one-sentence.png)): "Over twenty days, no site ran short of anything." No verdict. "I finish not knowing whether any choice I made mattered. I suspect almost any sane choice gives 100% here."
- The hasty player bought water twice, bought water nobody needed on the slowest route, left the forklifts four days late and unstaffed, left waste removal out two days, and drew an HHQ inquiry and an unfunded $2,418.75. The game showed 94.1% and four gaps and never said three lines were missed. They found the debrief "easily the best screen in the game" and wanted its count during play: "even just 'gaps so far: 2' in the header would change everything."
- Each player guessed the goal. "Keep the hub running. That's the whole job this week." is under Today, People to reach, a picker, second entry. "This week" and the record's title "Week one at the hub, twenty days played" disagree with the twenty days.
- The Spoke B player was never told how long the scenario runs or how it is judged.
- One tutorial pass line cannot fail: no tutorial event creates an unauthorized commitment.

Decision D28. Rows DESK-70, DESK-74.

## 3. What you learn does not reach you

The day's results page lists finished jobs by name ("Asking SMSgt Dunn: done by you") and never what was said. Answers live for one morning in "Other updates and sites", collapsed at the foot of Today, and among the last six visit lines under People to reach, where the daily staff meeting pushes them out within a week. Checked against every screen the Spoke B player saw:

- Day 1: the J4's answer (fuel from day 5; "the partner force can cut it back when its own needs rise"). Never on screen. Its only trace was a row at the foot of Payments.
- Days 4 and 6: both SCO answers, each bought with a shop hour (the thresholds; "no contract vehicle covers your area"). Never on screen. The player later met the $40,000 threshold as a refusal.
- Day 12: "64 more personnel arrive day 15" at Spoke C. Never on screen; the next morning read "There is no priority message this morning." The player's food order was then short on day 15 and the game gave the wrong reason for it (section 6).
- Day 14: the second group of 80 for day 20. Never on screen.
- Day 16: both weather closures. The player learned of them from red crosses on the map on day 17.

In the tutorial the same pattern: the morning the forklift vendor ran short and Dunn said nobody was licensed, both sat at the foot of the Inbox while Today said there was no priority message. A new request arrived the same way on day 7, as the third card under Where things stand. The game opens each morning on the Inbox, scrolled part of the way down.

The careful player's one change: "make the Today tab a proper morning brief, shown first and at the top... new requests, what people answered, vendor warnings. Each item should give its deadline and the last day I can order and still make it." That player found the true lead time for the forklifts only by typing day numbers into a field until the quote changed.

Decision D29. Rows DESK-69, DESK-73.

## 4. Preparation is a blind choice, and half of it does nothing

The Sim's own comment on four effects is "recorded for the debrief; no mechanical effect in v0": pulling existing vehicles, vendor lists and vetting, and confirming partner agreements change nothing anywhere, and the warrant increase changes nothing where the scenario sets no warrant (Spoke B). In the tutorial the purchase cards and the paying agent are already working on day 1, so two of the three pre-ticked choices (7 of 10 pre-planned days) also change nothing.

- The Spoke B player spent 6 of 14 days on choices that do nothing and dropped the contract-writing account, which does something. The turnover then said the system was down until about day 12 and did not connect the two.
- The careful tutorial player spent 4 days on choices that do nothing. Every player asked afterwards what their choices had bought. The 16 Nov report sorted "vendor lists" as known; it is a no-op.
- The choice comes before the turnover, so "pick what you'll miss least on the ground" is asked before the player knows what the ground is.
- What each choice changes is in a pointer tooltip only. Three choices in the tutorial are written as form labels ("Pull existing vehicles with scope and ceilings from reach-back") where Spoke B has the same choices in Maj Cole's voice.

Decision D32. Rows DESK-76, DESK-79.

## 5. Committing to something

Known, and merged while this review ran (DESK-63, PR 277): after Arrange support the form stayed, the button greyed and the player's own buy showed as a red refusal. In this review that caused a real mistake: the hasty player read the red as a rejection, did what it said ("ask for 1500 or less") and bought water twice. Every player called it out. The 17 Nov daily play is the first blind check of the fix.

New in this review:

- **Costs after the click.** Ratifying $2,400 of unauthorized work took 16 hours, the whole shop day, and the buttons were bare "Deny" and "Ratify". Ending a contract "uses shop time", which was 14 hours. Joining the staff meeting shows no cost on the button and is a standing hour a day. "Find finance" queued three hours and paid back one line. The Sim already previews all of these (`PreviewAny`); the buttons do not show it.
- **Reasons behind a pointer.** With two or more unavailable choices a picker says "2 choices are unavailable; point at one to see why" ([picture](plays/2026-11-17-review/point-at-one.png)). The driver cannot hover, and neither can a touch screen. Greyed arrangements (cash, card, BPA call) and greyed suppliers gave no visible reason in any of the three plays. D15 says the reason is under the button. DESK-63 now puts a supplier's reason in the list and DESK-66 finishes that; the other pickers are DESK-71.
- **The warning is not where the fix is.** Every forklift quote warns "Without operators, it fails more often" and the form has no way to add them; the fix is "Add what you learned", below the fold on the request. The hasty player bought first and found the button a day later. The careful player nearly did the same after spending a day finding out.
- **Comparing costs you the form.** Switching the arrangement to compare a card buy with a written order wipes the funding line, the pick and the COR. The careful player called that comparison "the first interesting thing in the game" and needed about a dozen actions to make it each time.
- **"Coordinate support"** in Spoke B took a default quantity of 1 from a field inside a collapsed section and answered with the single word "coordinated". The player moved 1 kW a day and never knew.
- **Nothing sums the commitment up.** The hasty player's worst buy (56 shop hours for water starting day 10 for a need that ended day 9) showed every warning, below the fold, in the same weight as everything else.

Decision D33. Row DESK-71 (with DESK-63 merged and DESK-66 open).

## 6. Learning what a request leaves out closes doors without saying so

The Spoke B player asked Capt Ruiz, learned the generators need operators and maintenance, and added both to the request. Two of the three power suppliers then went "(unavailable)", with the reason behind a pointer. The supplier left was Meridian: $726,570 of $900,000, starting nine days late. The player lost most of two days, then found by accident that "Buy on your own initiative" still sold the cheap generators with no check, bought them there, and the request read as covered.

- A vendor who lacks one line of a request cannot be picked on the request at all. That is the rule in the Sim (`Session.Choices.cs`), and it is never said.
- Own initiative is a way round it, and the game then shows the request as covered with "No decisions committed yet" on the request and "nothing covers it yet" in the morning's headline, over a line that names the buy.
- There is no way to take a line back off, to meet a request in parts, or to change a contract already running. The hasty player in the tutorial, with forklifts running and no drivers: "there is no way to amend a live contract to add operators... The mistake was unfixable and the game never said so."
- The textbook policy's own Spoke B answer (the cheap sets as a bridge beside Meridian) cannot be bought from the request page once the lines are added.

This is the game's central lesson (D3: the request is not the requirement) turning into a trap at the moment the player does the right thing. Decision D34, row DESK-77. Changing a running contract is a new action and is Nick's call: FOR-NICK #20.

## 7. What is on screen and wrong

Checked in the code; each is a defect. Sim ones are in DESK-78 unless a row is named.

- A shortfall gave the wrong reason. Spoke C food at 71% on day 15 was shown as a delivery waiting for the next boat; it was 480 meals against 672 people's worth after 64 more arrived. The held-delivery sentence is returned before supply is compared with demand.
- "It is the vendor's to put right, and there is nothing for you to do" on a failure whose stated cause is the parts left out of the buy.
- The strip said "0 of 16 h assigned" on days with a staff meeting, delivery trips and small buys; the day's plan said 5 of 8 where the strip said 4 of 8.
- The same COR on the same contract reads 65% on the order form and 80% on Payments (two different sentences in the Sim).
- "Peak demand: 468 kW" beside a request for six sixty-kilowatt sets, then "demand 324" in a shortfall: three sizes for one camp, none explained. The peak is a later headcount.
- A card buy with "Notice: 0 days" and "0 shop hours" quoted "earliest start day 2 to 3" and started on day 3; nothing says what decides the range.
- "Already set to supply... this would be on top of it" on a buy that overlapped by one day, or not at all.
- The Spoke B turnover says "there are two of you"; the team is the player alone, and the team page still draws "Give them this task" in grey with nobody to give it to (game, DESK-68).
- After an accepted ask to the J4 the form stays and previews a second, identical ask, shown as a red refusal (game, the fault DESK-63 fixed on the buying form; DESK-71).
- "Morning of day 1 · 0 alerts · 0 requests" with two requests open; a site not yet open shows "Power: 100%".
- No record when the contract-writing system comes back.
- The debrief's record files each morning's news under the day before, stops at day 13, and marks every day "Known then: Main Island observed".
- "1 hours", "a 8-day service".
- A vendor's own words at the meeting ("from Main Island") reach neither their card nor the quote, so a supplier on the site's island whose stock crosses the water gives no transport warning until a survey that is itself inside a collapsed section.

Game-side, in DESK-68: Menu leaves the game with no question; a "Developer tools" box is on every player screen, title included (three players clicked it or wanted to); "Show today's movement" does nothing visible on a day with no movement; Today and the Inbox open scrolled; buttons from People to reach stayed pinned to the foot of Today for the rest of the game (DESK-64, merged since; on day 17 of Spoke B two of them took a fifth of the pane, [picture](plays/2026-11-17-review/spoke-b-day17.png)); the scenario choice lists Spoke B above Tutorial with no word on either.

## 8. The map and the pane

In the tutorial the map takes about 60% of the screen. On a good day it shows one island and the label "Hub", and it did not change once in the careful player's first twelve days. When something was short it was the fastest readout in the game (the hasty player: "the cleanest readout in the whole UI"). In Spoke B it carried facts the text never did: fuel waiting at the Hub for a boat, a stock figure with no legend, both lanes closed, an air lane nothing mentions. Two callouts overlapped on day 17 and the island names drifted off their islands.

Everything the player reads and decides is in the right-hand pane, about a third of the width. One request was five to seven scrolls. The buying form is five scrolls with the price last. The Spoke B player: "Day 1 cost about 105 steps. The actual decisions were six clicks." Some of that is the driver's short scroll steps; the fold is real for a person too, and every warning that went unread in these plays was below it.

The screens the players praised are the full pages: the team page, the day's plan, the contract page, the debrief. That points at a layout change (a request and its buying form open as a full page; the map is where you see state), which is larger than a row and belongs to the UI pass Nick said starts with him: FOR-NICK #21.

## 9. The Sim pass

200 seeds, median (min to max):

| Scenario | Measure (pass line) | do-nothing | greedy | textbook |
| --- | --- | --- | --- | --- |
| Tutorial | Mission output (at least 0.95) | 0.787 | 0.979 (0.952 to 1) | 0.993 (0.97 to 1) |
| | Water, forklift, waste days out | 6, 13, 8 | 0, 1 (0 to 2), 0 (0 to 1) | 0 (0 to 1), 0, 0 |
| Spoke B | Mission output (at least 0.90) | 0.607 | 0.716 | 0.924 (0.836 to 0.934) |
| | B power, B fuel, C water days out (at most 1) | 23, 10, 24 | 18, 12, 2 | 1 (1 to 9), 1 (1 to 4), 0 (0 to 1) |
| Spoke B 90 | Mission output (at least 0.90) | 0.645 | 0.671 | 0.925 (0.894 to 0.947) |
| Sustainment | Mission output (at least 0.88) | 0.561 | 0.700 | 0.9095 (0.891 to 0.923) |

- Textbook beats greedy beats do-nothing everywhere, and the tutorial's textbook meets every line in every seed (DESK-61 held).
- Textbook misses its own lines in some seeds of Spoke B (mission 0.836 at worst, power out 9 days, fuel 4), Spoke B 90 and sustainment.
- The cold player's Spoke B, played on to day 30 with nothing more done, meets every line at 90% on $293k, with generators bought without operators or maintenance (5 failures). Textbook pays Meridian $693,900 and its food order is then refused for lack of funds. Leaving out what Capt Ruiz said costs about two bad days in 24.
- Spoke B has no requirement that can come up at the staff meeting, so the meeting (2 hours, then 1 a day) can never pay there. The cold player attended 28 of them.
- No policy moves the tutorial's unauthorized commitment line, ratifications, overdue UCAs or held small buys.

The last three are row DESK-81, after the DESK gate with DESK-12, since they are Spoke B's and the gate plays the tutorial.

## 10. The players' answers

**Careful, tutorial.** Who: "A new contracting officer taking over the contracting shop at an island hub of 300 people, with two staff, a $15,000 cash box, purchase cards and a $576,000 O&M line. The game told me this clearly." Trying to do: "The game never told me up front." Managed: "Yes... I do not believe I earned it. There were three requests in 20 days and each had one obviously workable answer. Nothing went wrong after I ordered, and the last twelve days asked nothing of me." One change: the morning brief (section 3).

**Hasty, tutorial.** Who: "a brand-new contracting officer at an island hub, dropped at a desk with three open tabs of jargon and a big green End the day button." Managed: "Partly. 94.1% and nothing fell over permanently." One change: "make the result of a purchase unmistakable, and put the consequences of a choice next to the button that commits it."

**Cold, Spoke B.** Who: "The contracting officer taking over a small shop at a 600-person hub... I learned this only after the prep choices." Trying to do: "No goal is stated." Managed: "Partly." One change: "show the reason a supplier or arrangement is unavailable as plain text on the form. I lost most of day 2 and part of day 3 to this, and the key discovery of the run was made by accident."

Asked what the game was trying to teach, the Spoke B player named it right (the request is not the requirement; the cheap fast vehicle covers part of the need; know every line of support and what it depends on; a second source is worth money) and said the game made it clear "only in pieces, and mostly after the fact."

## 11. What is sorted out as the model's, or the driver's

- Step counts are inflated by the driver's short scrolls and by the players reading every pane; the fold is still real (section 8).
- The Spoke B play's last command got no answer. The Sim did not hang (the autosave replays to day 30). Three software-rendered games on two CPUs most likely outran the driver's 30 seconds; unproven. PLAYTEST-CLAUDE.md now says two games at most.
- Calling 4,500 gallons a day on a card under $10,000 a possible split purchase, and the $39,200 cash buy trimmed to sit under the threshold: D11 leaves splitting findings out on purpose.
- A UCA quoted slower than a purchase order: true in these rules (DESK-61's note), and nothing makes a UCA worth it below the threshold. Left for after the gate.

## 12. What follows

Decisions D28 to D34 are in docs/design/desk-redesign.md section 10. Rows DESK-67 to DESK-81 are in the backlog. Two questions for Nick are FOR-NICK #20 and #21. The order: Codex fixes the preparation screen (DESK-67), then DESK-66; Fable builds the day's answers and news (DESK-73) and the commander's bar (DESK-74) first.
