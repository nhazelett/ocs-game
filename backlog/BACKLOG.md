Status against plan: M0 in progress. M0-08 merged (PR 4); Fable M0-04 (PR 5) integrated on codex/m0-schema-integration with shared-rules loading and 62 passing tests. Next: merge schema integration, then Codex M0-09; Fable M0-05 to M0-07.

| ID | Item | Acceptance criteria | Owner (Fable / Codex / Nick) | Component boundary | Status |
| --- | --- | --- | --- | --- | --- |
| M0-01 | Review and merge spec v0.2; accept ADR-0001 to 0003 | PR merged; ADR status lines changed to Accepted | Nick | docs/ | Done |
| M0-02 | Decide the game title | Q1 answered; one commit renames project code names | Nick | repo-wide rename | Done |
| M0-03 | Scenario skeletons in prose (tutorial, sustainment, surge with joint turn, drawdown) | One paragraph each in scenarios/SKELETONS.md | Nick (drafted by Fable, approved in chat) | scenarios/ | Done |
| M0-04 | Scenario schema v0 (scenario.json, rules.json, theme.json) and assumptions.md | JSON Schema files in scenarios/schema/; assumptions.md lists every simplification beside the real rule | Fable | scenarios/schema/, docs/ | Implemented by Fable in PR 5; integration awaiting CI/merge |
| M0-05 | LinesOfSupport.Sim domain model and day-advance loop (first-playable tier) with unit and property tests | dotnet test green; invariants hold across 200 seeds on a stub scenario | Fable | src/LinesOfSupport.Sim, src/LinesOfSupport.Sim.Tests | Not started |
| M0-06 | Spoke B scenario.json plus four approach input logs as golden files | Harness reproduces the day 10, 12, 16 and 60 effects described in SPEC section 5 | Fable | scenarios/spoke-b/ | Not started |
| M0-07 | View-model contracts for morning board and the two editors | Interfaces in LinesOfSupport.Sim with XML docs; no Godot types | Fable | src/LinesOfSupport.Sim | Not started |
| M0-08 | Solution scaffold, LinesOfSupport.Harness (run, validate, batch with outcome spread), CI builds the solution | CI test job runs real builds; harness validate rejects a broken scenario | Codex | src/LinesOfSupport.sln, src/LinesOfSupport.Harness and Harness.Tests, .github/, global.json; empty Sim/Sim.Tests bootstrap and scenarios/_stub only per kickoff | Done (PR 4 merged); M0-04 shared-rules integration follow-up |
| M0-08a | Integrate Fable M0-04 schema with Harness | Actual stub passes C# and Python validation; shared rules and optional theme supported; malformed references/windows rejected; CI green | Codex | src/LinesOfSupport.Harness and Harness.Tests, .github/, backlog/, handoff; reconcile scenarios/_stub with Fable PR 5 | Implemented; awaiting CI and merge |
| M0-09 | Godot 4.6 .NET project referencing Sim; empty board scene; desk UI panel layout shell | Project opens, builds, shows the shell | Codex | game/ | Not started |
| M0-10 | First bpy kit experiment: one site (tent, CHU, generator, fuel bladder, truck) flat-shaded to glTF, rendered in Godot | Screenshot in handoff note; tools and steps that worked recorded in tools/README.md | Codex | tools/, assets/ | Not started |
| M0-11 | Provenance check covers glTF and audio; PROVENANCE.md template filled for the kit | CI provenance job fails on an untracked asset | Codex | assets/, .github/ | Not started |
| M0-12 | Codex kickoff brief | docs/CODEX-KICKOFF.md read by Codex; its first handoff note references it | Fable | docs/ | Done (read by Codex; M0-08 handoff references it) |
