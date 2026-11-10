**Work session handoff: ART review fixes**

Expected look of this build and why: the Tabletop map says "Power, fuel and lodging down" at Spoke B. Fuel waiting at the source end of the closed barge lane has a nearby paper caption and a line to its crate. Native 1280 by 800 plain PNGs: [day eleven](images/2026-11-10-codex-art-fix-day10.png), [closed route](images/2026-11-10-codex-art-fix-closed.png), each below 150 KB.

## PRs reviewed this session

142 and 143: green CI at the start; Fable's two map findings require changes on both branches. No PR merged. Main a78f8b6 was retrieved through the connected GitHub app and its complete tree and recent commits verified by SHA; shell HTTPS has no credentials. Main merged into ART-04 without a conflict, preserving Fable's award-choice contract and backlog additions.

## Changed

Site captions name down and degraded capabilities using the actual tiles. Supported and unopened sites retain their captions. Held cargo is grouped by its actual route, destination, capability and reason. It sits on the source shore facing the destination. Its label names what is waiting, where it is going, and whether the route is closed or has no service today. A leader follows the crate as the camera moves; collision handling no longer pushes that label into an unrelated corner.

## Why

Answer Fable's review of PR 142; the same correction is carried into PR 143. All wording reads MapDay and capability views. No supply rules or causes are inferred.

## Validated how

Solution and Game builds pass; Game has no warnings. All 619 .NET tests pass (498 Sim, 121 Harness). Scenario, provenance, conflict and whitespace validators pass. Native graphical Map smoke passes and exits cleanly; it checks all three down capability names, the real closed-route caption, its source-side position, nearby leader, camera tracking and unchanged simulation hashes. Both screenshots opened and inspected. Seven inherited solution warnings remain.

## Unresolved

Archived screenshots and two unused comparison fonts remain sparse locally; their existing remote Git objects are preserved. No technical objection and no FOR-NICK addition. This note covers the fix pass; later work has its own handoff.

## Next

Carry the fix into ART-05, then take M2R-36, Nick's next prioritized row. Stop new work at five open Codex PRs per the current session instruction.
