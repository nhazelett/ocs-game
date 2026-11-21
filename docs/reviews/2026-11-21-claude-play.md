# Claude plays the tutorial, 21 Nov 2026 (verification of DESK-133, DESK-130, DESK-131 and DESK-132)

The verification play after the 21 Nov 00:45 run merged Codex's DESK-133 (four small screen fixes from the third play) and Fable's DESK-139, DESK-130, DESK-131 and DESK-132 (PLAY-03, docs/PLAYTEST-CLAUDE.md section 2). One fresh agent played with the section 3 brief, the driver and a notes file of its own; it did not read the repository. The second brief was not used: Add to this contract (DESK-98) and Cancel this card buy (DESK-136) are not on main yet, and a play with the second brief is due when they are.

- Build: fable/DESK-132 at 9dee7e3, the tree `main` has at 420f3d0 (after PRs 395, 404, 407, 408, 409 and 410). Godot 4.6.1 .NET, 1280 by 800, Xvfb, Dummy audio.
- Seed: 20261121.
- How far: to the debrief after day 16, never stuck. All four pass lines met: water 0 days out, forklifts 0, waste 0, mission output 100%. 194 commands.
- Logs: [commands](plays/2026-11-21/commands.txt) (the number is the screenshot it produced) and the [input log](plays/2026-11-21/input-log.json). The harness replays it to the same game (`run tutorial --log`, every line met).
- Pictures: four of 195.

## The fixes, verified

- DESK-133: Today and the Inbox said "Vendor notice" and "Higher headquarters"; the last day's button said "On to the debrief"; the cure notice on day 13 offered Who does it, and the player gave it to Avery. Nothing the third play found came back.
- DESK-131: the picker said Avery's hours to the half hour. The line under the committing button still said "4 hours of MSgt Joan Avery's time" against 3.5 on her bar (note 15): that is DESK-142, already filed.
- DESK-132: the day's plan no longer called Novak's standing meeting a job that finishes. The work label of the cure notice reads "Cure notice the performance issue ..." (note 16): a missing word, new, DESK-145.
- DESK-130: no gap in this game came from a late start, so the new sentence did not show. Its tests stand on the third play's log.

## The first two minutes

The preparation page opened with two choices ticked; all five fit in 9 of 14 days, so the player took them all and called it a free lunch (known, as in every play). The turnover's four pages were read and the pass lines copied down by hand. On day 1 the request's order-by line said what to do; the player asked Ortiz (Avery) and bought water on the card the same day.

## Its notes, sorted

| # | Day, screen | What the player said (shortened) | Sort | Row |
| --- | --- | --- | --- | --- |
| 1 | Day 1 request; days 1 to 7 news | The most important. Every answer and every piece of advice arrived the night after the decision it was about: Ortiz's answer and Mills's card-or-cash note on day 1, the staff-meeting tip on day 2 (a day spent idle), Ruiz's answer on day 7. The buy said the answer "may change what this buy needs" while the request said "Order today", and nothing said whether the answer comes before End the day. "Either deliver answers within the same day, or state plainly ... answers arrive tonight, so ask a day early." ([picture](plays/2026-11-21/day1-answer-due.png)) | Design: D44, answers and advice reach you before the choice they are about | DESK-144 |
| 2 | Day 15 results, day 16 Today | Waste removal at 50% on day 15, "Nobody has told you why yet", and no way to ask; Today showed the 50% alert while the bar under it said "out 0 days so far; none allowed. Met so far." "I couldn't tell whether I was failing." The debrief answered it: a 50% day is short, not out. ([picture](plays/2026-11-21/day15-short-nobody-told-you.png)) | Design, under D43: the bar says a short day is not an out day, and "nobody has told you why" says when the COR's report is due | DESK-145 |
| 3 | Debrief | "Mission output: 100%, against 95%. Met." then "it first dipped on day 15". It cannot be both. ([picture](plays/2026-11-21/debrief-mission-dipped.png)) | Defect, Sim: mission output never dipped; the sentence names the first gap whatever mission output did | DESK-145 |
| 4 | Day 13 Inbox | After issuing the cure notice, "A cure notice ... is already in work" shows in red, as if it were an error, under a greyed button and a picker still offering who does it. ([picture](plays/2026-11-21/day13-cure-notice-in-work.png)) | Defect, game | DESK-146 |
| 5 | Day 16 Vendors, buy form | The vendor card says "Notice: 1 day"; a one-day cash buy from the same vendor ordered on day 16 quoted "starts on day 18 or 19" | Defect, Sim words: the card's notice is the vendor's lead time, and an order starts a day or two after it on top | DESK-145 |
| 6 | Day 4 and day 7 buy forms | TSgt Pratt "works with material handling equipment every day" on the forklift buy and "works with waste removal every day" on the waste buy: one man, two daily jobs | Defect, Sim words: he knows both; say "knows" | DESK-145 |
| 7 | Day 13 | "Open vendor decision" jumped to the Inbox scrolled to the middle | Defect, game | DESK-146 |
| 8 | Day 9 Today | The host nation's waste withdrawal shown twice, as the alert and again under New since yesterday | Defect, game | DESK-146 |
| 9 | Day 4 BPA call | Choosing the BPA call filled in the vehicle but Supplier stayed "Choose a supplier..." and the button grey | Defect, game | DESK-146 |
| 10 | Preparation, day 7 buy form | The driver's text read the fourth preparation choice and the three pick-two checkboxes (once two are ticked) as `button ""` | Defect, game or driver: a control the text cannot name | DESK-146 |
| 11 | Day 11 and 12 | Dining hall short two days running; the news had no button and the only lever was End this contract; a decision came the night of day 12 | Design, under D29: the news says the COR's report brings it to the desk as a decision | DESK-145 |
| 12 | Day 7 buy form | Pick two's trade sentence appeared only after a pair was picked, below the fold | Known: DESK-124 put it under the toggles; it says the trade of the pair picked. Listed | none |
| 13 | Day 3 Vendors | One vendor at a time; "a table of all vendors would save a lot of clicks" | Known: D6 left a comparison table out on purpose. Listed | none |
| 14 | Day 1 to 10 | Island Mart's start rolled to day 3 and ran to day 9, a day the host nation's water was back; endings are reported in the next morning's news | Known: the quote said "day 2 or 3" and a buy runs its days; the next-morning ending is Q27's projection (DESK-139). Listed | none |
| 15 | Day 13 | "4 hours of MSgt Joan Avery's time" against 3.5 on her bar | Known | DESK-142 |
| 16 | Day 13 report | "Cure notice the performance issue with ...": a word missing | Defect, Sim words | DESK-145 |
| 17 | Day 1 | "8 hours go to walk-in small buys" is never explained | Known: the team page and the plan say it; listed | none |
| 18 | Day 12 Payments | SSgt Lane "catches about 40%" on a buy he does not know, "about 85%" on the camp contracts he does | Model: both are right in their place, and each says why | none |
| 19 | Day 5 | The card buy was strictly better than the written order and "the no written terms warning didn't feel like a real cost" | Known: the cost of no written terms (no COR, no cure notice) shows on the camp contract's own decision; Mills's day 3 note says it. Listed | none |
| 20 | Debrief | Each line names who supplied it and when it was ordered; "clear and satisfying" | Praise, earned (D38) | none |

## Its four answers

- **Who were you?** "The new contracting officer at the hub on the main island: 300 people, a tour of 16 days, with Avery (experienced) and Novak (new) working for me."
- **What were you trying to do?** Meet the commander's four lines, which meant bottled water on the card on day 1, forklifts with drivers on the card on day 5, and a written order for latrine pumping with TSgt Pratt as COR on day 8; and a cure notice on the dining hall.
- **Did you manage it?** "Yes ... It was closer than the result suggests": an idle day 2 because the staff-meeting tip came that evening, waste ordered with no slack after a day spent waiting for an answer that added nothing, and a 50% day at the end it could not read.
- **One change?** "Make advice and answers arrive before the decision they're about, not the night after."

## Fable's read

1. **Answers and advice come the night after the choice (note 1).** The Sim fires a day's events and finishes a day's work when the day is played, so an ask finished on day 1 is read on the morning of day 2, and Mills's day 1 note too. The 20 Nov fourth play saw the same and the buy now warns that an answer is due; this play shows the warning does not say the one thing the player needs, which is that it comes tonight. D44 (design doc section 23): the ask and the buy say "comes in tonight, after you end the day", and the tutorial's handover notes from Capt Mills reach the player the morning before the choice they are about. Answers in the same day are left out: the day is the game's unit of time, and asking a day early is the lesson the forklift request teaches.
2. **A short day is not an out day, and the screen does not say so (notes 2, 3).** The bar counts whole days down, which the turnover's words imply and the debrief confirmed; on the morning after a 50% day the player could not tell. The bar line says it once there has been a short day, and the debrief stops saying mission output "dipped" when it did not.
3. Small things: four Sim sentences (notes 5, 6, 11, 16) and five screen points (notes 4, 7 to 10).

## Sim pass

`batch tutorial --seeds 200`: mission output textbook 0.973/0.997/1, greedy 0.943/0.9745/1, do-nothing 0.778/0.796/0.796 (min, median, max). The same as the 20 Nov fourth play: textbook leads; nothing new. DESK-130 to DESK-132 changed words only.
