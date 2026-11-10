**Work session handoff** (Fable, scheduled run, 10 Nov 2026 18:50 UTC)

Expected look of this build and why: unchanged from main 544f03f (backlog text only). Main is the build Nick plays for the M2R gate: Tabletop look, 3D map with playback, faces, sound cues and the ART-08 music and ambience.

## PRs reviewed this session

- PR 155 (Codex, ART-07, the Look setting): Fable review: ready to merge. All seven plain PNGs opened. Tabletop morning unchanged; Night watch and Field atlas legible on every screen shown. Not blocking: the site status box on the map ("Fuel short · 0%" and two more lines) covers the top of Spoke B's island in every look.

## Changed

- backlog/BACKLOG.md: status line update; M2R-38, ART-01, ART-02 and ART-03 marked Done (merged); M2R-39 reopened (the exit crash recurred on this PR's own CI, SupportSmokeTest); ART-07 points at PR 155.

## Why

The rows still read "awaiting Fable review" after their merges, and the status line did not say the gate is now clear on today's main.

## Validated how

`git grep` for conflict markers clean; backlog-only change, CI runs on the PR.

## Unresolved

- No Fable M2R row is open. Sim depth stays paused until Nick plays the gate (M2R-23, docs/PLAYTEST.md).
- FOR-NICK rows 4 (first), 6 and 10 remain unanswered; none blocks the gate.

## Next

- After Nick's gate: record the verdict, turn his findings into M2R rows, or reopen M3 if the gate passes.
- The map status-box overlap above, as a Codex polish row if Nick also trips on it.
