**Work session handoff**

Expected look of this build and why: console output only. The harness now validates Fable's real schema stub and its shared rules. All simulation policies remain no-op placeholders; no game window is added by this integration.

## Changed

- Reviewed Fable schema work and incorporated its revised PR 6 from main, including Q4/Q5 answers. Fable's schemas, baseline rule values, scenario/theme JSON, assumptions and Python validator are unchanged in this PR. Codex's obsolete sibling rules fixture has been removed on main.
- M0-08b follows the M0-04 contract: validate shared `scenarios/rules/*.json`, select by rulesVersion, reject missing or duplicate versions, and treat theme.json as optional. Enabled JSON Schema format checks.
- Added C# cross-reference checks matching the Python reference, plus declared ID uniqueness, initial-arrangement references, arrangement vehicle references, and SCO known vehicles. These checks read data only and do not introduce a simulation API.
- Added integration tests using copies of Fable's actual data. CI's test job now also invokes Harness validate on the repository stub. Implemented Q4's loading convention and preserved Fable's proposed Q5 API; executable Sim contracts still await ADR-0005.

## Why

Fable's shared-rules and optional-theme design supersedes M0-08's temporary sibling-file assumptions. The integration makes the real harness command agree with the merged schema and the reference validator.

## Validated how

- .NET 8 Release tests: 62 passed (61 Harness, one Sim placeholder).
- Real `Harness validate scenarios/_stub`: valid, 30 days, two sites, repository schema mode. Python reference validator: one scenario, one rules file, zero errors.
- Negative integration cases reject unknown/missing/duplicate rules, bad dates and thresholds, dangling IDs, duplicate IDs, reversed/out-of-scenario event windows, and missing follow-ons. Theme omission passes; malformed themes fail.
- No production schemas or simulation code were changed. CI test/scenarios/provenance are required to pass before merge. User authorized merging and proceeding in chat, superseding the earlier Nick-only merge rule for this work.

## Unresolved

- Q5: session, policy, replay, metric and Game view-model contracts remain with Fable. Day counting is still a Harness-only placeholder.
- No change to the baseline contracting rule values was attempted in this code integration review; regulatory-baseline research remains Fable's M1 responsibility.

## Next

Merge the green integration PR; then M0-09 builds and tests the Godot 4.6 .NET shell in a separate PR. Fable continues M0-05 to M0-07 from updated main.
