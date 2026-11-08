# LinesOfSupport.Harness

Requires the .NET 8 SDK. Run these commands from the repository root:

```sh
dotnet build src/LinesOfSupport.sln -c Release
dotnet test src/LinesOfSupport.sln -c Release --no-build
dotnet run --project src/LinesOfSupport.Harness -- validate scenarios/_stub
dotnet run --project src/LinesOfSupport.Harness -- run _stub --seed 42 --policy do-nothing
dotnet run --project src/LinesOfSupport.Harness -- batch _stub --seeds 200 --policies do-nothing,greedy,textbook
dotnet run --project src/LinesOfSupport.Harness -- batch spoke-b-90 --seeds 200 --json > batch.json
```

Use `--help` for all arguments. A scenario argument accepts a directory, a JSON file, or a folder name under `scenarios/`. Folder names resolve from the current working directory upward toward the repository root. Explicit paths work from other directories. Exit codes: 0 = success, 1 = invalid data or file error, 2 = invalid arguments. `run` defaults to seed 0 and policy `do-nothing`; `batch` defaults to all three policies and seeds 0 through 199. Integer seeds may be negative.

## Batch reports (M0-15)

`batch` uses the accepted ADR-0005 API: `ScenarioBundle.Load`, `Replay.Run` and a fresh `PolicyCatalog.Create` for each seed. All simulation and policy decisions remain in Sim. No placeholder scores or replacement simulation rules are used. A batch needs a real `scenario.json` and its shared rules; incomplete legacy fixtures produce an error.

The default report has one row per policy and exposed metric, with **Runs, Min, Median, Max** columns. Even-size medians average the two middle values. Policies stay in the requested order; metric names sort in ordinal order. All `session.Metrics` keys are included, including `day`, `mission.output`, costs, compliance, and site capability measures. Units stay exactly as exposed by Sim: mission values are fractions, named `*Cents` values are cents. Text uses a dot decimal separator and up to six fractional places. It makes no independent balance/pass/fail judgment.

`--json` is a value-less flag for `batch` only. Standard output is one JSON document with `scenario`, `rulesVersion`, `simulationStatus` (`simulated`), `firstSeed`, `lastSeed`, and `results`. Each result contains `policy`, `metric`, `runs`, `min`, `median`, `max`; values are JSON numbers without the text report's rounding. The seed range is inclusive, beginning at zero. JSON output contains no validation preamble or log text. Errors go to standard error with the existing nonzero exit code and no partial report. If metric keys change across seeds or a metric is non-finite, reporting fails explicitly rather than silently dropping samples.

## Run placeholder behavior (pending M1-01)

The separate `run` verb still counts from day 0 to the scenario's `days` value and labels itself as an M0 placeholder. In that verb all three policy names do nothing and seeds have no effect. Connecting `run` and text replay/reporting is the separate M1-01 item; `batch` already uses the real Sim.

`run <scenario> --log <input-log.json>` reads an existing input log and checks its JSON syntax. It never overwrites the file. Its actions are **not replayed**, and the command says so. No input-log shape or simulation-facing interface is defined here; those belong to Fable. The harness-only placeholder can be replaced once the contracts arrive.

## Validation

Before M0-04, checks are intentionally limited: valid JSON object, non-empty string `id`, positive Int32 `days`, and non-empty array `sites`. Existing `rules.json` and `theme.json` must also parse. This is a temporary input check, not the authoritative scenario schema. The command reports fallback validation explicitly.

JSON schemas in the nearest ancestor's `schema/` directory (normally `scenarios/schema/`) validate each document with JsonSchema.Net. Recognized names are `<document>.json` or `<document>.schema.json`; other files are available for `$ref` resolution. Draft 2020-12 is the default unless the schema declares a supported dialect. Local references and declared schema IDs resolve within that catalog; schemas are not fetched from the network. Format assertions, including rules `asOf` dates, are enabled. Broken or unrecognized schema catalogs fail instead of silently falling back.

Fable's M0-04 contract uses shared files under `scenarios/rules/`. The harness selects exactly `rules-<scenario.rulesVersion>.json` and requires that file's `rulesVersion` to match. It also validates every shared rules file and rejects duplicate versions. A matching version inside an arbitrarily named file is insufficient. `theme.json` beside the scenario is optional and validated when present. A sibling `rules.json` is not a rules source in schema mode. Cross-checks cover references and event windows from Fable's Python reference validator, plus the schema's declared unique-ID invariant, initial arrangements, vehicle references, and SCO known vehicles. No contract or production schema changes were needed.

`LinesOfSupport.Harness.Tests` keeps harness tests separate from Fable's simulation tests. Temporary schema fixtures exercise generic validation; integration tests copy Fable's real schemas, shared rules and stub into temporary directories and reject mutated invalid versions. `scenarios/_stub/` uses Fable's M0-04 data and remains a test fixture, not a playable scenario. Batch tests compare every reported statistic to independent Sim runs, including odd/even sample counts with seeded variation, all-policy defaults, JSON parsing/repeatability, nested shared-rules resolution, locale-independent text, and error behavior. The production scenario, schema, rules and Sim files are never edited by these tests.
