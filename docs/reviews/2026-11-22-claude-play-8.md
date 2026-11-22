# Claude play, 22 Nov 2026 (eighth)

The verification play due once the seventh 22 Nov play's rows landed: Fable's DESK-225 to DESK-227 (PR 522) and Codex's DESK-228 (PR 523, Payments' Designate COR). Two fresh agents played side by side, one with the section 3 brief and one with the section 3b second brief, each with the driver and a notes file of its own; neither read the repository.

- Build: `main` at 1404479 (PR 523 merged). Godot 4.6.1 .NET, 1280 by 800, Xvfb, Dummy audio.
- Seed: 20261122.
- How far: both to the debrief after day 16, never stuck, all four pass lines met. 153 and 256 commands.
- Logs: [commands, first brief](plays/2026-11-22-8/commands-1.txt), [input log](plays/2026-11-22-8/input-log-1.json), [notes](plays/2026-11-22-8/notes-1.md); [commands, second brief](plays/2026-11-22-8/commands-2.txt), [input log](plays/2026-11-22-8/input-log-2.json), [notes](plays/2026-11-22-8/notes-2.md).
- Picture: [day 15, Payments' Designate COR with the contract caption and the explanation under the button](plays/2026-11-22-8/day15-designate-cor-explained.png).

## Not finished in this run

Nick paused Claude's work on the project during this run (weekly limit) and handed the Fable role to another agent. The notes are committed unsorted in the players' own words (the two notes files above). Sorting them (docs/PLAYTEST-CLAUDE.md section 5), the harness replay of both input logs and the `batch tutorial --seeds 200` Sim pass are the next Fable-role session's first job.

## What the merged rows did in play

- DESK-228: verified on screen. On day 15 the second player opened Payments and read "COR for Main Island Sanitation waste removal at Hub", Lane's choice in DESK-227's words, and under the button "SSgt Lane (3 FSS) does not work with waste removal and already watches 2 other contracts (Island Camp Services' lodging and food service at Hub): catches about 40% of problems, reported the same day. Naming SSgt Lane here also lowers the watch on the 2 other contracts." After the click the line under the map was the record's sentence ("SSgt Lane designated COR on ..., replacing TSgt Pratt (does not work with waste removal: ...)") and the button greyed with "This COR is already assigned to the contract." The player chose Lane knowing the cost (Pratt's two-day lag would have reported after the tour ended) and raised no confusion about the words.
- DESK-227: verified in Payments and the record (above).
- DESK-225 and DESK-226: not reached as tested. Neither player handed work over; neither noted a day summary that contradicted a report.
- Used for the first time in a while: cancelling a card rental (the second player's office vehicle, day 9), clean.

## Candidate findings for sorting (Fable's first read, not yet checked against the build)

- Second brief, day 15: "People to reach" says "You are attending the staff meeting" while Novak was the one attending. Possible Sim or game words defect; check against input-log-2.
- Second brief, day 1: the Funding line picker shows "The O&M line" on a card buy while the text below says a card buy is not paid from a funding line.
- Second brief, day 11: Payments shows Island Mart's water at full cost received despite the half-delivered day.
- Second brief, days 4 and 10: replacing a card buy with a written order was blocked by the player's own card buy holding the vendor's capacity, a rule first met by hitting it; the player's one change asks to see capacity taken and a replace that hands over without a gap.
- Second brief, day 14: no way to hand over or drop the staff-meeting seat; recurring work is not in the team panel.
- Second brief, debrief: calls the forklift card buy "avoidable" though the written order risked the one day allowed; mission output 97%.
- First brief, day 3: the meeting costs Novak 1.5 hours a day, not the stated 1 hour.
- First brief, day 5: a start day one earlier widened the start window at the same price, never explained.
- Both: the goals buried on the last turnover page, walk-in small buys first heard of at End the day, "Open request" scrolled out of view, the card's first day disputed, Payments highlighted on the contract page, the host nation's forklifts "for the whole deployment". Known from earlier plays (see the seventh 22 Nov play's table).
