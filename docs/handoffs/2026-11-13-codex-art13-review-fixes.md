**Work session handoff** (Codex, 13 Nov 2026, ART-13 review fixes)

Expected look of this build and why: every outage or delivery fact sits on opaque paper with a short tether. Quiet site and lane names have a broad contrasting paper halo; route dots stop before the lettering. The accepted sea, shore, haze, light and equipment keep their appearance. The same eight real states are recaptured below.

## PRs reviewed this session

- Fable accepted and merged the fixes in [209](https://github.com/nhazelett/ocs-game/pull/209#issuecomment-6850712777) and [211](https://github.com/nhazelett/ocs-game/pull/211#issuecomment-6850713026) at 10:50 UTC. Codex made no merge. Their fix handoffs remain the record; ART-13 now integrates main 3fd8baf containing both and Fable's 10:50 status/review note. The changes after 26c1087 are documentation only.
- [214](https://github.com/nhazelett/ocs-game/pull/214#issuecomment-6850722847): Fable accepted the scenery and blocked the bare low-contrast fact text plus the remote untethered Spoke B summary. Its original implementation and status heads had green CI (runs 366 and 367) before this review arrived. Both findings and the requested summary case are addressed on this same branch.

## Changed

- Full, compact and inline fact forms all retain an opaque paper panel and a visible straight tether of at most 120 px. No red outage fact is drawn bare over sea. All reported facts, percentages, restoration/failure wording, other shortfalls and lost confidence remain in visible text.
- Quiet site and lane names retain their small unboxed form, with dark ink and a four-pixel contrasting paper halo. Route-dot mesh visibility leaves an eight-pixel clear space around displayed lettering, including its halo; the underlying route and its state do not change.
- Each site can choose a tether source on any of four real projected shore edges, ordered by available room. The solver retains the existing scenery, badge, stem, card and pointer collision rules. It searches up to 64 distinct spatial alternatives per form/shore with the existing 12,000-attempt cap. Every fact summary remains tethered, so the long Spoke B heading cannot float away without a connection to its coast.
- Capability lists use one sentence case, with power first when present: "Power, fuel and lodging down" both in the current-status summary and reported-change summary. Detailed percentage lines keep their individual capability headings.
- A moving delivery identifies the actual departure/arrival terrain beneath its projected carrier. Like held cargo, its tether can leave its own island, while the checks still forbid crossings of other islands. This keeps the real day-13 delivery on paper instead of permitting an unboxed fallback.
- Regression checks require opaque fact-paper, one tether per fact panel, broad quiet-name halos and no route dots through lettering, in addition to all existing complete-fact, pointer-length and collision checks. No assertion was removed or relaxed.

Only ART-13's backlog Status cell changes. The initial ART-13 handoff now links here; rejected images remain as history. Fable also marked FOR-NICK #18 no longer needed in main 3fd8baf; that closure is retained: its denied description update remains unperformed, no permission is inferred, and there is no remaining owner action for that obsolete request.

## Why

Fable's two blockers are visual facts, not scenery or Sim changes. Paper protects fact contrast in every look; the short tether preserves spatial meaning when a compact summary is necessary. A real shore choice avoids detaching facts or shrinking models to create space. The new checks preserve those properties during crowded closures, live deliveries and alternate-look pan/zoom states.

## Validated how

- Fresh Release solution build and all 754 .NET tests pass: 633 Sim and 121 Harness, zero failed/skipped. Final Game build passes with zero errors. No Sim/scenario/schema/rules/spec/ADR source edits.
- Eight native suites pass with successful exits: MapArt, MapCallout, MapScenery, Map, MapLayout, DayPlayback, Look and DeskHierarchy. Coverage includes the same six art Fit/zoom views, held and closed-lane captures, all three looks with pan/zoom, partial supply and twenty real replay days, including the real day-13 restored-site delivery. Failed intermediate placement attempts were treated as failures and corrected before publication.
- New scenery remains 2,168 / 1,809 / 2,420 / 1,687 triangles across spoke-b / spoke-b-90 / sustainment / tutorial; actual model feet remain grounded. The graphical sea check found 27 of 43 sampled sea pixels changing over 1.2 seconds with unchanged Sim state. No graphical ERROR lines in the final capture runs.
- All eight new after-images are inspected, plain 1280x800 PNGs under 400 KB (largest 219,394 bytes). They use the same native capture fixtures and actual states as the first eight captures. Scenario/provenance/conflict-marker/whitespace checks pass.

Renderer/cost: Godot 4.6.1 .NET, OpenGL Compatibility, Linux Mesa llvmpipe 25.0.7. The scenery GPU budget, lights, materials and shader are unchanged from ART-13. These fixes add UI paper/tethers/halo drawing and route-dot visibility checks; shore alternatives increase bounded CPU layout work, cached until camera, size or facts change. No ordinary-laptop frame-rate or fresh Windows execution claim. The full replay fixture takes about 31 seconds here; that is test duration, not a gameplay performance measurement.

| State | Initially reviewed ART-13 | Fixed after |
| --- | --- | --- |
| Tutorial day 1, Fit map | [PNG](images/2026-11-13-codex-art13-after-tutorial-day1-fit.png) | [PNG](images/2026-11-13-codex-art13-fixed-after-tutorial-day1-fit.png) |
| Tutorial day 1, zoom | [PNG](images/2026-11-13-codex-art13-after-tutorial-day1-zoom.png) | [PNG](images/2026-11-13-codex-art13-fixed-after-tutorial-day1-zoom.png) |
| Tutorial day 4, Fit map | [PNG](images/2026-11-13-codex-art13-after-tutorial-day4-fit.png) | [PNG](images/2026-11-13-codex-art13-fixed-after-tutorial-day4-fit.png) |
| Tutorial day 4, zoom | [PNG](images/2026-11-13-codex-art13-after-tutorial-day4-zoom.png) | [PNG](images/2026-11-13-codex-art13-fixed-after-tutorial-day4-zoom.png) |
| Spoke B day 11, Fit map | [PNG](images/2026-11-13-codex-art13-after-spoke-b-day11-fit.png) | [PNG](images/2026-11-13-codex-art13-fixed-after-spoke-b-day11-fit.png) |
| Spoke B day 11, zoom | [PNG](images/2026-11-13-codex-art13-after-spoke-b-day11-zoom.png) | [PNG](images/2026-11-13-codex-art13-fixed-after-spoke-b-day11-zoom.png) |
| Held playback | [PNG](images/2026-11-13-codex-art13-after-held-playback.png) | [PNG](images/2026-11-13-codex-art13-fixed-after-held-playback.png) |
| Closed lane | [PNG](images/2026-11-13-codex-art13-after-closed-lane.png) | [PNG](images/2026-11-13-codex-art13-fixed-after-closed-lane.png) |

## Unresolved

Fable's visual re-review and hosted CI on this new fix head remain required. The GPU/runtime verification limits above remain. The own-island cargo pointer and similar Fit-map generator/CHU/dumpster colors were explicitly nonblocking in Fable's review and are not claimed fixed. PR 211's shorter request-picker wording is also a nonblocking follow-up outside this scenery row. No pending owner approval or new question to Fable for the implemented fixes.

## Next

ART-16 is PR 213 from a concurrent authoring session; Fable returned its title findings to that recently claimed branch at 10:50. Avoid duplicate work on that claim. ART-17 is next if unclaimed; follow the direct five-open-PR cap. This session ends after ART-13 publication and CI follow-up. Codex has not merged a PR, pushed main, force-pushed or deleted a shared branch.
