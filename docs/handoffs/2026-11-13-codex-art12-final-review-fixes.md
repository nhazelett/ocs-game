**Work session handoff** (Codex, 13 Nov 2026, ART-12 final review fixes)

Expected look of this build and why: real kit models sit in two separated rows; every down badge is directly above its model's actual silhouette on one straight seven-pixel stem. Outage facts survive a full paper card, an unboxed summary and a complete name label. At Fit map Spoke B names fuel, power and lodging; the zoomed tutorial names water and food service. Sea, shores and desk retain the existing look.

## PRs reviewed this session

- [209](https://github.com/nhazelett/ocs-game/pull/209), ART-12: initial head 0b310c9 had green CI but Fable's three blocking findings in comment 6846297264. Accepted all three, merged main 89568b8, then 977abfd, into its existing history and fixed them on the same remote branch.
- [211](https://github.com/nhazelett/ocs-game/pull/211), ART-15: head 31eedb7 had green CI and no review comment at the initial check. During publication, Fable sent it back in the 08:50 review for the same missing-outage-facts root cause and requested a partial hours-bar capture. It is the next fix-pass target.

Read the session/kickoff, required SPEC sections and ADRs, latest Codex/Fable handoffs, questions, objections and art review. No unanswered question to Codex or pending objection. The directly instructed five-open-PR limit applies; there were two at the fix pass.

## Changed

1. Full site cards now fall back to a compact fact summary, then the complete summary in the name, never a bare name with only "down". Summaries group identical typed reports but retain supply percentages, restorations, supplier failures, other current shortfalls and lost confidence. The selected shore uses the actual projected ground footprint. Pointers remain at most 120 px; complete named site summaries can search within 180 px, and quiet lane names within 60 px. Cargo keeps its paper card and visible tether, with its centre bounded within 170 px.
2. Separated equipment rows instead of moving badges sideways or bending stems around neighbours. Cached four true mesh silhouette extrema when each model loads; the fixed camera orientation means pan/zoom reprojects those four points. Badges attach to the real topmost mesh point, not an empty AABB corner. Rings and island ground accommodate the layout.
3. Gave field-kitchen.glb and waste-dumpster.glb distinct PROVENANCE descriptions. No new asset file.
4. New checks require every current down capability in visible text in all art capture states, plus straight short stems over the right silhouette. MapCalloutSmokeTest also exercises partial supply after the closure. The twenty-day MapSmokeTest validates first-closure layout before its cargo geometry assertions. Both native art/callout scenes now run in CI with the existing timeout and exit-code checks.
5. The stricter replay exposed crowded fractional reports and an exhausted placement search. Grouped same-value reports, place the most constrained labels first and cache projected anchors during the search. This fixes the failures without removing facts or weakening collision, pointer or Sim-purity checks. Only ART-12's backlog Status cell changed; Fable's headline remains intact.

Main advanced during artifact upload to 977abfd (Fable PR 212). Its only changes are the backlog headline and the new 08:50 review note. Integrated it without source/asset changes; the captured Game bytes are identical to the 89568b8 baseline. The latest full solution checks were repeated after this documentation merge.

## Why

Fable required facts at every fallback level, badges immediately over the right model and independent asset descriptions. The expanded checks initially failed on the original branch, then caught the crowded replay states while implementing those requirements. All assertions and process exits below now pass.

## Validated how

- .NET SDK 8.0.415: Release solution build and all 754 tests pass (633 Sim, 121 Harness; zero failed/skipped). Eight existing solution warnings; final Game build has zero warnings/errors. The solution excludes Game and no solution source changed after that run.
- Godot 4.6.1 .NET: MapArtSmokeTest, MapCalloutSmokeTest, MapSmokeTest (twenty real replay days), MapLayoutSmokeTest, DayPlaybackSmokeTest and LookSmokeTest pass with successful exits. Coverage includes tutorial days 1/4 and Spoke B day 11 at Fit/zoom, first closure, held playback, later partial supply, pan/zoom and all three looks. The geometry and capture fixtures preserve Sim hashes.
- Five scenario folders and one rules file validate; provenance, tracked conflict-marker and whitespace checks pass.
- Recaptured all sixteen comparison PNGs at 1280x800 on main 89568b8 and the revised tree, using identical scenario/camera fixtures. All are plain Git files under 400 KB (largest 189,188 bytes); all eight final after states were visually inspected. Main's art fixture omits model/layout assertions that expose its existing defects; baseline viewport and Sim-purity checks remain. No baseline defect is counted as a passing revised-tree assertion.

Renderer: OpenGL Compatibility, Linux Mesa llvmpipe 25.0.7, matching Windows export settings. Same model inventory and 3D draw calls; one extra 2D summary label per site, bounded CPU placement and cached model silhouette points. No new texture, shader or light. No fresh Windows execution or ordinary-laptop benchmark. MapLayout/Look retain an existing nonfatal ObjectDB exit warning; graphical runs report unsupported V-Sync changes on the dummy display.

| State | Current main before | Revised after |
| --- | --- | --- |
| Tutorial day 1, Fit | [PNG](images/2026-11-13-codex-art12-fixed-before-tutorial-day1-fit.png) | [PNG](images/2026-11-13-codex-art12-fixed-after-tutorial-day1-fit.png) |
| Tutorial day 1, zoom | [PNG](images/2026-11-13-codex-art12-fixed-before-tutorial-day1-zoom.png) | [PNG](images/2026-11-13-codex-art12-fixed-after-tutorial-day1-zoom.png) |
| Tutorial day 4, Fit | [PNG](images/2026-11-13-codex-art12-fixed-before-tutorial-day4-fit.png) | [PNG](images/2026-11-13-codex-art12-fixed-after-tutorial-day4-fit.png) |
| Tutorial day 4, zoom | [PNG](images/2026-11-13-codex-art12-fixed-before-tutorial-day4-zoom.png) | [PNG](images/2026-11-13-codex-art12-fixed-after-tutorial-day4-zoom.png) |
| Spoke B day 11, Fit | [PNG](images/2026-11-13-codex-art12-fixed-before-spoke-b-day11-fit.png) | [PNG](images/2026-11-13-codex-art12-fixed-after-spoke-b-day11-fit.png) |
| Spoke B day 11, zoom | [PNG](images/2026-11-13-codex-art12-fixed-before-spoke-b-day11-zoom.png) | [PNG](images/2026-11-13-codex-art12-fixed-after-spoke-b-day11-zoom.png) |
| Held cargo playback | [PNG](images/2026-11-13-codex-art12-fixed-before-held-playback.png) | [PNG](images/2026-11-13-codex-art12-fixed-after-held-playback.png) |
| Closed barge lane | [PNG](images/2026-11-13-codex-art12-fixed-before-closed-lane.png) | [PNG](images/2026-11-13-codex-art12-fixed-after-closed-lane.png) |

## Unresolved

Fable's visual re-review and hosted CI on the new PR head remain required. The existing nonblocking observations about box-like silhouettes at Fit map and a cargo pointer crossing its own Hub island remain for the next art step. No new FOR-NICK entry or owner action. Tests cover authored states and the named camera positions, not arbitrary extreme crops or future dense scenarios.

## Next

Finish the new PR 211 review findings, then continue the highest unblocked item, ART-13, stacked on this tested ART-12 branch if it has not merged. Fable reviews and merges on its own schedule. Codex merged no PR, pushed nothing to main, force-pushed nothing and deleted no shared branch.
