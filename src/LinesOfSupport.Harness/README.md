# LinesOfSupport.Harness

Requires the .NET 8 SDK. Run these commands from the repository root:

```sh
dotnet build src/LinesOfSupport.sln -c Release
dotnet test src/LinesOfSupport.sln -c Release --no-build
dotnet run --project src/LinesOfSupport.Harness -- validate scenarios/_stub
dotnet run --project src/LinesOfSupport.Harness -- run _stub --seed 42 --policy do-nothing
dotnet run --project src/LinesOfSupport.Harness -- batch _stub --seeds 200 --policies do-nothing,greedy,textbook
```

Use `--help` for all arguments. A scenario argument accepts a directory, a JSON file, or a folder name under `scenarios/`. Folder names resolve from the current working directory upward toward the repository root. Explicit paths work from other directories. Exit codes: 0 = success, 1 = invalid data or file error, 2 = invalid arguments. `run` defaults to seed 0 and policy `do-nothing`; `batch` defaults to all three policies and seeds 0 through 199. Integer seeds may be negative.

## M0 placeholder behavior

Sim is an empty, compiling class library owned by Fable. The harness currently loads a scenario, counts from day 0 to its `days` value, and reports the count. All three policy names do nothing; seeds have no effect. The batch spread reports only days advanced. There is no mission score, policy comparison, balance claim, authoritative state or replay yet.

`run <scenario> --log <input-log.json>` reads an existing input log and checks its JSON syntax. It never overwrites the file. Its actions are **not replayed**, and the command says so. No input-log shape or simulation-facing interface is defined here; those belong to Fable. The harness-only placeholder can be replaced once the contracts arrive.

## Validation

Before M0-04, checks are intentionally limited: valid JSON object, non-empty string `id`, positive Int32 `days`, and non-empty array `sites`. Existing `rules.json` and `theme.json` must also parse. This is a temporary input check, not the authoritative scenario schema. The command reports fallback validation explicitly.

Once JSON schemas exist in the nearest ancestor's `schema/` directory (normally `scenarios/schema/`), JsonSchema.Net validates `scenario.json` and the companion `rules.json` and `theme.json` against their corresponding schemas. Recognized names are `<document>.json` or `<document>.schema.json`; other files are available for `$ref` resolution. Draft 2020-12 is the default unless the schema declares a supported dialect. Local references and declared schema IDs resolve within that catalog; schemas are not fetched from the network. A companion file is required if its schema exists. Broken or unrecognized schema catalogs fail instead of silently falling back. Confirm final file names and log/policy contracts with Fable in `backlog/QUESTIONS.md`.

`LinesOfSupport.Harness.Tests` keeps harness tests separate from Fable's simulation tests. Its schema fixtures live only in temporary test directories; no production schema is defined by this item. `scenarios/_stub/` is a clearly marked harness fixture authorized by the kickoff brief and will need updating or replacement when M0-04 lands.
