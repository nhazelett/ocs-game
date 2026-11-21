# Claude plays the tutorial, 20 Nov 2026 (fourth play: verification of DESK-123, DESK-126, DESK-128 and DESK-129)

The verification play after the 20 Nov 22:45 run merged four fixes from the day's second and third plays (PLAY-03, docs/PLAYTEST-CLAUDE.md section 2). One fresh agent played with the section 3 brief, the driver and a notes file of its own; it did not read the repository. The second brief was not used: the new actions (DESK-94's card cancel, DESK-97's Add to this contract) are not on screen until Codex's DESK-136 and DESK-98, and a play with the second brief is due then.

- Build: `main` at 3958b7c (after DESK-94, DESK-97, DESK-123, DESK-126, DESK-128 and DESK-129). Godot 4.6.1 .NET, 1280 by 800, Xvfb, Dummy audio.
- Seed: 20261120.
- How far: to the debrief after day 16, never stuck. All four pass lines met: water 0 days out, forklifts 0, waste 0, mission output 99.7%. 189 commands.
- Logs: [commands](plays/2026-11-20-4/commands.txt) (the number is the screenshot it produced; the first two looks are Fable's check that the game was up) and the [input log](plays/2026-11-20-4/input-log.json). The harness replays it to the same game (`run tutorial --log`, every line met, state hash 0d7c87cd).
- Pictures: four of 190.

## The fixes, verified

- DESK-123: on day 1 the water buy's summary ended "MSgt Carla Ortiz's answer about this request is due on day 1; what they say may change what this buy needs." ([picture](plays/2026-11-20-4/day1-answer-due.png)); on day 4 the forklift buy said the same of Dunn. The player read both. On day 1 it bought anyway (the answer added nothing); on day 4 it bought anyway and Dunn's answer (drivers) caught it out the next morning, which the buy had warned of. It asked for a way to wait (note 8): left out by D42 on purpose; Stop this work made the day 4 mistake free, and the player used it.
- DESK-129: the turnover and Today said the allowance as a total ("out 0 of the 1 day allowed so far"), and on day 5 the purchase order's summary said "If it starts on day 9, material handling equipment at Hub is out 1 day in all: all the commander allows." ([picture](plays/2026-11-20-4/day5-pass-line-cost.png)). The player read it right ("day 9 would use my whole 1-day forklift allowance") and chose with it. The third play's misreading did not come back.
- DESK-128: the player never spent cash and was not told to watch the cash box; it said only that it "never ran short of cash or used the paying agent".
- DESK-126: decisions read "gal/day". The request page's own "Peak demand: 4,500 gal" did not change: it is built in the game, not the Sim (DESK-138 (d)).

## The first two minutes

The preparation page opened with two choices ticked; all five fit in 9 of 14 days, so the player took them all and called the choice empty (known, as in every play today). The turnover's four pages were read and the pass lines copied from the last one. On day 1 the order-by line said what to do, and the player bought water on the card after asking Ortiz.

## Its notes, sorted

| # | Day, screen | What the player said (shortened) | Sort | Row |
| --- | --- | --- | --- | --- |
| 1 | Preparation | Two choices start ticked; all five fit | Known: third play note 14, D32 | none |
| 2, 6, 16 | Preparation, Today, Vendors | Empty labels; "Open request" vanished after a scroll | Model: the driver prints only words fully in view; on Today the whole card is the button | none |
| 3 | Preparation | Effects in game terms before the words mean anything | Model: a person skims them | none |
| 4 | Turnover | Pass lines only on the last page | Known: D28 (Today's bar shows them every day) | none |
| 5 | Turnover | Host nation forklifts "for the whole deployment", then withdrawn on day 7 | Known: the withdrawal is the tutorial's forklift lesson (DESK-80); the briefing warns the yard will want forklifts | none |
| 7 | Day 1, request | Ask said 2 h, 1.5 h and "about 1 shop hour" | Known: DESK-131 | none |
| 8, 13, 22 | Days 1 and 4, buy | The answer-due sentence, but no way to wait for it | Known: D42 left out holding the buy; Stop this work makes buying early safe | none |
| 9, 26 | Days 1 and 5, buy | Fast and Good still starts day 4 to 6 or day 8 or 9 | Known: D43, DESK-124 | none |
| 10 | Day 1, buy | "Execution confusion events can occur" with no scale | Design: listed for the next design pass with the COR note below | none |
| 11 | Day 1, after Arrange support | The buy column went back to a blank form; only the left side said it was bought; could buy twice | Defect | DESK-138 (a) |
| 12 | End the day | Two "End the day" buttons, header and dialog | Model: the second confirms the first | none |
| 14 | Day 2, Today | "1 open request" for water already arranged | Known: DESK-133 (PR 395) | none |
| 17, 27 | Days 3 and 6 | The first staff meeting gave nothing; the second gave the waste request a day early | Works as D4 means it | none |
| 18 | Day 4, top strip | "1 of 8 h assigned" with no unfinished tasks | Known: DESK-132 | none |
| 19 | Day 4, request and vendor card | The order-by line says a BPA call starts 3 days after it goes in; the vendor card says "Notice: 1 day" | Defect: "Notice" reads as the start | DESK-138 (d) |
| 20, 29 | Days 4 and 7, buy | The COR pick changes nothing in the quote; "65%" has no meaning in outage days | Design: listed for the next design pass | none |
| 21, 23 | Day 4, team | Who does it found on the team page; Back returned to the desk with the request open | Known: DESK-127 gave asks the picker; the buy form has it below the fold. Model for Back | none |
| 24 | Day 5, buy | Coastal Rentals with operators costs the same as without | Known: assumptions.md "Adding a part to a running contract" (a buy's price does not drop when a part is left out; playtest) | none |
| 25 | Day 5, vendor card | Harbor Equipment's card did not say it has no drivers | Known: D6, the card fills in as the shop meets the vendor | none |
| 30 | Day 7 | Lodging at 50% for a day with "nothing for you to do" | Works: inherited camp BPA slip, Unforeseeable in the debrief | none |
| 33 | Day 12, Vendors | A contract opened from a vendor card goes "Back to what we are paying for" and lands on Payments | Defect | DESK-138 (c) |
| 34 | Day 12, Payments | O&M remaining is one number | Model | none |
| 35 | Day 13, Today | "0 alerts" while a vendor decision waited on the same page | Defect | DESK-138 (b) ([picture](plays/2026-11-20-4/day13-zero-alerts-decision.png)) |
| 37 | Days 14 to 16 | Nothing happens | Known: D31, DESK-80 | none |
| 38 | Day 16 | "Back to the debrief" before any debrief | Known: DESK-133 (PR 395) | none |
| 40 | Debrief | A win says nothing about which decisions mattered | Design: the debrief explains gaps (D28); a clean win has none. Listed for Nick's next play, not acted on | none |

The picture of note 11 is [here](plays/2026-11-20-4/day1-blank-form-after-buy.png).

## Its four answers

- Who: a new contracting officer running a three-person shop at a 300-person hub.
- Trying to: keep water, forklifts and waste removal inside the commander's allowance and mission output at 95% for 16 days while the host nation withdrew support.
- Managed it: yes, every line met with no day out; it says partly by luck (the day 4 call without drivers, caught the next morning).
- One change: make the quote show the cost of the risks it warns about in outage days. DESK-129 did this for a late start and the player used it; the same is not done for execution confusion or the COR's catch rate (notes 10, 20, 29).

## Sim pass

`batch tutorial --seeds 200`: mission output textbook 0.973/0.997/1, greedy 0.943/0.975/1, do-nothing 0.778/0.796/0.796. Textbook leads; nothing new.
