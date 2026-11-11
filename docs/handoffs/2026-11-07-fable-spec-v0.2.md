**Work session handoff**

Expected look of this build and why: no build. This PR is documents only: the specification, three proposed decision records, the M0 backlog and the open questions. Nothing runs yet, by design; implementation waits for Nick's review of the spec.

## Changed

- `docs/SPEC.md`: the authoritative specification, v0.2, exported from the design session of 7 Nov 2026. The three diagrams are rendered as Mermaid so GitHub shows them.
- `docs/decisions/ADR-0001-stack.md`, `ADR-0002-determinism.md`, `ADR-0003-single-player-two-seats.md`: proposed, not accepted; they become accepted when the spec PR merges.
- `backlog/BACKLOG.md`: status line and the M0 items with owners and acceptance criteria, all `Not started`.
- `backlog/QUESTIONS.md`: Q1 game title, Q2 error-rate placeholders, Q3 scenario skeletons in prose.

## Why

The repo needs the spec before either agent writes code, and the decisions that shape the code (stack, determinism rules, single-player two-seat model) need to be reviewable in the same PR as the reasoning behind them.

## Validated how

- Spec exported from the live doc at rev 39 and diffed by eye against the doc's section list (bottom line, 1 to 13, with 4.1 to 4.4 inserted after section 4).
- Mermaid blocks checked for syntax by rendering in a Markdown preview.
- CI `test` job skips cleanly because `src/LinesOfSupport.sln` does not exist; `provenance` passes because `assets/` holds only `PROVENANCE.md`.

## Unresolved

- Game title (Q1). Project code names stay `LinesOfSupport.*` until it is set; renaming is a one-commit change before M0 code lands.
- Scope section 4.2's prep-day costs and section 4.1's lead times are first-pass numbers and are marked as tunables in the spec.
- Section 10 is the one to read for what has to be true before a Steam page exists; the bottom line says the same in fewer words. Wording to be tightened in v0.3.

## Next

- Nick reviews and merges the spec PR, answers Q1 to Q3.
- Fable (M0): scenario schema v0, `assumptions.md`, `LinesOfSupport.Sim` domain model and day loop with tests, Spoke B `scenario.json`, four approach input logs as golden files, view-model contracts.
- Codex (M0): solution and `LinesOfSupport.Harness` scaffold, CI build of the solution, Godot 4.6 .NET project referencing Sim, first bpy kit experiment, provenance check hardening. Codex's kickoff brief is `docs/CODEX-KICKOFF.md` in this PR.
