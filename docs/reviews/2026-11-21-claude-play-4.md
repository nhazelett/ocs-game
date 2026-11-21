# Claude plays the tutorial, 21 Nov 2026, fourth play (DESK-150 to DESK-153, DESK-155, DESK-156)

The verification play due after the 21 Nov 06:45 run merged Codex's DESK-150 (the notice counts from the signed order, PR 426), DESK-151 (CI only, PR 427), DESK-152 (the debrief says the tutorial is done, PR 428) and DESK-153 (Open vendor decision at the top, PR 429), and Fable's Q29 and Q30 (read contracts, PR 430), DESK-155 (rules said when they apply, PR 431) and DESK-156 (the pinned summary at most three sentences, PR 432) (PLAY-03, docs/PLAYTEST-CLAUDE.md section 2). One fresh agent played with the section 3 brief, the driver and a notes file of its own; it did not read the repository. The second brief was not needed: no stopping, handing-over or ending action changed.

- Build: the tree of PR 432 at 1f5e451 (main with PRs 425 to 431 merged, plus DESK-156); the same tree PR 432 lands. Godot 4.6.1 .NET, 1280 by 800, Xvfb, Dummy audio.
- Seed: 20261121, as the earlier plays today.
- How far: to the debrief after day 16, never stuck for ten steps, all four pass lines met (water 0 days out, forklifts 0, waste 0 with one day at 50%, mission output 100%). 225 commands.
- Logs: [commands](plays/2026-11-21-4/commands.txt) and [input log](plays/2026-11-21-4/input-log.json). The harness replays the log to every line met (`run tutorial --log`, state hash ce7d6ad0).
- Pictures: two of 226.

## What the merged rows did in play

- DESK-150: "Counted from the signed order, which comes after the buy is written; the quote gives the first day it can start." showed on the supplier facts. The player still read "starts 3 days after it goes in" against "1 day after a signed order" as a contradiction (note 9). Shown as built; not enough on its own. Watched.
- DESK-152: the debrief opened "The tutorial is done: all four of Col Ward's lines held." Verified.
- DESK-153: Open vendor decision on day 13 opened the decision at the top, and the player said it "worked". Verified.
- DESK-155: (c) the out-day sentence showed on the turnover's last page and the player used it ("which the game counts as short, not out"). (d) the trade line before a pick was read: "gamey but explained inline". (a) reworded, and the player still read it as a contradiction (note 21, DESK-159). (b) waits for Codex's DESK-157.
- DESK-156: the summary was three sentences on both written buys. Its cost: finance's day moved to the quote's notes, which sit below the writer picker, and the player never scrolled there; after committing, the Now line said the order "waits for finance to certify the funds on day 6", and the player "felt cheated" ([picture](plays/2026-11-21-4/day5-notes-below-the-writer.png)). D47, DESK-158. Even at three sentences the pinned summary is four lines, and pick two sat below it in a scroll a third of the column high ([picture](plays/2026-11-21-4/day5-pick-two-below-the-summary.png)); the player's clicks on Good and Cheap missed until it scrolled. Known: D45, DESK-148 builds that footer.

## The first two minutes

The player ticked every preparation choice (known), read Mills's four turnover pages, found the commander's lines on the last page and again on Today's bar, opened the water request through the Requests tab (the Today entry's "Open request" is inside a longer button the driver names by its whole text; a person would click it: DESK-161), asked Ortiz and bought on the card from Island Mart on day 1.

## Their notes, sorted

| # | Day, screen | What the player said | Sort | Row |
| --- | --- | --- | --- | --- |
| 1 | Preparation | Two choices pre-ticked with no reason; five choices fit 14 days, so no trade-off | Known (every play) | none |
| 2 | Turnover | Host nation supplies forklifts and there is a forklift BPA: which one supplies them now? | Design, watched | none |
| 3 | Turnover | "Coastal Rentals for material handling equipment and vehicle for the contracting office" is garbled | Defect (Sim list wording) | DESK-160 (a) |
| 4 | Turnover, night 2 | "I'm here three more days" and "here for your first three days", then "I fly out today" on night 2 | Defect (scenario text, since DESK-144 moved his notes a day earlier) | Fixed in this PR: "two more days", "your first two days" |
| 5 | Day 1, Today | "Open request →" not a click target | Model (driver names the whole button) | DESK-161 |
| 6 | Day 1, request | The ask's answer comes tonight but the buy is due today | Known (D44: the game says so before the choice) | none |
| 7 | Day 1, buy | After committing, the buy form resets to a blank order, inviting a duplicate | Known (DESK-147 receipt) | none |
| 8 | Day 1, plan | Walk-in small buys first heard of at the end of the day; no count of how many wait | Design, watched (DESK-149 c says what they are) | none |
| 9 | Day 4, forklift request | BPA call "starts 3 days after it goes in" against the card's "1 day after a signed order" | Known (DESK-150 shown, still read as a contradiction) | watched |
| 10 | Day 4, request | "8 hours of your time" in the quote, Avery 6.5 in the picker | Model (the quote is for the writer chosen, you by default) | none |
| 11 | Day 5, request | The suggested route changed overnight to a card buy with no word why | Design, watched | none |
| 12 | Day 5, request | "Add what you learned" is in the other pane, not beside the quote | Known (D45) | DESK-148 |
| 13 | Day 5, buy | The pinned summary covers Fast, Cheap and Good; clicks missed | Known (D45, D46) | DESK-148 |
| 14 | Day 5, buy | "Only after committing did it say the order waits for finance to certify the funds on day 6. That wait was never in the quote." | Design: D47 | DESK-158 |
| 15 | Day 5, buy | No odds for execution-confusion events, so card against written order was a guess | Design, watched | none |
| 16 | Days 6 to 8, Payments | No running card total | Design, watched | none |
| 17 | Day 7 | The staff meeting's hour every day; nothing suggests handing it to someone | Known (DESK-155 b, DESK-157) | none |
| 18 | Days 9 to 12 | Empty days take three clicks; the map showed nothing all game | Design, watched | none |
| 19 | Day 10 | Island Mart "delivered through day 9", then on day 10 "Water at Hub from Island Mart ended." | Defect (a day late and bare) | DESK-160 (b) |
| 20 | Day 13 | Cure notice: decision says 4 shop hours, the button 3.5 for Avery, after commit "4 hours of your time" | Known | DESK-147 (b) |
| 21 | Debrief | "Unforeseeable: nobody could have known" beside "Lane would have reported it sooner": did my monitor matter? | Design, no decision needed: the clause says what an earlier report would have changed | DESK-159 |
| 22 | Days 15, 16 | Waste at 50% with no lever: no call the vendor, ask the monitor now | Design, watched (a cure notice needs the COR's report) | none |
| 23 | Debrief | Best screen in the game; does not say whether the card or the order was the better forklift call | Praise; design, watched | none |

## Their four answers

- Who: the contracting officer for the 300-person hub on Main Island for 16 days, with MSgt Avery and SrA Novak, a card, $15,000 cash and $576,000 of O&M.
- What: Col Ward's four lines: water and forklifts out no more than a day, latrines every day, 95% of sorties.
- Did it: yes, all four.
- One change: "show everything that decides timing, including the funds-certification wait, in the quote before I commit, and keep the action controls from being hidden under the quote box."

## Fable's read

1. **D46 took finance's day out of view (note 14).** The start day already counted it, but the player could not see why, and the Now line after committing read like a new delay. D47: the start sentence says what it waits for, in one clause; still three sentences (DESK-158).
2. **The lag clause still reads against the verdict (note 21),** the second play in a row. On a one-day failure at the end, an earlier report changes nothing, and the clause should say so (DESK-159).
3. **The buy column's footer is the next thing to fix (notes 12, 13).** DESK-148 is unblocked and is Codex's next row.

## Sim pass

`batch tutorial --seeds 200` on this build: textbook mission output 97.3% to 100% (median 99.7%), water out at most 1 day, forklifts and waste never out; greedy 94.3% to 100%; do-nothing 77.8% to 79.6%. Unchanged from the third play; textbook beats both on every pass line.
