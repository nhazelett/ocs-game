**M2R-26 morning and preparation clarity**

Expected look of this build and why: the first day reads "Morning of day 1", shop hours are spoken for in words, and the green cash bar visibly fills at the labelled full balance. Preparation and Developer tools checkboxes have bright outlines and a gold check. Actual screenshots: [Before you leave](images/2026-11-09-codex-m2r-26-prep.png), [first morning](images/2026-11-09-codex-m2r-26-day1.png). Both plain PNG, under 120 KB, not LFS.

## PRs reviewed this session

No open PRs at the start of implementation. Main includes the merged after-action PR 130 and Fable's PR 131, including M2R-29 and FOR-NICK #5's publishing approval. Nick explicitly approved pushing M2R-26 and opening its PR in this chat as well. No merge was performed by Codex.

## Changed

Published the M2R-26 item from the existing remote claim, merged forward to current main 26855e1 without rewriting history. Recovery caveat: that remote contained only the starting claim, and the earlier run's unpublished files were unavailable from this checkout and accessible local history. Recreated the same backlog requirements on current main and reran validation; this is not represented as a byte-for-byte recovery of the earlier payload.

Morning, completed-day and inbox wording use the existing Sim day values. Empty and occupied shop queues read naturally, with actual queued/capacity values and no new time rules. Cash remains the actual cash/full-balance gauge, with the comparison in its visible label and an explicit green fill. A root theme supplies generated runtime checkbox icons for both states, including disabled states; no asset files are introduced. Existing Session/Save assertions now expect the clearer wording. Added ChromeSmokeTest to CI.

## Why

The first strip the player sees should explain the current morning, work already spoken for and cash. The preparation page must visibly distinguish choices that are checked from choices left open. This completes the existing M2R-26 scope; no design, scenario or simulation changes.

## Validated how

.NET 8 Release solution build and all 611 tests pass (490 Sim, 121 Harness). Godot 4.6.1 .NET Game build passes. Five scenario folders validate, provenance and conflict-marker checks pass, and all 72 Python tests pass.

New ChromeSmokeTest passes rendered: normal New game, authored checked/unchecked prep defaults, first morning, actual full cash, on-screen Next day, an actual finance visit's queued hours compared to direct Sim, next morning and completion. It checks that rendering does not change the state hash. Screenshots were rendered and visually inspected at 1280 by 800. Shell, Session, FrontDoor, Turnover, Prep, Save, PlainNames and Debrief native regressions pass; PlainNames scans 2,979 panel visits and 145,989 strings.

## Unresolved

Fresh solution build has seven inherited warnings in Fable's components: four nullable vendor-field warnings in Session.Map.cs and three xUnit collection-size analyzer warnings in TurnoverTests.cs. No errors; Game adds no build warnings. Original cloud-only payload remains unavailable, so provenance of the reconstruction is explicitly recorded above. No new FOR-NICK entry or approval request; #5 is answered and this publication fulfills it.

## Next

Fable reviews and merges the PR under the existing workflow, then performs the remaining pre-gate walk when its queued prerequisites are satisfied. Nick's cold playtest remains the gate; this PR does not declare the game ready or start paused simulation work.
