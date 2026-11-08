## Title
ADR-0002: The simulation is deterministic from (scenario, rules version, seed, input log)

## Status (Proposed / Accepted / Superseded)
Accepted (spec v0.2 PR 1 merged 7 Nov 2026)

## Context
Reproducible scenarios are needed for instruction (one seed code for a class), testing (golden files), and the counterfactual debrief (re-run from day N with one decision changed). See SPEC.md sections 4 and 8.

## Options considered
1. Best-effort determinism with floats and a single RNG.
2. Strict determinism: per-subsystem seeded generators, integer or fixed-point arithmetic for outcome-bearing quantities, fixed subsystem update order, no wall clock, no dictionary-order dependence.

## Decision
Option 2, enforced by tests: golden-file hashes of final state per (scenario, seed, input log); property tests for invariants (buffers never negative, obligations never above caps, every state change carries a CauseRecord). Saves are the input log plus a snapshot for quick resume; loading replays the log.

## Consequences
- Adding a random draw to one subsystem does not shift the others.
- The presentation layer may use floats freely because it never writes to state.
- Rule changes that alter a golden hash are reviewed on purpose.
- Slightly more discipline in the sim code; worth it.

## Date
2026-11-07

## Author
Fable, for Nick Hazelett
