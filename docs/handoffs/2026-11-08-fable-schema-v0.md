**Work session handoff**

Expected look of this build and why: no build. Data contracts and a validator. Nothing a player sees.

## Changed
- `scenarios/schema/`: `scenario.schema.json`, `rules.schema.json`, `theme.schema.json` (JSON Schema 2020-12, v0).
- `scenarios/rules/rules-2025-10.json`: baseline rules. Thresholds and UCA limits are sourced; everything else is a first-pass tunable from SPEC 4.1 to 4.4.
- `scenarios/_stub/`: minimal passing scenario and theme for the harness until `spoke-b/` exists.
- `tools/validate/validate.py`: schema plus cross-reference checks (ids resolve, capabilities exist in rules, event windows inside the scenario, rulesVersion matches). Reference implementation for the Harness `validate` verb.
- `.github/workflows/ci.yml`: new `scenarios` job runs the validator on every pull request.
- `docs/assumptions.md`: every simplification beside the real rule, with a revisit milestone.
- `scenarios/README.md`: conventions.

## Why
Fable's Sim and Codex's Harness both load these files; the schema is the contract between them (ADR-worthy changes only from here). The validator caught a real error in the stub on first run (an event outside the scenario window), which is the point of having it.

## Validated how
`python3 tools/validate/validate.py` passes locally; schemas pass `check_schema`; CI job added and expected green.

## Unresolved
- Codex's harness (PR 4, merged first) expects a sibling `rules.json` in each scenario folder; this PR makes rules shared by version instead and removes the empty `_stub/rules.json`. Until Codex does M0-08b, `LinesOfSupport.Harness validate scenarios/_stub` reports a missing companion; harness unit tests are unaffected (own fixtures). Q4 and Q5 answered in `backlog/QUESTIONS.md`.
- Scope line ids (`generation`, `fuelSupply`, `operator`, `maintenance`...) are free strings until the capability card catalog (`cards.json`) lands in M1; the validator will then check them.
- `effect` and `predicate` kinds are open strings in v0; the Sim rejects kinds it does not implement, and the validator will learn the list once the Sim has it.
- Capability demand constants in `rules.model.capabilities` are rough and only matter relative to vendor capacities; both get tuned together in M1.

## Next
Fable: M0-05, `LinesOfSupport.Sim` domain model and day loop with tests, then M0-06 Spoke B scenario and golden logs.
Codex: M0-08 can now point `validate` at `scenarios/_stub` and the schema files.
