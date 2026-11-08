LinesOfSupport.Sim (C# class library, no Godot dependency), LinesOfSupport.Sim.Tests (xUnit), LinesOfSupport.Harness (console). Project names follow ADR-0004.

`LinesOfSupport.sln` targets .NET 8. Build with `dotnet build src/LinesOfSupport.sln -c Release` and test with `dotnet test src/LinesOfSupport.sln -c Release --no-build` from the repository root. `LinesOfSupport.Harness.Tests` covers command-line and validation behavior separately from Fable's simulation tests. See `LinesOfSupport.Harness/README.md` for commands and M0 limitations.
