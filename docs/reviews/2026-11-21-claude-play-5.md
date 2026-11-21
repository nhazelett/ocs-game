# Claude plays the tutorial, 21 Nov 2026, fifth play (DESK-154, DESK-157 to DESK-161)

The verification play due after the 21 Nov 06:45 run merged Fable's DESK-158 (the start sentence says what it waits for, PR 434), DESK-159 (the lag clause says what an earlier report would have changed, PR 435) and DESK-160 (two tutorial sentences, PR 436), and the 08:45 run merged Codex's DESK-157 (Join the staff meeting says its daily hour, PR 438), DESK-154 (number fields named, PR 437) and DESK-161 (link lines clickable, PR 439) (PLAY-03, docs/PLAYTEST-CLAUDE.md section 2). One fresh agent played with the section 3 brief, the driver and a notes file of its own; it did not read the repository. The second brief was not needed: no stopping, handing-over or ending action changed since the last play.

- Build: main at 4d914ff (PR 439 merged). Godot 4.6.1 .NET, 1280 by 800, Xvfb, Dummy audio.
- Seed: 20261121, as the earlier plays today.
- How far: to the debrief after day 16, never stuck for more than two steps, all four pass lines met (water 0 days out, forklifts 0, waste 0 with one day at 50%, mission output 100%). 171 commands, the shortest play yet.
- Logs: [commands](plays/2026-11-21-5/commands.txt) and [input log](plays/2026-11-21-5/input-log.json). The harness replays the log to every line met (`run tutorial --log`, state hash bac6f287).
- Pictures: two of 172.

## What the merged rows did in play

- DESK-158: "It starts on day 8 or 9, after finance certifies the funds on day 6 and the buy is written; it is wanted from day 8." showed on the day 5 Coastal Rentals order, and the player did not repeat the fourth play's "felt cheated". Verified.
- DESK-159: the debrief's clause ended "but hearing sooner would not have changed that day." The player read it correctly: "the monitor trade-off I weighed made no difference here" (note 39). Verified; whether the pick mattered is the new question (DESK-165).
- DESK-160: (a) "Coastal Rentals for material handling equipment and a vehicle for the contracting office" on the turnover; (b) "Water at Hub from Island Mart ended: its last day was day 9." Neither drew a note. Verified.
- DESK-157: "Joins the staff meeting so requests reach you in the room; whoever goes then spends 1 hour at each meeting, every day." showed under the button; the player joined, and the meeting paid off on day 6 (note 25). Verified.
- DESK-154 and DESK-161: the driver named every number field by its caption, and the player opened a request through "Open request" once; once it had scrolled the line out of view first (note 6). Driver rows, verified.

## The first two minutes

The player ticked every preparation choice (known), read Mills's four turnover pages, found the commander's four lines on the last page and again on Today's bar ("clear and useful"), opened the water request from Today, asked Ortiz through Avery and bought on the card from Island Mart on day 1.

## Their notes, sorted

| # | Day, screen | What the player said | Sort | Row |
| --- | --- | --- | --- | --- |
| 1 | Preparation | Two choices pre-ticked with no reason; nothing says this is a tutorial | Known (every play) | none |
| 2 | Preparation | All five fit in 14 days, so no choice | Known (D32: the trade-off belongs to Spoke B) | none |
| 3 | Turnover | "I'll leave you a note each day", not where | Model (they are on Today and in the results) | none |
| 4 | Turnover | Host nation runs the forklifts, and there is a forklift BPA: which is mine? | Design, watched (second play running) | none |
| 5 | Turnover | The commander's bar is clear | Praise | none |
| 6 | Day 1, Today | "Open request" not found after scrolling it away | Model (driver; it worked unscrolled) | none |
| 7 | Day 1, request | Selecting a person and the Ask button are separate | Watched | none |
| 8 | Day 1, request | "Order today" against "Ortiz's answer may change what this buy needs": what waiting costs is not said | Known (D44) | none |
| 9 | Day 1 | Bottled water at about $0.31 a gallon is not believable | Design, watched: the tutorial price keeps the 7-day card buy under the card's limit; changing it moves the lesson | none |
| 10 | Day 1 | No "Tutorial" label, no guidance prompts | Known (20 Nov second play: a tutorial that teaches out loud is not taken; Mills's notes and the quote are the teaching) | none |
| 11 | Day 1, plan | Walk-in small buys first met in the day's plan | Known (DESK-149 c) | none |
| 12 | Day 2, Today | Join the staff meeting at the bottom of the news card | Watched | none |
| 13 | Day 2, Vendors | "Meet Mr. Tomas Reyna": terms already known, so what do 2 hours buy? | Defect (the game says what the meeting does not do, never what it does) | DESK-166 |
| 14 | End of day 3 | The host nation pulled its forklifts, as Mills hinted | Praise | none |
| 15 | Day 4, request | No way to pick who writes the buy on the buy panel | Known (D45) | DESK-148 |
| 16 | Day 4, monitor picker | SSgt Lane "already watches 2 other contracts", unexplained until day 11 | Defect (Sim words) | Fixed in this PR: "(Island Camp Services' lodging and food service at Hub)" |
| 17 | Day 4, team then request | After the hand-over the buy column still said "given to you, about 8 hours" | Known | DESK-147 (a) |
| 18 | Day 4, request | Does the BPA cover maintenance? | Watched (the ask answered it the next morning) | none |
| 19 | Day 4 night | Dunn: the rental needs drivers, Harbor is short of forklifts; Mills's warning paid off. "The best design moment in the tutorial" | Praise | none |
| 20 | Day 5, request | "Add what you learned" buried mid-page; adding operators changed no price | Known (D45, DESK-148); the price line is DESK-149 (d) working as built | none |
| 21 | Day 5, card quote | "No written terms on a 13-day service: execution-confusion": a report column's name, not English ([picture](plays/2026-11-21-5/day5-card-quote-report-words.png)) | Defect (Sim words) | Fixed in this PR: "on a day the vendor and the unit disagree on how much or when, nothing on paper settles it and only half of that day's supply arrives." |
| 22 | Day 5, buy | Pick two clearly explained; took fast and good | Praise | none |
| 23 | Day 5 | Stopping Avery's Harbor call meant the team screen; the request page offered Stop this work only on the new order | Defect (game) | DESK-164 |
| 24 | Day 5, team screen | Red "Choose another teammate for this task." under every task, reads as an error | Defect (game) | DESK-162 |
| 25 | Day 6 | The staff meeting flagged the honey trucks a day early | Praise | none |
| 26 | Day 7 | Quote "8 to 16 hours", then 13 for Avery | Model (the range is for you; Avery is faster) | none |
| 27 | Day 8 | Coastal started on day 8 | none | none |
| 28 | Day 9 | The host nation's water came back on the last paid Island Mart day | Model (the player bought days 3 to 9 against a request for days 2 to 8) | none |
| 29 | Days 8, 9 | Quiet days cost two clicks | Known (D30, Skip ahead) | none |
| 30 | Day 10 | Skip ahead said day 13, stopped on day 12 | Model (its sentence says it also stops "when anything needs you"; a COR report came in) | none |
| 31 | Day 11 | Lane's report explained his two contracts | Fixed with note 16 | none |
| 32 | Day 13 | Cure notice given to Avery, panel still "4 hours of your time" | Known | DESK-147 (b) |
| 33 | Day 13 | The news names terminate; the decision panel sends it to the contract page | Watched (as built) | none |
| 34 | Day 15 | 50% waste: was that a miss? | Known (DESK-155 c says once what counts as out) | none |
| 35 | Day 16, Today | "Open site details →" only marked the site on the map; the details came from clicking the map label ([picture](plays/2026-11-21-5/day16-open-site-details.png)) | Defect (game) | DESK-163 |
| 36 | Day 16 | Pratt's report due day 17, after the end | Watched | none |
| 37 | Debrief | First time anything said "tutorial"; it never taught directly | Known (note 10) | none |
| 38 | Debrief | Only here learned Coastal came with operators and maintenance | Watched | none |
| 39 | Debrief | Paid for fast and good partly to avoid a failure: did it matter? | Design, no decision needed: the debrief says what the pick did to the odds | DESK-165 (a) |
| 40 | Debrief | Nothing on walk-in buys, the cure notice or what it achieved | Defect (the debrief drops threads the player started) | DESK-165 (b), (c) |
| 41 | Debrief | Stopped at the end | none | none |

## Their four answers

- Who: a contracting officer who knows the instruments, running the 300-person hub with an experienced MSgt and a new SrA.
- What: hold Col Ward's four lines for 16 days.
- Did it: yes, all four. "It wasn't hard. The one real test was the forklift request ... Most of my friction was hunting for buttons and second-guessing stale or contradictory text, not making contracting decisions."
- One change: "Make the Tutorial actually teach, and do it on the Request page ... Make the quote and Buy pane refresh when work changes hands. Today the key controls ('Add what you learned', 'Join the staff meeting', who writes a buy) are found by scrolling and luck."

## Fable's read

1. **The fixes held; the friction left is Codex's queue.** Every Sim sentence from the fourth play read as meant. Stale "given to you" text (DESK-147) and controls found by scrolling (DESK-148) are the player's one change and are already Codex's next two rows; this play adds three small game rows (DESK-162 to DESK-164).
2. **Two player-facing sentences were still report words or a mystery (notes 16, 21).** Both are fixed in this PR's Sim; no golden moved.
3. **The debrief drops what the player started (notes 39, 40).** A fast and good pick, a cure notice still running and the walk-in buys each get one sentence (DESK-165). The tutorial that teaches out loud stays not taken (20 Nov second play), but it has now been the one change in three plays; it is the first thing to look at if Nick's own play says the same.

## Sim pass

`batch tutorial --seeds 200` on this PR's tree: textbook mission output 97.3% to 100% (median 99.7%), water out at most 1 day, forklifts and waste never out; greedy 94.3% to 100%; do-nothing 77.8% to 79.6%. Unchanged from the fourth play in every column (this PR changes words only); textbook beats both on every pass line.
