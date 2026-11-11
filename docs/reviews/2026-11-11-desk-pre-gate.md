# DESK pre-gate walk (Fable, 11 Nov 2026, scheduled run)

FABLE-SESSION 2b: before a milestone with a player-facing surface goes to Nick, Fable writes down what a first-time player sees, from screenshots. This walk is on the combined tree that merging PR 181 lands (main at e8cf7eb with DESK-14, plus PR 180 DESK-27 and PR 181 DESK-29). On that tree 573 Sim and 121 Harness tests, validate.py, provenance.py and the game build pass. The screenshots were taken this run with Godot 4.6.1 under a virtual display from the game's own smoke scenes, and are committed as `docs/handoffs/images/2026-11-11-fable-desk-walk-*.png`.

## Nick's ten findings, one by one

1. **"Why am I selecting Plant Operators or Maintenance?"** The checkboxes are gone (DESK-23). The request says what it includes, what is missing and who would know, with "Ask about this request" under it; a part becomes addable only after someone tells you about it. The committed request screenshots predate DESK-13 and DESK-22 (they still say "3 unit-days", show a Directory tab and one hours bar), so this walk cannot show the request as Nick will see it. DESK-30 asks Codex for a fresh one.
2. **"Without a request."** Gone from the list. "Buy on your own initiative" is its own entry, and in the tutorial it leads with the office vehicle, why you would want it and who sells it (DESK-25, DESK-29).
3. **"Directory seems overwhelming."** Replaced by Vendors: one card at a time, with blanks and a sentence saying how to fill them: "Not known yet: how they take payment, their prices, how much they can supply and where they deliver. Meet Mr. Marco Silva to find out." (`walk-vendor-blank.png`)
4. **"I don't know what I was trying to do day 2."** Partly answered. End the day now lists what is open before the day ends: the water wanted from day 2 with nothing arranged, and which teammate has nothing to do (`walk-end-check.png`). The staff meeting now sometimes hands you a requirement. But the tutorial's day 2 itself is still a quiet day in the taught path: Capt Mills's note on agreements ("The forklift question is coming...") is the only thing that happens. A player who reads it can go and meet Harbor Equipment's rep or ask SMSgt Dunn early; one who does not has nothing pointing at it. Worth watching at the gate rather than changing now.
5. **"Someone is already on the way to the finance office."** Every task has a name. The team page reads "Going to find the finance office: about 3 shop hours · 3 hours left" under You, with "MSgt Robin Hayes · 2.5 hours at their pace" ready to take it (`walk-team.png`, DESK-14).
6. **"Why on earth would I ever select TSgt Pratt?"** The COR list says why: "TSgt Pratt (3 CES) works with material handling equipment every day and watches no other contract: catches about 80% of problems, reported 2 days late." against SSgt Lane at about 40% (`walk-cor.png`, DESK-27).
7. **"Not too sure what I'm looking at in the ledger."** It is "What we are paying for": funding, then each line with who provides it and what it costs this office, in sentences ("Provided by the J4 under the ACSA; this office has no recorded costs.") (`walk-ledger.png`).
8. **"Everything is 0%."** The cause (terminating host nation support) is refused in the Sim (DESK-01) and the button is never offered (DESK-21). Ending a contract happens only on the office's own contract, behind "End this contract? Here is what stops." with Keep this contract first (`walk-end-contract.png`).
9. **"I should have been clicking continue vs next day."** One button, End the day (DESK-20); Skip quiet days lives in the Menu.
10. **"I don't know if I clicked terminate."** Same answer as 8: there is no Terminate in the list to click by mistake.

The award form also carries the customer's ask beside each number ("Quantity (forklifts) · Asked: 3 forklifts", "Start day · Asked: day 8") and the pick-two trade in one sentence ("Fast and good: you pay a 15% premium for a quick award without cutting corners, about $1,296 of this price.") (`walk-ask.png`).

## The first two minutes of the tutorial

Title, Before you leave, then "Your contracting team": three portraits and names to keep or change. Day 1 (`walk-day1.png`): three named bars in the strip ("0 of 8 h assigned" each), cash, one End the day button, and MSgt Carla Ortiz's water request with Open. A first-time player knows who is on the team and what the first thing to do is.

## Small things seen, none blocking

- Day 1's Inbox opens scrolled a few lines down, so the top of MSgt Ortiz's name is cut off (`walk-day1.png`). Codex, cosmetic.
- The award form's "It would take 14 shop hours" does not say whose hours; assignment happens on the team page afterwards. Fine for the gate; if Nick trips on it, the preview could name who it will go to.
- Run graphically (not headless), PrioritiesSmokeTest fails its quote-placement check here, on main as well as on PR 181, so it is this environment's window and not a regression. CI runs it headless and passes.

## Gate readiness

Ready for Nick once PR 181 (which carries 180) merges and DESK-30's fresh request screenshot shows the forklift request in forklifts with the team strip. The gate is DESK-28: the same cold-start play as M2R-23.
