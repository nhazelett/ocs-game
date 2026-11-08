# LinesOfSupport.Harness

Requires the .NET 8 SDK. Run these commands from the repository root:

```sh
dotnet build src/LinesOfSupport.sln -c Release
dotnet test src/LinesOfSupport.sln -c Release --no-build
dotnet run --project src/LinesOfSupport.Harness -- validate scenarios/_stub
dotnet run --project src/LinesOfSupport.Harness -- run _stub --seed 42 --policy do-nothing
dotnet run --project src/LinesOfSupport.Harness -- run spoke-b --log scenarios/spoke-b/golden/approach1-as-written.json
dotnet run --project src/LinesOfSupport.Harness -- batch _stub --seeds 200 --policies do-nothing,greedy,textbook
dotnet run --project src/LinesOfSupport.Harness -- batch spoke-b-90 --seeds 200 --json > batch.json
```

Use `--help` for all arguments. A scenario argument accepts a directory, a JSON file, or a folder name under `scenarios/`. Folder names resolve from the current working directory upward toward the repository root. Explicit paths work from other directories. Exit codes: 0 = success, 1 = invalid data or file error, 2 = invalid arguments. `run` defaults to seed 0 and policy `do-nothing`; `batch` defaults to all three policies and seeds 0 through 199. Integer seeds may be negative.

## Batch reports (M0-15)

`batch` uses the accepted ADR-0005 API: `ScenarioBundle.Load`, `Replay.Run` and a fresh `PolicyCatalog.Create` for each seed. All simulation and policy decisions remain in Sim. No placeholder scores or replacement simulation rules are used. A batch needs a real `scenario.json` and its shared rules; incomplete legacy fixtures produce an error.

The default report has one row per policy and exposed metric, with **Runs, Min, Median, Max** columns. Even-size medians average the two middle values. Policies stay in the requested order; metric names sort in ordinal order. All `session.Metrics` keys are included, including `day`, `mission.output`, costs, compliance, and site capability measures. Units stay exactly as exposed by Sim: mission values are fractions, named `*Cents` values are cents. Text uses a dot decimal separator and up to six fractional places. It makes no independent balance/pass/fail judgment.

`--json` is a value-less flag for `batch` and `crash-run`. Standard output is one JSON document with `scenario`, `rulesVersion`, `simulationStatus` (`simulated`), `firstSeed`, `lastSeed`, and `results`. Each result contains `policy`, `metric`, `runs`, `min`, `median`, `max`; values are JSON numbers without the text report's rounding. The seed range is inclusive, beginning at zero. JSON output contains no validation preamble or log text. Errors go to standard error with the existing nonzero exit code and no partial report. If metric keys change across seeds or a metric is non-finite, reporting fails explicitly rather than silently dropping samples.

## Run and replay (M1-01)

`run` loads the validated scenario/shared rules, plays the selected policy through `Replay.Run`, and prints `SessionReport.ToText`: timeline, capability-by-day strip, scorecard, gap chains, pass criteria and final state hash. Defaults remain seed 0 and `do-nothing`. `PlaceholderRun` has been removed. Both run and batch now use the real Sim.

`run <scenario> --log <input-log.json>` uses `InputLog.Load` and `Replay.Run(bundle, log)`. The log supplies the seed, prep choices and dated actions; Sim checks scenario and rules identity. The log is never overwritten. Combining `--log` with `--seed` or `--policy` is a usage error, so no supplied option is silently ignored. Report titles follow the same convention as the reviewed golden reports. Errors produce no partial report. The accepted action/input-log contract remains owned by Sim (ADR-0005).

## Validation

Before M0-04, checks are intentionally limited: valid JSON object, non-empty string `id`, positive Int32 `days`, and non-empty array `sites`. Existing `rules.json` and `theme.json` must also parse. This is a temporary input check, not the authoritative scenario schema. The command reports fallback validation explicitly.

JSON schemas in the nearest ancestor's `schema/` directory (normally `scenarios/schema/`) validate each document with JsonSchema.Net. Recognized names are `<document>.json` or `<document>.schema.json`; other files are available for `$ref` resolution. Draft 2020-12 is the default unless the schema declares a supported dialect. Local references and declared schema IDs resolve within that catalog; schemas are not fetched from the network. Format assertions, including rules `asOf` dates, are enabled. Broken or unrecognized schema catalogs fail instead of silently falling back.

Fable's M0-04 contract uses shared files under `scenarios/rules/`. The harness selects exactly `rules-<scenario.rulesVersion>.json` and requires that file's `rulesVersion` to match. It also validates every shared rules file and rejects duplicate versions. A matching version inside an arbitrarily named file is insufficient. `theme.json` beside the scenario is optional and validated when present. A sibling `rules.json` is not a rules source in schema mode. Cross-checks cover references and event windows from Fable's Python reference validator, plus the schema's declared unique-ID invariant, initial arrangements, vehicle references, and SCO known vehicles. No contract or production schema changes were needed.

`LinesOfSupport.Harness.Tests` keeps harness tests separate from Fable's simulation tests. Temporary schema fixtures exercise generic validation; integration tests copy Fable's real schemas, shared rules and stub into temporary directories and reject mutated invalid versions. `scenarios/_stub/` uses Fable's M0-04 data and remains a test fixture, not a playable scenario. Batch tests compare every reported statistic to independent Sim runs, including odd/even sample counts with seeded variation, all-policy defaults, JSON parsing/repeatability, nested shared-rules resolution, locale-independent text, and error behavior. The production scenario, schema, rules and Sim files are never edited by these tests.


## Crash run (M3-10)

```sh
dotnet run --project src/LinesOfSupport.Harness -- crash-run spoke-b,spoke-b-90
dotnet run --project src/LinesOfSupport.Harness -- crash-run spoke-b,spoke-b-90 --games 1000 --json > crash-run.json
dotnet run --project src/LinesOfSupport.Harness -- crash-run spoke-b,spoke-b-90 --games 6 --first-seed 23
```

The default is exactly 1,000 games across every named scenario and all three Sim policies. A deterministic round-robin schedule assigns one game to each scenario/policy pair at seed 0, then seed 1, and so on. At least one game per pair is required; extra games go to the first pairs in the listed scenario and policy order. `--first-seed` is an Int32 starting seed (negative values are accepted); generated seeds use Int64 arithmetic. Every game gets a fresh Sim policy instance. Scenario arguments are comma-separated folder names, directories or scenario.json paths, resolved by the same schema/rules loader as run and batch. Duplicate scenario IDs are refused.

After every simulated day the runner checks the invariants already exercised by Sim's InvariantTests: nonnegative buffers, cash, funding obligations and costs; condition bounds; funding/UCA caps; active arrangement periods; arrangement cause presence and valid, unique cause-record references. All exposed metrics must be finite, also checked at creation. These are read-only assertions; no simulation state is repaired or independently advanced by the checker.

The runner continues after a game's exception or failed check, so the report covers the entire requested matrix. Failures include scenario, rules version, policy, seed, day, phase, category and exception message. Exit 1 means any game failed (or input data could not be loaded); exit 2 means invalid arguments. A successful simulation is a robustness pass regardless of whether its mission score is good. The text summary table reports game/pass/fail counts, seed ranges, checked days and mission-output min/median/max for successful games only. JSON has the same rows plus detailed failures; it is produced even when a simulated game fails. Scenario-load failures happen before any games and produce no partial success report.

After merge, **Actions > Crash run > Run workflow** runs the default 1,000-game matrix on demand. Its game count is configurable. The job fails if any game fails, writes a summary table to the run, and uploads `crash-run-<commit>` JSON even after a simulation failure. It has read-only repository permissions and needs no Godot, LFS assets or secrets. No nightly schedule is created.
