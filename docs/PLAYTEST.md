# DESK playtest (backlog DESK-28)

For Nick, after the desk redesign (docs/design/desk-redesign.md). The same cold-start play as on 10 Nov: the tutorial, with no help from outside the game.

1. Get the build as below (Get the build). Leave Developer tools off.
2. New game, Tutorial. Play as you did on 10 Nov, as far as you like; the full twenty days if you can.
3. Write down every moment you did not know what you were looking at or what to do next, with the day and the screen. Your ten findings from 10 Nov are in the design doc, section 1; the pre-gate walk (docs/reviews/2026-11-11-desk-pre-gate.md) says what each one should look like now. Your findings from the 12 Nov play are in section 6: since then each pane scrolls as one piece with its button at the bottom, a greyed button says why directly under it, the desk has five tabs that fit (Today, Requests, Vendors, Payments, Inbox), and Today lists where each request stands. Check those first.

The gate question: does the desk make sense without help? Yes: the paused work resumes (Spoke B gets the team, the port-to-port shipper, better island art). No: say where, and that is redesigned first. Tell Fable in chat.

The M2R script below is kept for its build steps and as the record of the last gate.

# M2R playtest (backlog M2R-23)

For Nick. This is the gate for M2R, Playable opening (docs/design/opening-and-guidance.md section 10). It replaces the M2 script you played on 8 Nov 2026; that version is in the history of this file. Play it only after Fable has said in chat that M2R is ready (the walk in docs/reviews/ repeated from fresh screenshots); until then some checks below cannot pass.

The gate has four checks and one question. Do them in order: the first check only works once, on a cold start.

## Get the build

1. On GitHub, open **Actions, CI, Run workflow**, choose **main**, and run it. After the checks pass, `export-windows` creates the build. Other branches do not export through this button.
   Tag route: Releases, Draft a new release, choose a new `v...` tag on `main`, and publish. The tag also runs CI; its verified zip is attached to that release.
2. When the run finishes, download its artifact `LinesOfSupport-<short-commit>-windows-x86_64` (button route) or `LinesOfSupport-<tag>-windows-x86_64` (tag route). Unzip until `LinesOfSupport.exe` and its sibling `scenarios` folder are together, then run the exe. No install or setup.
3. If Windows SmartScreen warns about an unknown publisher: More info, Run anyway. The build is not code-signed yet.

Leave the Developer tools switch on the title screen off for the whole playtest.

## Check 1: two minutes, cold

Start the exe and choose New game, Spoke B. Start a timer. Read what the game puts in front of you and click through it as you would on your own.

At two minutes, stop and say out loud, without looking back:

| Question | A pass sounds like |
| --- | --- |
| Who am I? | The contracting officer taking over the hub's shop on the main island |
| Where am I? | A hub on the main island, with small sites going in on the outer islands; Spoke B on Second Island, reached by barge |
| What is due? | Spoke B opens on day 7 and needs power; Spoke C opens on day 5; more people land at Spoke B on day 20 |

Pass: all three, in your own words. Write down anything you had to guess, and the first thing on screen that made you stop.

## Check 2: Spoke B, turnover to debrief, no files

Carry on from check 1 and play the thirty days to the debrief. For the gap check below, play approach 1 by hand:

1. On day 1, Capt Ruiz from 3 CES brings the request for power at Spoke B. Take it as written: six 60 kW generators, the generation line only. Leave fuel, operators and maintenance off.
2. Go to the market for it and award a BPA call to Harbor Equipment Co (the shop already has a BPA with them), starting day 7.
3. Do not survey Second Island, do not ask the J4 about fuel, and do not buy fuel for Spoke B yourself.
4. Spoke C, the small buys and everything else: do what seems right to you.
5. If someone at Spoke B makes a deal with a vendor without you (it happens around day 11), ratify it.

Pass: you reach the debrief without opening a file, the console or the developer tools.

### What the debrief should say

| Gap | Tag | Why the game says so |
| --- | --- | --- |
| Spoke B fuel and power, about days 10 to 12 | Avoidable | The generators were bought without their fuel line, and nobody owned fuel at Spoke B. The dependency checklist showed fuel uncovered from day 1 and named the J4 (the ACSA manager) as its usual owner; the J4 was never asked |
| Spoke B fuel, about days 15 to 19 | Discoverable | Fuel comes from the main island and waits on the barge; a survey of Second Island would have shown that |
| Spoke B power, from about day 20 | Avoidable | Six generators against the people who land on day 20, which was in the plan from day 1; the generators also failed with no operators or maintenance line |

Exact days move a little by seed. The tags should not.

## Check 3: the first consequence by day 10

During check 2, note the morning when the game first shows you something that happened because of a decision you made. Under approach 1 it is the morning of day 11: fuel at Spoke B is out, the generators are dark on the map, and Capt Ruiz is the one telling you.

Pass: you saw it by the morning of day 11 and could say which of your decisions caused it, before the debrief told you.

Optional, after the debrief: start Spoke B again and change one thing. On day 1, after awarding the same generators, ask the J4 to add Spoke B to the fuel order. The day 10 outage should not happen; the barge stopping for weather on day 16 still costs Spoke B a day of fuel around day 19, which is the barge lesson. This is the one-decision fix (golden `approach1-with-j4`), and it needs Ask the J4 in the game (M2R-22).

## Check 4: the one-sentence lesson

After the debrief, before reading the list below, write one sentence: what did the game teach you?

## Intended outcomes (read after writing your sentence)

Any of these counts as a match.

Spoke B:

1. A requirement is not a scope: what the customer wrote down is rarely everything the capability needs to run (fuel, operators, maintenance).
2. Some lines are not yours to buy, but they are yours to get owned: bulk fuel is the J4's (ACSA or DLA Energy), and a CCO who sees it uncovered and does nothing still owns the gap.
3. Information has a price and a payoff: a survey costs shop hours now and prevents a gap later.
4. A coordination or a handshake is not a contract: it can disappear, and the gap it leaves is yours.
5. Speed is bought with debt: a UCA gets the vendor moving and leaves definitization and obligation work that comes due.
6. Plans change on dates you can see: population growth and funding expiry were in the plan on day 1.
7. The same decision can be right and still bite: vendors fail, links close, and the debrief separates bad luck from bad calls.

Tutorial (if you play it, see below):

8. Cash on hand and whether the vendor takes it decide what you can buy today.
9. A BPA call is not binding on the vendor: they can say no.
10. A service needs terms and someone watching it (a written order and a COR), not a stack of SF 44s.

## The tutorial (optional at this gate)

New game, Tutorial. The outgoing contracting officer stays three days and tells you what to look at; from day 4 you are alone for the rest of the twenty days. Three requests arrive, on days 1, 4 and 9.

Pass: you finish without help from outside the game, all three requests are covered by about day 15, and you made no unauthorized commitments. Note any moment where the mentor told you something the screen did not let you do.

## The gate question

After playing: does building a contract action feel like deciding, or like data entry?

Deciding: M2R closes; M3-07 and the paused Sim routes resume (design doc section 10).
Data entry: say where it dragged (which screen, which choice felt like typing). That screen is redesigned before anything else.

Tell Fable in chat. The answer goes in `backlog/FOR-NICK.md` and the milestone review (docs/REVIEW.md).
