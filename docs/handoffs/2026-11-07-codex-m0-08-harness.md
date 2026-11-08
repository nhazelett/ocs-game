**Work session handoff**

Expected look of this build and why: console text only, no game window or art. A valid stub prints its 30-day count; batch prints three equal day-count rows marked "not simulated." This is the M0 foundation, not evidence of balanced policies or working simulation rules.

## Changed

- Read `docs/CODEX-KICKOFF.md`, the requested specification sections, and accepted ADR-0001 to ADR-0004. Updated local main, then worked on `codex/m0-foundation`.
- Created `src/LinesOfSupport.sln`, targeting .NET 8, with the empty Sim library, one Sim assembly-load placeholder test, Harness, and a separate Harness.Tests project. Sim has no third-party or Godot dependency. Empty Sim/Sim.Tests bootstrap is the explicit M0-08 kickoff assignment; Fable owns all subsequent simulation work.
- Added `run`, `validate`, and `batch` commands with explicit errors and exit codes. The day counter is internal to Harness; policies and seed effects are no-ops until Fable's contracts arrive. `--log` parses an existing JSON file, never overwrites it, and explicitly reports that it does not replay actions.
- Added fallback input checks and JSON Schema validation for when M0-04 arrives, including companion files and local/declared-ID references. Normalized file URIs to preserve reference fragments on Windows. Pinned JsonSchema.Net 8.0.5, whose published package license is MIT; newer package license terms differ.
- Added the explicitly authorized `scenarios/_stub/` fixture and command documentation. Schema test fixtures are temporary and do not define Fable's production schema.
- Removed CI's missing-solution skip; every PR now builds and tests the actual solution. Added `global.json` for .NET 8. Updated backlog and recorded Q4/Q5 for Fable.

## Why

M0-08 needs a runnable, independently tested command-line foundation while M0-04 to M0-07 are still pending. Keeping placeholder behavior in Harness avoids inventing a shared simulation API or writing Fable's rules. The extra harness test project keeps command-line tests out of Fable's test component.

## Validated how

- .NET SDK 8.0.427 installed for local verification at `C:\Users\nickh\.codex\tools\dotnet8\dotnet.exe`; no persistent PATH change. This machine previously had only the runtime. Contributors need a .NET 8 SDK on PATH or can invoke that local executable.
- Release solution build: zero warnings/errors. Release tests: 43 passed (42 harness cases plus one Sim placeholder). Tests cover bad JSON, missing/invalid fields and files, CLI argument errors, repeatability, log parsing, all three policies across 200 seeds, schema-only failures, malformed schemas, reference resolution, and rules/theme companions.
- Executed real console commands: `validate scenarios/_stub`, `run _stub --seed 42 --policy do-nothing`, and `batch _stub --seeds 200 --policies do-nothing,greedy,textbook`. All succeed; run reports 30 days and batch reports 200 runs per policy with no simulated outcomes.
- [PR 4](https://github.com/nhazelett/ocs-game/pull/4): implementation commit `a3d87ee` passed GitHub `test` and `provenance` on Ubuntu ([CI run](https://github.com/nhazelett/ocs-game/actions/runs/42446632617)). The provenance check is unchanged; no asset files were added. Export was skipped as intended on a PR and remains the existing tag-only placeholder. Final bookkeeping changes trigger the same checks again; use the PR for current status.

## Unresolved

- Q4: Fable must confirm final schema names and whether shared rules are loaded by reference or as a sibling. The stub is not an authoritative scenario and must be replaced or updated when M0-04 lands.
- Q5: real input replay, policy behavior, mission metrics and the seeded simulation session await Fable's ADR-backed contracts. M0-08 does not claim M1 balance or golden-file acceptance.
- GitHub Free does not enforce private-branch protection. Main remains PR-only by agreement; Nick merges. No work on M0-09 to M0-11 in this PR.

## Next

Nick reviews and merges the M0-08 PR once `test` and `provenance` pass. Fable delivers M0-04 to M0-07 and answers Q4/Q5. Codex takes M0-09 (Godot shell) as a separate backlog item and PR; integrate Harness with real simulation contracts at M1.
