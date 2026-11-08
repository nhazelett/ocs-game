**Work session handoff**

Expected look of this build and why: on Spoke B, the morning board now carries two new seeded events. Some day between 8 and 24 the Third Island ferry stops for 2 to 4 days (a "Forecast" report appears the day before if Third Island was surveyed). Some day between 9 and 20 a Spoke C commander message announces a follow-on element, followed by a line giving the arrival day and numbers (about 40% more people, 3 to 5 days out). The debrief can show a new factor, PopulationJump, which the game has no label for yet (M2-23). Nothing else in the game changes.

## PRs reviewed this session

None. No PRs were open at session start (scheduled run, 14:47 UTC): PRs 52 to 56 had been merged at Nick's 14:17 to 14:43 check-in.

## Changed

M2-19 (event chains 2 and 3), on branch fable/M2-19:
- Scenario schema: effect `daysRange` (seeded length), effect `forecastDays` (link.closed only), effect kind `site.populationJump`, vendor offer `sourceIsland`.
- Sim: effect lengths drawn at creation from the events stream; forecast record a day ahead when the link's non-hub island is surveyed; unplanned arrivals added to `PopulationOn` from their arrival day (known only from the notice day); `SessionView.PlannedPopulation`; consumables from an offer with `sourceIsland` ride the links from that island; the survey and the action editor (once surveyed) name it.
- Debrief: `GapFactorKind.PopulationJump` (Unforeseeable for 7 days after arrival, then Avoidable); the off-island factor now cites only closures on its own route (it used to cite any link closure, so a Third Island ferry closure would have shown up in a Spoke B fuel gap).
- Textbook policy: tops up after a jump, but only by BPA call, cash or card. Its preference for on-island sourcing uses the offer's stock island.
- spoke-b scenario: `ferryWeather`, `spokeCFollowOn`; Coastal Provisions food stocks from Main Island; three model-assumption lines.
- Goldens for spoke-b regenerated and diffed by hand; spoke-b-90 unchanged.
- docs/assumptions.md (debrief tag row, "Seeded disruptions" table), ADR-0006 addendum, backlog M2-19 row and new Codex row M2-23.

## Why

SPEC 4 says disruptions are authored chains with preconditions and windows, seeded. SPEC 11 needs 3 event chains for first playable. M2-18 was claimed by another session (`fable/M2-18`, pushed about 14:25 UTC), so this run took M2-19. M2-18 then merged as PR 57 while this run was working, so main was merged into this branch: BACKLOG.md and assumptions.md were keep-both, and the spoke-b goldens were regenerated on the combined tree and diffed against main's again. Main's backlog already used M2-21 and M2-22, so the Codex row for this work is M2-23. A stray blank line that broke the backlog table between the M2-20 and M2-21 rows was removed.

## Validated how

- `dotnet test src/LinesOfSupport.sln` on the tree merged with main (after PRs 57 and 58): 161 Sim tests and 97 Harness tests pass. That includes 16 new tests in SeededDisruptionTests: five seeds give different days; the closure is 2 to 4 days inside its window; the forecast comes a day ahead only after a survey; the jump is 40% on 3 to 5 days' notice and unknown before its notice; Coastal Provisions food ships from the hub; jump gaps tag both ways; closures stay on their own route; determinism.
- `python3 tools/validate/validate.py` (3 folders, 0 errors), `python3 tools/validate/provenance.py` pass; no conflict markers.
- 200-seed pass-criteria count, spoke-b: textbook 182 of 200 pass (was 177), median mission output 0.931 (was 0.911); greedy 0.714 (was 0.721); do-nothing 0.607 (unchanged). spoke-b-90 is unchanged on every policy (187 of 200 for textbook).
- Golden diffs: the approach logs keep the Spoke B day 10, 12 and 16 stories line for line. What changed: the new event lines; Spoke C water and food dips late in the run where a scripted log cannot react (tagged unforeseeable); seed-1 policy runs, where new arrangements shift later vendor rolls.
- One test changed meaning: "Approach 3 has no gaps" is now "Approach 3 has no gaps except unforeseeable Spoke C follow-on gaps".

## Unresolved

- The 7-day react window for PopulationJump tagging, and the one day of forecast lead, are placeholders to tune in the M2-11 playtest.
- `tools/validate/validate.py` (Codex lane) does not yet mirror the new C# cross-reference checks; that is M2-23.

## Next

- Fable: M2-19 was the last open Fable M2 item. Next, add rows from SPEC 11's first-playable list (Fable lane) or support the M2-11 playtest.
- Codex: M2-23 after this merges.
