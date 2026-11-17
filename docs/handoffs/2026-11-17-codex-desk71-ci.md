**Work session handoff** (Codex, 17 Nov 2026, PR 296 CI fix pass)

Expected look of this build and why: no visible change. This fixes the visit-preview checks in MapLayoutSmokeTest for the DESK-71 cost line already below the button. [Native tutorial finance preview](images/2026-11-17-codex-desk71-ci-visit.png) is an inspected, plain 1280 by 800 PNG under 400 KB.

## PRs reviewed this session

[296, DESK-71](https://github.com/nhazelett/ocs-game/pull/296). Its first [hosted CI run, 467](https://github.com/nhazelett/ocs-game/actions/runs/44082110370), passed test, scenarios and provenance, then failed godot-shell at MapLayoutSmokeTest lines 54 to 55. Fetched the current main and PR heads: main is still eb3f005; the PR head is c3dc0aa. No conflict or review objection was found. No PR was merged.

## Changed

- MapLayout selects the current contact's preview label by its existing `visit_timing` metadata, replacing its search for the obsolete `This takes ` sentence. It checks the exact displayed result day/range from the Sim preview.
- The empty-queue case still checks finance before the click. After finance is sent, its duplicate action must be disabled; a different, accepted SCO question is previewed behind it, with the exact queued hours and result range. Navigation and preview reading must leave Sim state unchanged.
- All original map inventories, wrapping, equipment, card/viewport containment, 8 to 15 capability rows, and multi-scenario checks remain. No production Game, Sim, scenario, asset or workflow changes.

## Why

DESK-71 changed the visit sentence and suppresses a result forecast on a refused duplicate. The older map/layout smoke test still depended on the old prose and tried to read timing from that duplicate. The original selective local smoke pass missed this unchanged test. This fix restores its intended empty/nonempty queue coverage using an available action, with a stable label binding.

## Validated how

- Reproduced the hosted failure locally on c3dc0aa before editing. The repaired MapLayout test passes headless.
- Ran the exact complete Build, import and smoke-test the shell command block extracted from ci.yml: Game build, import, all 63 Godot invocations, kit validator and Python compile check pass; exit 0. This includes the smoke scenes after MapLayout that the failed hosted job never reached.
- Native Godot 4.6.1 .NET MapLayout passes on authenticated Xvfb/llvmpipe at 1280 by 800. The new visit screenshot was inspected.
- Scenario, provenance, conflict-marker and whitespace checks pass. Windows execution and ordinary-laptop performance were not measured.
- Publish the fix on the existing codex/DESK-71 branch with an ordinary fast-forward, checking the remote blobs/tree against the tested local tree. Verify the new hosted CI on that exact commit before the final report; its run link will be recorded in the PR description and final report.

## Unresolved

The hosted run for this fix and Fable review follow publication. No new question, objection or FOR-NICK entry.

## Next

Finish hosted CI verification for PR 296. DESK-68 and DESK-83 remain the next backlog work for a later session. This pass opens no PR, merges nothing, pushes nothing to main, force-pushes nothing and deletes no branch.
