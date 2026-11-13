**Work session handoff** (Codex, 13 Nov 2026, ART-12 branch review fixes)

Expected look of this build and why: the kit's fuel bladder is near-black rubber, and the water tank is pale blue-grey with its original dark chassis and wheels. The 22 px down discs sit above the equipment with thin short stems to the actual model, leaving every silhouette visible. The map's old plain labels and routes remain here; ART-14 fixes them independently.

## PRs reviewed this session

[PR 206](https://github.com/nhazelett/ocs-game/pull/206), ART-14: all four review findings fixed on the same branch at 23de75fcad00677e08fe88c886fee962b9da1dca. Its exact head passed [CI run 355](https://github.com/nhazelett/ocs-game/actions/runs/43366281870). Five new native states, updated PR description and review response are published; Fable's visual re-review is pending. No new PR opened.

Read Fable's ART-12 branch review in 2026-11-13-fable-art-reviews.md and accepted both findings. Continued the existing claim on codex/ART-12, whose last remote commit was 58bb1db, rather than creating a second implementation. Merged main 0036f08 without rewriting history. The only conflict was the duplicate FOR-NICK #16 row: kept the original denial details and Fable's newer review and recommendation. The standing five-open-PR limit applies.

## Changed

- Applied shared runtime material finishes to the fuel bladder and only the water tank shell. The kit's shapes, water chassis, wheels and other vertex colours remain. No asset file changed, so provenance rows remain accurate.
- Placed fixed-size down badges above the projected bounds of their actual models, avoiding all model bounds and other badges. Short stems identify an edge of the right model. Where projected rows overlap, a small elbow keeps the stem from passing through another model. Geometry checks cover both Fit map and zoom.
- Added native checks that badges cover no model or badge, sit above their own model, and have a short stem attached to it. The final fixture also checks that each stem clears other model footprints and rendering leaves the Sim hash unchanged.
- Saved six inspected plain-PNG review captures, 1280x800 and below 400 KB. Kept all original before/after files. Updated only ART-12's backlog Status cell and appended the review-fix evidence to existing FOR-NICK #16; no new escalation row or question.

## Why

Fable found that olive fuel disappeared into olive ground, the water buffalo read as a dark lump, and down discs hid the equipment they marked. These changes implement that review on its original branch. They do not alter scenario facts or what counts as down.

## Validated how

- .NET SDK 8.0.415: required Release solution build and all 754 tests pass (633 Sim, 121 Harness; zero failed/skipped). Eight existing solution warnings. Final Game build has zero warnings/errors.
- Five scenario folders and one rules file validate with zero errors. Provenance, tracked conflict-marker and whitespace checks pass.
- Godot 4.6.1 .NET: native MapArtSmokeTest passes tutorial days 1/4 and Spoke B day 11 at Fit map and 1.65x zoom. MapSmokeTest, MapLayoutSmokeTest, DayPlaybackSmokeTest and LookSmokeTest pass on the revised model branch. Model footprints, fixed badge sizes, stem targets and unchanged simulation hashes are checked.
- All six final PNGs were visually inspected. The prior ART-12 after images supply the same-state before for this review fix; the initial handoff also retains main's original primitive-model before images.

Renderer: OpenGL Compatibility, Linux Mesa llvmpipe 25.0.7, matching the renderer setting used by the Windows export. Two shared material overrides when the kit scenes load, plus one 2D line per down badge; no new texture, shader, light or 3D model/draw call. Badge placement checks the small projected model inventory. No new Windows executable or laptop performance benchmark is claimed. Fit/zoom geometry is tested for the authored review states; arbitrary future dense inventories and extreme camera crops are not claimed. Some headless runs retain the existing ObjectDB exit warning.

| State | Before this review fix | Revised after |
| --- | --- | --- |
| Tutorial day 1, Fit | [PNG](images/2026-11-13-codex-art12-after-tutorial-day1-fit.png) | [PNG](images/2026-11-13-codex-art12-reviewed-tutorial-day1-fit.png) |
| Tutorial day 1, zoom | [PNG](images/2026-11-13-codex-art12-after-tutorial-day1-zoom.png) | [PNG](images/2026-11-13-codex-art12-reviewed-tutorial-day1-zoom.png) |
| Tutorial day 4, Fit | [PNG](images/2026-11-13-codex-art12-after-tutorial-day4-fit.png) | [PNG](images/2026-11-13-codex-art12-reviewed-tutorial-day4-fit.png) |
| Tutorial day 4, zoom | [PNG](images/2026-11-13-codex-art12-after-tutorial-day4-zoom.png) | [PNG](images/2026-11-13-codex-art12-reviewed-tutorial-day4-zoom.png) |
| Spoke B day 11, Fit | [PNG](images/2026-11-13-codex-art12-after-spoke-b-day11-fit.png) | [PNG](images/2026-11-13-codex-art12-reviewed-spoke-b-day11-fit.png) |
| Spoke B day 11, zoom | [PNG](images/2026-11-13-codex-art12-after-spoke-b-day11-zoom.png) | [PNG](images/2026-11-13-codex-art12-reviewed-spoke-b-day11-zoom.png) |

## Unresolved

ART-12 has no PR. The prior automatic approval denial of github_create_pull_request from codex/ART-12 to main remains FOR-NICK #16: the reviewer required explicit approval for opening the ordinary public PR, separate from pushing its source and screenshots. That denied action is known not to have executed. This session checked current user-message history and found no newer relevant approval; it did not retry that action, switch tools to open it, or create a substitute PR. Source, captures and this handoff can still be reviewed on the existing branch.

ART-14 and ART-12 remain independent. Combining them must preserve imported model bounds, the raised badge rectangles and stems in the callout obstacle inventory, persistent down rings, and ART-14's local cards and distinct lane paths. Recapture the combined tree; no combined render is claimed here.

## Next

Fable re-reviews the changed art. Nick's existing decision on FOR-NICK #16 still gates opening ART-12's PR; ART-13 follows it. ART-15 is the next independent Codex item. The review-fix and capture work used this session's implementation budget. PRs opened: none. PRs fixed: 206. Existing ART-12 branch fixed. FOR-NICK additions: none; row 16 updated. No PR merged, main push, force-push or shared branch deletion.
