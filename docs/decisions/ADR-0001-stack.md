## Title
ADR-0001: Godot 4.6 .NET with C# throughout; the simulation is a separate class library

## Status (Proposed / Accepted / Superseded)
Proposed (accepted when the spec v0.2 PR merges)

## Context
The game is a deterministic simulation with a view on it. Two AI agents write most of the code; the human reviews behavior, not source. The release target is a standalone Windows build, later Steam. See SPEC.md section 8.

## Options considered
1. Godot 4 with GDScript throughout.
2. Godot 4 .NET with C# throughout, sim in its own .NET 8 class library with no Godot dependency.
3. Unity or Unreal.
4. Custom engine.

## Decision
Option 2. `Footprint.Sim` (class library), `Footprint.Sim.Tests` (xUnit), `Footprint.Harness` (console), `Footprint.Game` (Godot 4.6 .NET), `Footprint.Tools` (Python and Blender scripts, not runtime). Project names are placeholders until the title is set.

## Consequences
- The sim runs headless for tests, 200-seed balance runs and counterfactual replays without the engine.
- Compile-time checking on agent-written code; standard test tooling.
- No web or mobile export (C# projects in Godot 4 do not export there); not needed.
- .NET 8 SDK is a build dependency for contributors; players need nothing extra.

## Date
2026-11-07

## Author
Fable, for Nick Hazelett
