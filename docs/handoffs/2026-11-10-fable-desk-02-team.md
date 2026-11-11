**Work session handoff** (Fable, scheduled run, 10 Nov 2026, DESK-02)

Expected look of this build and why: no screen change yet. The Sim now has a team of three in the tutorial (you, MSgt Joan Avery, experienced, and SrA Eli Novak, junior), each with 8 hours a day. The outgoing CCO's shop paragraph introduces them by name. DESK-22 (Codex) puts the three bars and the handing-out on screen.

## PRs reviewed this session

| PR | Verdict |
| --- | --- |
| 158 DESK-20 (one End the day button) | Ready to merge. Nonblocking: Skip quiet days centred and next to Continue on the title menu. |
| 159 DESK-21 (what we are paying for) | Ready to merge after 158. Nonblocking: host nation and partner cards show three $0.00 lines each; the contract view's Back bar runs under Menu; two lines start lower case. |
| 160 DESK-25 (own initiative) | Screens match D5. CI red: godot-shell crashed on exit (signal 11) after SupportSmokeTest printed PASS, on a handoff-only commit whose code was green at 6fd097b. Rerun requested. |
| 161 ART-09 (music) | Screens fine. Same exit crash after PerformanceSmokeTest PASS. Rerun requested. |

## Changed

- `shop.team` in the scenario schema and data: id, role (you, experienced, junior), name, rank, portrait, hours a day, speed. Bundle checks exactly one "you".
- Every action has an optional `assignee`; the work it creates goes to that person, in that person's hours (a job of the usual size divided by their pace). No assignee: you. New action `assign` hands unfinished work over and re-reckons its hours.
- The day: each person works only their own items from their own hours. Hours left over go to walk-in small buys, as before.
- Every preview (award, BPA, IDIQ, external, ACSA, host nation, visits, file review) reckons days and queue for the assignee.
- Views: `SessionView.Team` (name, rank, face, role label, pace sentence, hours a day, spoken for today, free today, queued, their work items) and `HoursIfAssigned`.
- Names and faces: `InputLog.team` (only changes from the scenario), passed to `Session.Create`.
- "someone is already on the way to the finance office" names the person; the outgoing CCO's "Someone's been out to look at" is now "We've been out to look at".
- Textbook policy divides work (least loaded first); greedy, the first-timer, leaves everything on you.
- Tutorial: `shop.team`, people 3. Spoke B and the others have no team: you alone with all the shop's hours, so nothing there moves.

## Why

D2. Work is divided by the player; the experienced CCO is faster; that is the only difference at first.

## Golden moves (tutorial only)

- approach1-taught and approach2-cash-for-waste: the logs now give the forklift call (and in approach 1 the written waste order) to Avery. Outcomes unchanged; "shop hours worked" falls (37 to 33, 29 to 27) because Avery is faster.
- policy-textbook-seed1: with three people the textbook office keeps up with walk-in small buys, so the day-3 complaint to the commander is gone and the purchase card program is running from day 5 instead of day 3 (it now runs on one person's hours). Pass criteria unchanged (two not met, as before).
- do-nothing and greedy: unchanged. Every other scenario: unchanged.
- This differs from the design doc's Sim note ("goldens move only in text"): one person per job is what D2 says and what the rules' shop days always meant, so the numbers had to move. Recorded in the design doc under Sim notes.

## Validated how

521 Sim tests (10 new in TeamTests) and 121 Harness tests pass; validate.py and provenance.py pass.

## Unresolved

- Spoke B's team is DESK-12, after the DESK gate: its approach logs and about 30 fixture tests assume one shared pool of hours.
- Game text still says "someone" in two places (the Audio panel's cue list and "someone has news for you" on the board); DESK-22 can name the person.
- An unknown assignee is refused on Apply; previews do not yet say so (the game only offers team members).

## Next

DESK-03 (requirements completed by asking the right person), stacked on this branch.
