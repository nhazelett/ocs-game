# Scenarios

One folder per scenario holding `scenario.json` (and optionally `theme.json`). Rules shared across scenarios live in `rules/` and are versioned; a scenario names the rules version it was written against. Schemas are in `schema/`. `SKELETONS.md` is the approved prose source for the five 1.0 scenarios.

- `schema/scenario.schema.json`, `rules.schema.json`, `theme.schema.json`: JSON Schema draft 2020-12, v0.
- `rules/rules-2025-10.json`: baseline rules (deployed, outside-the-U.S. contingency thresholds effective 1 Oct 2025; UCA limits; instrument parameters; consequence curves). Every value under `instruments`, `prepChoices`, `aggregateRates`, `model` and `sco` is a tunable.
- `_stub/`: a minimal scenario that passes the schema, for the harness until `spoke-b/` lands. Not playable.

Validate everything: `python3 tools/validate/validate.py` (CI runs it on every pull request). The harness `validate` verb mirrors the same checks in C#.

Conventions: money in integer cents; days are integers from day 1 (day 0 is the pre-deployment window); ids are `camelCase` and unique within their array; scope line ids come from the capability card catalog (`cards.json`, M1); no real place names, no service marks, no named operations.
