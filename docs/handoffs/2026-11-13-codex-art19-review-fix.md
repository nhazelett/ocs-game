**Work session handoff** (Codex, 13 Nov 2026, ART-19 fix pass)

Expected look of this build and why: the title's far sea fades directly into the paper, like its other three edges. The detached pale sky band is gone in Field atlas, Night watch and Tabletop. The harbor, title, copy and deployment actions stay in place.

## PRs reviewed this session

- One open Codex PR: [222, ART-19](https://github.com/nhazelett/ocs-game/pull/222). Its hosted CI was green and it was mergeable. Read Fable's [18:50 review](https://github.com/nhazelett/ocs-game/pull/222#issuecomment-6861955816): the sea and settings pass; the detached sky strip blocks merge. Accepted that finding and fixed it on the same branch.
- Pulled main `e6f0d709ccda062160a8aae60ca02cec552cce2a` into an isolated checkout, then merged it into codex/ART-19, preserving both histories. Read CODEX-SESSION, CODEX-KICKOFF, the required SPEC sections, ADR-0001 to 0003, DISSENT, backlog, QUESTIONS, FOR-NICK, all four decided objections and both agents' latest handoffs. No unanswered question or objection awaits Codex.
- The active delegation's five-open-PR limit takes precedence over the repository's ten-open wording. No new PR is needed for this fix pass.

## Changed

The shared sea shader now lets a caller omit its painted horizon. The title opts out because its steep camera looks down on the water; the map retains the default horizon and its existing palette. The four existing paper fades now blend sea directly to paper. No new model, texture, draw call or screen effect is added.

The title smoke fixture samples open sea above the island in every cold/saved look and rejects a detached sky band. Its screenshot setup also waits for the actual rendered 1280 by 800 image: X11's late initial configure otherwise gave a smaller physical image than the logical UI. This is confined to the screenshot fixture.

Updated only ART-19's Status cell. Fable's headline is retained. Six new full-screen native PNGs and three exact-pixel top-edge comparisons are below. No Sim, scenario, rule, golden or asset changes.

## Why

This answers the single blocking ART-19 review finding with Fable's first suggested option: drop the sky strip and let the far sea fade into paper. The optional closer island-shaped fade remains a later visual refinement; the row does not require it. This fix does not hold Nick's replay or unpause simulation depth.

## Validated how

- Required Release solution build passes with .NET SDK 8.0.428, one worker and shared compilation disabled: zero errors, eight existing warnings in unchanged Sim/test files. All 757 .NET tests pass (636 Sim, 121 Harness), none failed or skipped.
- Game Debug build passes; final incremental rebuild has zero warnings/errors. Godot 4.6.1 .NET import passes.
- Headless TitleArt, FrontDoor, Look, TabletopTheme and MapScenery suites pass. FrontDoor, Look and MapScenery emit ObjectDB exit warnings without failure or crash; final title-specific and theme suites exit cleanly.
- Native TitleArt passes on GL Compatibility / Mesa llvmpipe: all six cold/saved looks, the new no-sky-band assertion, moving sea, all four paper edges, real Continue, settings keyboard access, hidden-render pause and unchanged Sim. The initial native attempts stopped on a physical/logical viewport mismatch; the fixture now waits for the real review-size render before examining pixels. The successful capture run has no errors, only the driver's unsupported V-Sync warning.
- All six new 1280 by 800 PNGs were visually inspected and are below 400 KB. A pixel comparison of the left 480 by 800 region (title, copy, deployment actions and settings) is identical to the reviewed PR head in every state. The comparison crops copy screenshot pixels without resizing or repainting; labels identify before/fixed.
- Scenario validation passes: five scenario folders, one rules file, zero errors. Provenance, tracked conflict markers and whitespace checks pass.

Renderer/cost: GL Compatibility and the existing title viewport/4x MSAA remain. This fix only disables the horizon blend for the title; no additional viewport, model, texture, render pass or draw call. Fresh Windows execution and ordinary-laptop frame rate were not measured on this Linux/Mesa software-rendering machine.

| State | Before this fix | Fixed |
| --- | --- | --- |
| Cold, Field atlas | [PNG](images/2026-11-13-codex-art19-after-cold-look-0.png) | [PNG](images/2026-11-13-codex-art19-fixed-after-cold-look-0.png) |
| Cold, Night watch | [PNG](images/2026-11-13-codex-art19-after-cold-look-1.png) | [PNG](images/2026-11-13-codex-art19-fixed-after-cold-look-1.png) |
| Cold, Tabletop | [PNG](images/2026-11-13-codex-art19-after-cold-look-2.png) | [PNG](images/2026-11-13-codex-art19-fixed-after-cold-look-2.png) |
| Saved, Field atlas | [PNG](images/2026-11-13-codex-art19-after-saved-look-0.png) | [PNG](images/2026-11-13-codex-art19-fixed-after-saved-look-0.png) |
| Saved, Night watch | [PNG](images/2026-11-13-codex-art19-after-saved-look-1.png) | [PNG](images/2026-11-13-codex-art19-fixed-after-saved-look-1.png) |
| Saved, Tabletop | [PNG](images/2026-11-13-codex-art19-after-saved-look-2.png) | [PNG](images/2026-11-13-codex-art19-fixed-after-saved-look-2.png) |

Top-edge comparisons (reviewed PR head above, fixed below): [Field atlas](images/2026-11-13-codex-art19-top-edge-look-0.png), [Night watch](images/2026-11-13-codex-art19-top-edge-look-1.png), [Tabletop](images/2026-11-13-codex-art19-top-edge-look-2.png).

## Unresolved

Fable's visual re-review and hosted CI on the updated PR head remain required. No new FOR-NICK item or technical objection. ART-19 is the only current Codex row: older implemented rows already have merged PRs, ART-10 is a direction, ART-11 is Later, and the remaining depth work waits for the existing tutorial gate. No unblocked, unimplemented Codex item remains after this fix pass.

## Next

Fable reviews PR 222's new top edge and merges on its own schedule. Nick plays the tutorial again per docs/PLAYTEST.md; ART-19 does not hold it. Codex merged no PR, pushed nothing to main, force-pushed nothing and deleted no shared branch.
