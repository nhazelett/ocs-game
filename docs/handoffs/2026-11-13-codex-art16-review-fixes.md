**Work session handoff** (Codex, 13 Nov 2026, ART-16 review fixes)

Expected look of this build and why: a single grounded harbor island beside the title, one filled New game, secondary Continue saved game, and small Sound, Look and Skip quiet days controls. Developer tools lines up with the picture's right edge.

## PRs reviewed this session

- PR 213: Fable's 10:50 blockers fixed on the existing codex/ART-16 branch. Integrated main 3fd8baf without rewriting history; the sole backlog conflict retains Fable's merged ART-15 status and this item's review status. Fable's headline and other rows remain unchanged.
- PR 214: latest published 3affcd1 is mergeable; CI run 369 is successful. Its same-branch review-fix handoff covers the contrast and detached-summary findings. No newer review thread or additional fix required. No unanswered question to Codex; all objection records are decided.

## Changed

- Removed the small raised reef which read as floating. No replacement geometry or new asset.
- Reparented Skip quiet days into the small flat settings row with Sound and Look. Its action and visibility are unchanged. Regression checks require the shared row and smaller visible size relative to Continue saved game.
- Right-aligned the navigation controls; on the title they meet the image's 48 px margin. Gameplay retains its 20 px right margin. Native checks verify actual Developer tools/image alignment.
- Only ART-16's backlog Status cell changed beyond preserving main in the merge.

## Why

Meets both blocking findings on PR 213 and the nonblocking Developer tools alignment note. The optional ART-13 title sea unification remains later work after that scenery is accepted.

## Validated how

Release solution build and all 754 .NET tests pass (633 Sim, 121 Harness; zero failures/skips). Final Game build has zero warnings/errors. Scenario, provenance, conflict-marker and whitespace checks pass. Ten affected native suites pass: TitleArt, FrontDoor, Audio, Music, Save, Look, TabletopTheme, Chrome, DeskHierarchy, MapCallout; the first, second, eighth and ninth are rechecked after final alignment. A graphical TitleArt run also passes actual displayed pixels, retained kit, RenderingServer idle mode, exact autosave Continue, settings focus and unchanged Sim hashes.

All six new images are inspected plain 1280x800 PNGs under 400 KB. The before column uses the original reviewed ART-16 captures, from 51f0499, in the same fixture states. No imagery is generated or edited outside Godot.

Renderer/cost: Godot 4.6.1 .NET, OpenGL Compatibility, Linux Mesa llvmpipe 25.0.7. One initial/resize render and retained image; one shadowed light and nine kit instances. Removing the reef reduces geometry. No continuous title 3D draw. Fresh Windows execution and ordinary-laptop performance remain unverified.

| State | Reviewed before | Fixed after |
| --- | --- | --- |
| cold | [PNG](images/2026-11-13-codex-art16-after-cold.png) | [PNG](images/2026-11-13-codex-art16-fixed-after-cold.png) |
| saved | [PNG](images/2026-11-13-codex-art16-after-saved.png) | [PNG](images/2026-11-13-codex-art16-fixed-after-saved.png) |
| deployments | [PNG](images/2026-11-13-codex-art16-after-deployments.png) | [PNG](images/2026-11-13-codex-art16-fixed-after-deployments.png) |
| look-0 | [PNG](images/2026-11-13-codex-art16-after-look-0.png) | [PNG](images/2026-11-13-codex-art16-fixed-after-look-0.png) |
| look-1 | [PNG](images/2026-11-13-codex-art16-after-look-1.png) | [PNG](images/2026-11-13-codex-art16-fixed-after-look-1.png) |
| look-2 | [PNG](images/2026-11-13-codex-art16-after-look-2.png) | [PNG](images/2026-11-13-codex-art16-fixed-after-look-2.png) |

## Unresolved

Fable's visual re-review and CI on the new head remain required. No new FOR-NICK entry or pending owner approval. Automatic approval rejected an attempted persistent unauthenticated TCP X display without a cleanup trap. It did not execute. Direct owner history was checked; no permission for that security change was inferred. A safer, short-lived Xauthority-authenticated display with cleanup succeeded for the final capture; the persistent unauthenticated operation was not retried. Earlier Unix-socket display setup failures were environment errors, not successful renders.

## Next

Take ART-17 if unclaimed, under the direct five-open-PR cap. No PR merged by Codex, main push, force-push or shared branch deletion.
