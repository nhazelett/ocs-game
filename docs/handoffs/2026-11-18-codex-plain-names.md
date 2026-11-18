**Work session handoff** (Codex, 18 Nov 2026, plain-names fix pass)

Expected look of this build and why: no visible change from the DESK-100 and DESK-101 screenshots Fable accepted. Arrangement and supplier refusals remain with their disabled choices; a successful buy leaves its refreshed standing beside a quiet reset form. This pass changes test shutdown and CI diagnostics only.

## PRs reviewed this session

- PR 318 (DESK-100) and PR 320 (DESK-101): accepted Fable's 02:45 review. Both passed their visual review but crashed after `PLAIN_NAMES_SMOKE_PASS` in five of six hosted runs. The earlier ledger drain did not reach this separate process. The callback-capture lead remains unproven; no production callback was changed.
- Pulled main at b3b85ae in a clean isolated checkout after the older checkout failed fetching missing Git objects. Read the session/kickoff instructions, SPEC, ADRs 0001–0003, latest Fable and Codex handoffs, questions and decided objections. No unanswered Codex question or undecided objection needed a response. Two Codex PRs were open; the active session limit is five.

## Changed

- PlainNamesSmokeTest frees its shell, clears the managed reference and waits two frames as before; it now drains managed finalizers while Godot is alive and waits another frame before reporting success and quitting, matching CorSmokeTest. Its optional screenshot Image is explicitly disposed. All scenarios, forms, live days, debrief causes, naming and state-purity assertions remain.
- CI also rejects `Leaked unsafe reference` in the plain-names log even if the process exits 0, so a lucky non-crashing leak cannot pass. No smoke test, assertion or timeout was removed or relaxed.
- Merged main into the existing DESK-100 branch and integrated that repair into the existing DESK-101 branch, without code conflicts. Kept both backlog Status cells, Fable's headline/review records and all accepted screenshots. No Sim, scenario, asset, shared-contract or design-file change.

## Why

A pass line followed by native shutdown failure is a failed process, not a green smoke test. The long all-scenario plain-names replay lacked the managed-finalizer drain already used by the COR and award-context tests. Fable requested reliable process exits before merging either PR.

## Validated how

- Fresh `dotnet build src/LinesOfSupport.sln -m:1 --disable-build-servers` and `dotnet test src/LinesOfSupport.sln --no-build -m:1 --disable-build-servers`: all 921 tests pass (800 Sim, 121 harness). Sim/Harness source trees are identical across the repaired stack. Existing compiler/analyzer warnings remain.
- Game Debug builds and Godot editor import pass. Ten consecutive independent fixed plain-names processes exit 0, each checking 1,818 panel visits and 200,334 strings, with zero unsafe-reference or crash diagnostics. One baseline local run was clean too; local reproduction is not claimed.
- Final DESK-101 Game rebuild and five process-level regressions pass: PlainNames again, RequestBuyFit, AwardContext (including Pane), AwardChoices and Ledger. Every process exits 0 without the reported leaks or crash. These preserve the actual buy, exact independent Sim state, reset-form gating, supplier reasons and replay checks.
- Local tools: .NET SDK 8.0.428/runtime 8.0.34 and Godot 4.6.1 .NET, headless with Dummy audio. The older hosted crash log used .NET 11; hosted checks remain the verification of that environment. Windows execution and ordinary-laptop performance are unmeasured. Existing reviewed plain PNGs remain accurate.
- Scenario, provenance and conflict-marker validators, conflict-regression tests, complete CI script `bash -n` and `git diff --check` pass. Published Git trees are checked against local trees.

## Unresolved

Hosted CI is still running at publication. Initial repair runs: [DESK-100 44235779346](https://github.com/nhazelett/ocs-game/actions/runs/44235779346) and [DESK-101 44236029513](https://github.com/nhazelett/ocs-game/actions/runs/44236029513). The final CI-diagnostic/validation commit starts another run on each branch without another Game change; DESK-100's is [44236596864](https://github.com/nhazelett/ocs-game/actions/runs/44236596864). Fable should use the final branch runs and inspect their plain-names diagnostics before merging. No new FOR-NICK entry, question or objection.

## Next

Fable reviews/merges the repaired stack on reliable green CI; Codex merges no PR. There is no further unblocked ordered Codex item before the gate: DESK-98 waits for DESK-97, and DESK-97, DESK-94 and other Sim depth remain gated. The daily Fable play is due on or after 14:00 UTC on 18 Nov. Opened no new PR; fixed the existing two branches. No main push, force-push or shared-branch deletion.
