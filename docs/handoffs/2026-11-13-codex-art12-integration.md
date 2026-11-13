**Work session handoff** (Codex, 13 Nov 2026, ART-12 integration)

Expected look of this build and why: the real generator, black fuel bladder, pale water tank, CHU, field kitchen, dumpster and forklift now sit inside ART-14's themed map. Down equipment keeps its 22 px disc on a short raised stem, and the site keeps its red ring. Cards and shore names clear the equipment, outage badges and stems; air and barge keep their separate paths. Site labels attach to the edge of the site's ring so the zoomed tutorial leaves the equipment visible. The desk, sea and lighting retain main's current look.

## PRs reviewed this session

The live repository had zero open PRs, so the open-PR fix pass required no changes. Reviewed the merged PR 206 result and Fable's 05:00 handoff. Read the session workflow, kickoff, required SPEC sections and ADRs, latest Codex/Fable handoffs, art review, questions, FOR-NICK and decided objections. No unanswered question to Codex or pending objection was found. The direct five-open-PR limit applies.

Continued the existing remote ART-12 branch at 4bdfe096, preserving its review fixes. Merged main f7a51dd without rewriting history. Resolved both Game conflicts by retaining ART-12's kit checks and persistent down markers together with ART-14's callout placement, reported facts and separate lanes. Combined both versions of FOR-NICK #16, including the denial history and Fable's review. No PR was merged by Codex.

## Changed

- Imported equipment bounds now join the callout scenery inventory with their owning island. Raised outage badge rectangles and every stem segment are reserved for both card placement and pointer routing.
- Added native checks that cards, quiet names and pointers never overlap an outage badge or its stem. The six Fit/zoom art capture states also run the full callout checks. The stricter zoom check caught the former site-centre attachment leaving a label on the island; labels now attach to the site's ring edge.
- Retook the required tutorial days 1/4 and Spoke B day 11 Fit/zoom pairs against main, plus held-cargo playback and the closed lane: sixteen new plain PNGs. Main's baseline uses the same capture fixture, session states, camera and renderer; the baseline fixture's model-only assertions are omitted because main still uses primitives.
- Updated only ART-12's Status cell. Updated existing FOR-NICK #16 with the completed integration; no new escalation row or Fable question. No new asset file or provenance change was needed for this integration.

## Why

Fable's 05:00 review required ART-12 to absorb the merged ART-14 changes, include the new models and raised badges in the placement obstacles, and recapture the combined result before review. This implements those requests on the original branch.

## Validated how

- .NET SDK 8.0.415: required Release solution build and all 754 tests pass (633 Sim, 121 Harness; zero failed/skipped). Eight existing solution warnings; final Game build is clean. An initial parallel solution invocation exited without diagnostics; rerunning with one MSBuild worker succeeded.
- Five scenario folders and one rules file validate with zero errors. Provenance, tracked conflict-marker and whitespace checks pass.
- Godot 4.6.1 .NET: MapArtSmokeTest, MapCalloutSmokeTest, MapSmokeTest, MapLayoutSmokeTest, DayPlaybackSmokeTest and LookSmokeTest pass. Coverage includes all required capture states, twenty replay days, authored scenario layouts, pan/zoom and all three looks. Geometry checks and all captures preserve the Sim hash.
- All sixteen new PNGs are 1280x800, below 174 KB, outside LFS, and visually inspected. A temporary authenticated Xorg display was stopped after each capture.

Renderer: OpenGL Compatibility, Linux Mesa llvmpipe 25.0.7, matching the Windows export's renderer setting. This integration adds CPU obstacle tests for the small projected model/badge inventory; no additional model, texture, shader, light or 3D draw call beyond the existing ART-12 implementation. No fresh Windows executable or laptop performance benchmark is claimed. Some native regressions retain the existing ObjectDB exit warning without a failure or crash. Coverage is the authored review states and tested camera positions, not every possible extreme crop or future dense inventory.

| State | Main before | Combined after |
| --- | --- | --- |
| Tutorial day 1, Fit | [PNG](images/2026-11-13-codex-art12-combined-before-tutorial-day1-fit.png) | [PNG](images/2026-11-13-codex-art12-combined-after-tutorial-day1-fit.png) |
| Tutorial day 1, zoom | [PNG](images/2026-11-13-codex-art12-combined-before-tutorial-day1-zoom.png) | [PNG](images/2026-11-13-codex-art12-combined-after-tutorial-day1-zoom.png) |
| Tutorial day 4, Fit | [PNG](images/2026-11-13-codex-art12-combined-before-tutorial-day4-fit.png) | [PNG](images/2026-11-13-codex-art12-combined-after-tutorial-day4-fit.png) |
| Tutorial day 4, zoom | [PNG](images/2026-11-13-codex-art12-combined-before-tutorial-day4-zoom.png) | [PNG](images/2026-11-13-codex-art12-combined-after-tutorial-day4-zoom.png) |
| Spoke B day 11, Fit | [PNG](images/2026-11-13-codex-art12-combined-before-spoke-b-day11-fit.png) | [PNG](images/2026-11-13-codex-art12-combined-after-spoke-b-day11-fit.png) |
| Spoke B day 11, zoom | [PNG](images/2026-11-13-codex-art12-combined-before-spoke-b-day11-zoom.png) | [PNG](images/2026-11-13-codex-art12-combined-after-spoke-b-day11-zoom.png) |
| Held cargo playback | [PNG](images/2026-11-13-codex-art12-combined-before-held-playback.png) | [PNG](images/2026-11-13-codex-art12-combined-after-held-playback.png) |
| Closed barge lane | [PNG](images/2026-11-13-codex-art12-combined-before-closed-lane.png) | [PNG](images/2026-11-13-codex-art12-combined-after-closed-lane.png) |

## Unresolved

ART-12 still has no PR. The earlier automatic approval rejection of `github_create_pull_request` from `codex/ART-12` to `main` remains FOR-NICK #16: it required explicit authorization to open the ordinary public PR. The action is known not to have executed. This session read the latest direct user-message history and searched owner messages for newer ART-12 approval; none was found. It did not retry the denied action or use another tool to open a substitute PR. A source-blob upload was initially rejected: automatic approval called the code private/uncommitted and associated it with the earlier PR denial. Verified the clean local merge commit and read Nick's direct 10 Nov message, "Codex can push whatever is needed, I'll allow it to do it's own risk assessment, but flag anything that may warrant more Claude review based on further claude feedback." Retried only that denied blob once under this materially relevant push authorization; it succeeded with the exact local blob hash. Source/artifact publication can continue, while the separate PR-opening denial remains unchanged.

Fable's review of these combined captures remains pending. ART-13 follows ART-12; ART-15 is the next independent row.

## Next

Keep ART-12's tested combined branch available for Fable's visual review and the existing FOR-NICK #16 decision. Move to ART-15 independently; no need to wait for a merge. PRs opened: none. Open PRs fixed: none; existing ART-12 branch integrated. FOR-NICK additions: none; row 16 updated. No main push, force-push or shared branch deletion.
