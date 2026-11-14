**Work session handoff** (Codex, 14 Nov 2026)

Expected look of this build and why: no visible change beyond DESK-43. DESK-44's trial is not shipped; the complete screen row waits for Fable to choose how full explanations fit the multi-island map.

## PRs reviewed this session

Started with zero open PRs. Opened PR 240 (DESK-43); its hosted test, scenarios and provenance checks pass, with godot-shell still running when checked. No fixes to prior PRs were needed. This record is stacked on PR 240.

## Changed

Q24 records the measured conflict between D18's always-visible full explanations and the existing map-card placement rules. Only DESK-44's Status cell changes. No Game, Sim, scenario, rules, golden or asset changes in this record.

## Why

The trial used CapabilityTile.WhyShort verbatim, wrapped at 252 px at the existing 17 px font, and kept reasons visible in every compact form. The existing MapCalloutSmokeTest passed the tutorial case but failed its partial-supply case on map day 10: fuel, power and lodging at Spoke B have zero valid card positions inside a 760 by 416 rectangle after reserving scenery, fixed outage badges, other cards and noncrossing short tethers. Hiding reasons, shrinking type or weakening the geometry check would silently choose a design tradeoff. Q24 asks Fable to decide between expandable map details, a selected-site detail area or revised placement rules. Local trial patch: /tmp/ocs-desk44-map-trial.patch.

## Validated how

The source tree here equals the tested DESK-43 tree. Its Release solution build, all 805 .NET tests, game build/import, expanded Team native regression, market and map-layout checks and repository validators pass. The DESK-44 trial built but its map regression failed as described; that failed trial is removed from this branch. This documentation record needs no screenshot.

## Unresolved

Q24 needs Fable's design decision. No Nick action or new FOR-NICK entry. Windows execution and laptop performance remain untested. No objection to the required outcome, and no reduced screen row is marked implemented.

## Next

DESK-45, the day's plan before ending it, on the existing DayPlan contract. Continue independent rows while Fable decides the map treatment. Fable reviews and merges; Codex never merges.
