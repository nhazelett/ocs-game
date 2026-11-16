**Work session handoff** (Codex, 15 Nov 2026, ART-20 review fixes)

Expected look of this build and why: the same matte 3D kit now frames heads and shoulder tops. High buns, offset ponytails, parted bobs, shoulder waves and curls visibly separate the cast; narrow and broad builds, civilian V necks, cardigans, lapels, polos and zip collars replace the repeated broad collared torso. Ortiz has her visible bun again. The tutorial's three default teammates have different silhouettes. Portraits remain still images with one light and the Tabletop palette.

## PRs reviewed this session

- Fix pass: PR 261 (ART-20), the only open Codex PR. Its existing CI was green and it was mergeable. Accepted all of [Fable's review findings](https://github.com/nhazelett/ocs-game/pull/261#issuecomment-6897884542); no counter or new objection.
- Pulled main at 908a0c4 and merged it into the same branch, keeping both sides. Read the session/kickoff workflow, required spec sections and ADRs, latest handoffs, questions and decided objections. No outstanding question needed a Codex response. This session uses the requested five-open-PR limit.

## Changed

- Retained the original Blender kit and lighting. Rebuilt hair geometry with tapered irregular hairlines, temple locks reaching the ears, visible tied hair and separate longer/curl silhouettes. Both high buns remain visible from the near-front camera. The old thick, level cap edge is gone.
- Authored shoulder/neck builds and narrower jaw variations, softer brows on the narrower builds, and different civilian necklines. Checked every name against its scenario and earlier portrait. The kit README contains the full per-key audit, including Brandt/Joan Avery and Okafor/Eli Novak aliases; no scenario or saved-team key changed.
- Separated the review's tutorial Ruiz/Nair/Tan group (ponytail/bun/shoulder waves), Spoke B Lin/Park (narrow bob/round glasses versus broader short sweep/rectangular glasses), and You/junior teammate (close hair and zip collar versus coils and crew neck). The grouped-sheet pass also caught Dunn/Kowalski and Brooks/Tan: Dunn now has a silver crop and zip collar; Kowalski keeps the receding sides and civilian jacket; Brooks has a chin bob and Tan longer waves.
- Tightened the fixed camera from 2.85 to 2.20 orthographic scale. The head crosses at least 154 of 256 pixels at the eye line, checked in PortraitSmokeTest. All 27 previous renders fail this framing check (66–122 opaque pixels at that row); all new renders pass. The eye checks use the new camera's eye position and retain their light-opening/dark-iris requirements.
- Extended the native CastReview fixture with `--scenarios`: it reads actual scenario names/keys and adds the default team, showing every tutorial and Spoke B person at 48×60 px. No image is enlarged in those two sheets. Refreshed every portrait's provenance. The public backlog retains main's existing review state.

## Why

Fable found that the submitted hair and repeated broad bust obscured the established identities, looked like hats, and made several people interchangeable at 48 px. The larger head crop also restores readable faces in the 36 px strip. These are fixes to the open ART-20 PR, not another backlog item.

## Validated how

- Required Release solution build and all 808 .NET tests pass: 687 Sim, 121 Harness, none skipped, with .NET 8.0.428. Existing nullable/analyzer warnings remain. Game Debug build passes.
- Godot 4.6.1 .NET native OpenGL Compatibility, Mesa llvmpipe: Portrait, Team, DeskHierarchy and Look pass; Look covers all three looks. The final exact head-framing threshold passes again natively and headless. Team exercises the real bounded face popup and last-choice selection, customization, assignment, save/resume and restart.
- Portrait, People, GoingToPeople, Helpers, EndDay, DaySummary and Save pass headless. People logs a nonfatal ObjectDB cleanup warning at exit; its checks pass and the process exits successfully. Native rendering reports the virtual display's unsupported V-Sync setting.
- Re-exported the Windows preset resource pack; from a project outside the checkout, loaded all 27 revised textures and checked dimensions, mipmaps, transparent corners and the new head framing: `PACKED_CAST_PASS`. This does not verify a Windows executable.
- Scenario validation (five scenarios, one rules file), provenance, tracked conflict-marker and whitespace checks pass. No Sim, scenario, schema, rules or golden changes.
- Inspected all ten new native 1280×800 plain PNGs. Every file is below 193 KB. The original matching flat-cast before captures remain below; the initial submission's after captures remain in the earlier handoff for comparison.

Renderer/cost: Blender 4.3.2 Cycles CPU renders offline, fixed seed, 128 samples, one soft area light and ambient world light. No runtime character meshes or lights. The 27 source PNGs total 2,537,100 bytes; the unchanged 256×320 RGBA8 textures with all mip levels total 11,796,300 bytes (11.25 MiB), before engine overhead. Fresh Windows execution and ordinary-laptop performance remain unmeasured.

| Matched state | Before (flat cast on main) | Revised after |
| --- | --- | --- |
| Cast 1: card, assignment, strip | [PNG](images/2026-11-15-codex-art20-before-cast-1.png) | [PNG](images/2026-11-15-codex-art20-fix-cast-1.png) |
| Cast 2: card, assignment, strip | [PNG](images/2026-11-15-codex-art20-before-cast-2.png) | [PNG](images/2026-11-15-codex-art20-fix-cast-2.png) |
| Cast 3 and unassigned | [PNG](images/2026-11-15-codex-art20-before-cast-3.png) | [PNG](images/2026-11-15-codex-art20-fix-cast-3.png) |
| Tutorial Today and strip | [PNG](images/2026-11-15-codex-art20-before-today.png) | [PNG](images/2026-11-15-codex-art20-fix-today.png) |
| Team personalization and picker | [PNG](images/2026-11-15-codex-art20-before-picker.png) | [PNG](images/2026-11-15-codex-art20-fix-picker.png) |
| Giving a task to a free teammate | [PNG](images/2026-11-15-codex-art20-before-assignment.png) | [PNG](images/2026-11-15-codex-art20-fix-assignment.png) |
| Entire tutorial cast at 48 px, including default team | [PNG](images/2026-11-15-codex-art20-legacy-tutorial-48.png) | [PNG](images/2026-11-15-codex-art20-fix-tutorial-48.png) |
| Entire Spoke B cast at 48 px, including default player | [PNG](images/2026-11-15-codex-art20-legacy-spoke-b-48.png) | [PNG](images/2026-11-15-codex-art20-fix-spoke-b-48.png) |

## Unresolved

Hosted CI and Fable's art re-review remain required. Pixel differences and framing checks do not establish recognizability; Fable judges the supplied actual-size sheets. Fresh Windows execution and ordinary-laptop performance remain unmeasured.

## Next

Update existing PR 261 with this complete fix pass and the revised review images, check hosted CI, then Fable re-reviews against D24 and the art bar. The tutorial replay gate and paused Sim-depth work retain their existing scope. No merge, main push, force-push or shared branch deletion.
