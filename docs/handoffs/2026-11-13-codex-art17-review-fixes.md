**Work session handoff** (Codex, 13 Nov 2026, ART-17 review fix)

Expected look of this build and why: the seventeen named portraits stay awake and individually posed. People without a portrait key show a featureless moss silhouette in the same Tabletop palette and knit collar; the fallback no longer assigns a shared face to unrelated people. The desk screenshot shows the actual 36 px team icons.

## PRs reviewed this session

- PR 216: read Fable's 12:45 review and green CI run 371. Accepted the fallback finding and requested recaptures. Merged current main d47fe6e into the same branch without rewriting history; resolved only BACKLOG.md, keeping Fable's headline, ART-16 completion and new ART-18 row.
- PRs 213 and 214 are already merged by Fable. Their latest review and both agents' handoffs were read; no outstanding objection or question to Codex was found. The active user instruction caps this session at five open PRs.

## Changed

- Restored the unknown-key/null-key fallback to a featureless silhouette with the current background and collar style. All seventeen named SVG files remain byte-identical to reviewed PR head 35fe110.
- The generator and fallback provenance match the new asset. PortraitSmokeTest checks a uniform facial interior for fallback renders, and keeps the two open-eye/pupil checks on every named face, including glasses.
- Updated only ART-17's backlog Status cell; refreshed the cast sheet and captured Today with the real team strip. No scenario, Sim, schema, SPEC or ADR changes.

## Why

Fable found five unrelated tutorial vendor representatives sharing one grey face. A silhouette honestly communicates an unassigned portrait until ART-18 supplies the nine missing identities. Fable explicitly amended its earlier fallback/open-eyes wording in the review; no objection.

## Validated how

- Release solution build and all 754 .NET tests pass (633 Sim, 121 Harness); Game Debug build passes. Existing nullability/analyzer warnings remain in unchanged Sim files. The first default parallel build stopped without a diagnostic; the single-worker build succeeds.
- Six native suites pass: Portrait, People, Team, GoingToPeople, Requirement and DeskHierarchy. Native cast and desk capture runs also pass their assertions.
- Scenario validation (five folders), provenance, tracked-conflict and whitespace checks pass. All eighteen SVGs parse and regenerate exactly; seventeen named portraits are byte-for-byte unchanged.
- Both new plain PNGs are 1280x800 and below 400 KB, visually inspected and outside LFS. The cast shows the featureless fallback; the desk shows eyes on the actual 36 px team icons. One capture was started before Game finished building; it produced no screenshot and was rerun successfully after the assembly existed.

| Evidence | Native PNG |
| --- | --- |
| Original closed-eye cast baseline | [Before](images/2026-11-13-codex-art17-before-cast.png) |
| Initial PR, including the rejected grey fallback | [Before review fix](images/2026-11-13-codex-art17-after-cast.png) |
| Corrected cast and silhouette | [After review fix](images/2026-11-13-codex-art17-review-cast.png) |
| Tutorial Today, actual team strip | [36 px desk portraits](images/2026-11-13-codex-art17-review-desk.png) |

Renderer/cost: Godot 4.6.1 .NET, OpenGL Compatibility, Linux Mesa llvmpipe 25.0.7. Static 256x320 SVG rasterization and existing 36 px team icons; no new rendering loop, shader, light or texture size. The silhouette has less geometry than the rejected face. No fresh Windows execution or ordinary-laptop performance measurement. Captures use a temporary Xauthority-authenticated display with cleanup.

## Unresolved

Fable's visual re-review and hosted CI on the updated branch remain required. No Nick-only blocker or new FOR-NICK entry. The old ocs-game checkout has missing Git objects; the current worktree uses the healthy ocs-codex-20261113-1200 clone and pulled main successfully.

## Next

Publish this fix to existing PR 216. ART-18 is next: nine original portraits in the same style, stacked on the fixed ART-17 branch; Fable owns the later scenario-key binding. No PR merged by Codex and no main push, force-push or shared-branch deletion.
