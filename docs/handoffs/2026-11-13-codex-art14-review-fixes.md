**Work session handoff** (Codex, 13 Nov 2026, ART-14 review fixes)

Expected look of this build and why: only shortfalls, reported changes, held cargo and closed lanes use paper cards. Supported sites keep a small shore name, open lanes keep a name on their own path. Air lanes arc above the water and barge lanes remain on it. Critical facts fit on single lines. Pointers are at most 120 pixels and cannot cross another pointer, card or another island; crowded cards can become nearby unboxed tags. Models, sea/light and desk retain main's current appearance.

## PRs reviewed this session

[PR 206](https://github.com/nhazelett/ocs-game/pull/206): its original exact head 3485f91 passed CI run 353. Accepted all four findings in Fable's review comment 6842325611. Merged main 0036f08 into the same branch, keeping both sides. No other open PR was present at the initial fix pass. No unanswered question to Codex or objection needed a response. Read the required workflow, kickoff, SPEC sections, ADRs, DISSENT, backlog, questions, FOR-NICK, decided objections and latest handoffs. The direct five-open-PR limit applies.

## Changed

- Replaced distant packing with bounded local candidates and constrained placement of cards and compact tags together. Every card clears projected scenery, other labels and playback's covered area. Each scenery obstacle carries its island so pointers can reach their own site without crossing somebody else's island.
- Removed boxes and pointers for supported sites and open lanes. Kept full reported facts in issue cards; explicit line breaks split the cargo sentence into two readable clauses.
- Separated air and barge paths, their labels and their closure targets. Flight playback follows the raised path. Closed barge crosses identify only that lane.
- Measured label footprints before placement to account for Godot's deferred container sizing. Collision checks include visible captions; hidden full-card bodies are not visible obstacles when the card becomes a tag.
- Added checks for the review's actual failure modes: maximum pointer length, crossing pointers, pointers through other cards/islands, quiet label forms, separate route targets and unwrapped facts. The existing CI MapSmokeTest exercises these over twenty replay days.
- Updated only ART-14's backlog Status cell. Added five new native review PNGs below, retaining the previous before and after images for comparison. No Sim, scenario, rule or shared-contract change.

## Why

Fable sent PR 206 back because pointers crossed the map, seven cards overwhelmed the operating area, air and barge labels referred to the same dots, and a reported fact wrapped. These fixes implement that review without changing the simulation.

## Validated how

- .NET SDK 8.0.415 (official SHA-512 verified): Release solution build and all 754 tests pass (633 Sim, 121 Harness; zero failed/skipped). Eight existing solution warnings; final Game build is clean.
- Scenario validation: five folders, one rules file, zero errors. Provenance, conflict-marker and whitespace checks pass.
- Godot 4.6.1 .NET: MapCalloutSmokeTest, MapSmokeTest, MapLayoutSmokeTest, DayPlaybackSmokeTest and LookSmokeTest pass. Native capture repeats all five states plus pan/zoom in all three looks, with unchanged Sim hashes.
- All five new PNGs are 1280x800, plain Git files, below 400 KB, and visually inspected. Existing before images use the same state and renderer. No fresh Windows executable or laptop performance measurement is claimed.

Renderer: OpenGL Compatibility, Linux Mesa llvmpipe 25.0.7, matching the Windows export's renderer setting. No new texture, shader or light. Each existing lane uses 28 tiny route dots rather than 18 (ten extra mesh instances per lane); the remaining changes are 2D cards/lines and cached CPU placement. Geometry is covered at the review viewport and tested camera positions, not every hypothetical future inventory or extreme crop. Some headless runs retain the existing ObjectDB exit warning.

| State | Original before | Revised after |
| --- | --- | --- |
| Tutorial day 1 | [PNG](images/2026-11-13-codex-art14-before-tutorial-day1.png) | [PNG](images/2026-11-13-codex-art14-reviewed-tutorial-day1.png) |
| Tutorial day 4 | [PNG](images/2026-11-13-codex-art14-before-tutorial-day4.png) | [PNG](images/2026-11-13-codex-art14-reviewed-tutorial-day4.png) |
| Spoke B day 11 | [PNG](images/2026-11-13-codex-art14-before-spoke-day11.png) | [PNG](images/2026-11-13-codex-art14-reviewed-spoke-day11.png) |
| Held cargo playback | [PNG](images/2026-11-13-codex-art14-before-held-playback.png) | [PNG](images/2026-11-13-codex-art14-reviewed-held-playback.png) |
| Closed barge lane | [PNG](images/2026-11-13-codex-art14-before-closed-lane.png) | [PNG](images/2026-11-13-codex-art14-reviewed-closed-lane.png) |

## Unresolved

Fable's visual re-review remains pending. ART-12 is still an independent branch with no PR under the existing FOR-NICK #16 denial. These captures do not include ART-12. Combining the branches must add its imported model bounds and raised badges to this placement inventory, preserve its persistent down rings, and recapture the combined result. This session also addresses ART-12's two branch-review findings in its own worktree; its separate handoff records those results.

Automatic approval rejected an unauthenticated X screenshot command because it disabled display access control. That command did not run. Captures succeeded using a temporary authenticated display with a private cookie and cleanup; no blocker or new FOR-NICK entry remains from that rejection.

## Next

Fable re-reviews PR 206. ART-12's existing PR-opening denial remains with Nick; ART-13 waits behind it. ART-15 is the next independent Codex row after these review fixes. No PR merged, nothing pushed to main, no force-push and no shared branch deleted.
