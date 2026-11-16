**Work session handoff** (Codex, 16 Nov 2026, ART-20)

Expected look of this build and why: larger matte 3D heads with visible tied, parted and curled hair, different builds and civilian necklines. Cards, assignment choices, the strip and face picker use the same revised cast. The map and desk layout are unchanged.

## PRs reviewed this session

[PR 261, ART-20](https://github.com/nhazelett/ocs-game/pull/261) is the only open Codex PR. Its initial submission passed CI and was mergeable; Fable requested clearer hair and identities, closer framing and full scenario sheets at 48 px. Current main `908a0c4` is integrated with both sides retained. The active five-open-PR limit applies.

## Changed

Publish the completed review fixes to the same feature branch. All 27 revised portraits, the Blender recipe and identity audit, framing regression, ten native review PNGs and provenance are included. The full technical account and matched before/after images are in [the ART-20 fix handoff](2026-11-15-codex-art20-fix.md). The public backlog retains main's existing review state. No Sim, scenario, schema, rules or golden change.

## Why

Fable's review found hat-like hair, repeated broad builds, small heads and several look-alike characters. The retained original kit now separates those identities at the actual UI sizes; every scenario/default-team name and portrait key is audited in the kit README.

## Validated how

The game/assets/tests and review images are unchanged from tested local implementation `c3d18a7`: Release solution build, all 808 .NET tests (687 Sim, 121 Harness), Game build, native Portrait/Team/DeskHierarchy/Look (all looks), affected headless suites and the external packed-cast checks passed. Fresh scenario, provenance, tracked conflict-marker and whitespace validators pass. Both 48 px scenario sheets were inspected again. All ten new plain 1280x800 PNGs are under 193 KB. Upload hashes and the final remote tree are checked against the prepared local tree.

Godot 4.6.1 .NET OpenGL Compatibility / Mesa llvmpipe; Blender 4.3.2 Cycles renders offline. Revised PNGs total 2,537,100 bytes; RGBA8 images with mipmaps total 11.25 MiB before engine overhead. No runtime character meshes/lights. Fresh Windows execution and ordinary-laptop performance remain unmeasured. Detailed warnings and limits are retained in the fix handoff.

## Unresolved

New hosted CI and Fable's art re-review remain required after this branch update. The art bar is judged from the revised screenshots; raster uniqueness alone does not establish recognizability.

## Next

Update PR 261's description to the revised evidence and check hosted CI. Fable then re-reviews. No other unblocked Codex row remains; the tutorial replay gate and paused Sim-depth work retain their scope. No merge, main push, force-push or shared branch deletion.
