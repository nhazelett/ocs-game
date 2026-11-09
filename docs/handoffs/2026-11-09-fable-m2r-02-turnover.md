**Work session handoff**: Fable, scheduled run, 9 Nov 2026. M2R-02 turnover and briefing data, stacked on M2R-01 (PR 96).

Expected look of this build and why: no change on screen. The turnover and the prep message exist as Sim views only; Codex shows them in M2R-11. What the views say for Spoke B with no prep picks, so Nick can react to the words before any screen is built (the prep message preselects GPC accounts, cash and paying agent, and system access; with those picked the shop paragraph says the system is down until about day 3 and purchase cards work):

    BRIEF Your first morning on the main island
     You're the contracting officer for the hub on the main island, six hundred people, and the small sites going in on the outer islands. Spoke B on Second Island has no grid and no fuel point, and the only way there is a barge twice a week. Spoke C on Third Island is on the host nation grid.
     Spoke B opens on day 7 with a hundred and eighty people, and a second group lands on day 20. Spoke C opens on day 5.
    FROM Capt Sam Whitfield, Contracting: Welcome to the island. I fly out in three days, so let's get you the keys.
    -- Shop
    Here's the shop. It isn't much. There are two of you, 16 hours a day between you. The contract-writing system is down until about day 3, so everything's manual until then. The paying agent has $50,000 in cash for SF 44 buys. Purchase cards work.
    -- Contracts
    Everything we have runs through the host nation. Main Island Utilities (host nation) has power, fuel, lodging, food service and waste removal at Hub; lodging and waste removal at Spoke B; and lodging and waste removal at Spoke C, for the whole deployment. The host nation fuel point at the hub has never let us down.
    -- Vendors
    Here's who I've used. Half of these I've never called. Main Island Utilities (host nation) on Main Island for power, fuel, lodging, food service and waste removal: I've seen their work myself. Island Power Rentals on Main Island for power: I've talked to them, but their prices are estimates. Island Power Rentals quoted me once and I never followed up. Meridian Site Services on Second Island for power: I've talked to them, but their prices are estimates. Harbor Equipment Co on Main Island for power: someone's been out to look at them. Second Island Fuel on Second Island for fuel: they're a name on a list; I've never called them. Port Fuel Haulers on Main Island for fuel: I've talked to them, but their prices are estimates. Port Fuel Haulers came by last week asking for work. Third Island Water Works on Third Island for water and food service: I've talked to them, but their prices are estimates. Coastal Provisions on Third Island for water and food service: they're a name on a list; I've never called them. Island Power Rentals, Second Island Fuel and Coastal Provisions aren't vetted yet.
    -- Money
    Last, the money. O&M has $900,000, good through day 75. That's all the money there is until the new fiscal year.
    LAST Spoke B opens in a week and nobody's bought them power yet. CE's outside.

## PRs reviewed this session
None new beyond the M2R-01 note (PR 95 reviewed by comment; see the M2R-01 handoff).

## Changed
- Schema and Sim: `briefing`, `turnover` (section remarks plus per-item remarks), `prepBriefing` (from, intro, line per choice, defaults); bundle checks.
- `SessionView.Turnover()` (`TurnoverView`: four prose paragraphs plus rows); `PrepView.From/Intro/Defaults`, `PrepOption.Says/Default`. ADR-0006 addendum.
- Spoke B and Spoke B 90 scripts. The prep message comes from Maj Cole (SCO staff); the turnover from Capt Whitfield.
- Backlog: M2R-02 implemented; M2R-18 (Codex validate.py mirror) widened to these checks.

## Why
docs/design/opening-and-guidance.md 3.1, 3.2, R6 and R9 (Nick: no bullet points in game text).

## Validated how
`dotnet test src/LinesOfSupport.sln` 377 Sim (16 new in TurnoverTests: four prose paragraphs with no list marks or ids for every scenario folder, the shop paragraph follows prep, sustainment's hidden problems do not reach the turnover, bad references refused) and 120 Harness pass; every golden unchanged; `validate.py`, `provenance.py`, `conflicts.py` pass.

## Unresolved
- The vendor paragraph lists every vendor, which is long for Spoke B (eight). If Nick finds it heavy, the turnover can name only the vendors the outgoing CCO has a remark for and leave the rest to the directory.
- Site names read as "at Hub" because the site is called "Hub". A scenario can rename it; no code change made.
- Sustainment has no cast or script yet; its turnover is facts only.
- The prep message's sender is the SCO staff officer; design doc 3.1 also allows a home-station deployment manager, which would need a new role.

## Next
M2R-03 (CE small-buy stream and the GPC program).
