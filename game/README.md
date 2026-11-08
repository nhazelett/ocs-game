# LinesOfSupport.Game

M0-09 shell for **OCS: Lines of Support**. Requires the **.NET 8 SDK** and **Godot 4.6 .NET** (tested with 4.6.1). The standard Godot download without .NET cannot run these C# scripts. No art, add-ons, export templates or Python packages are needed to open and run the game project.

From the repository root:

```sh
dotnet build game/LinesOfSupport.Game.csproj -c Debug
godot --editor --path game
```

Replace `godot` with your Godot .NET executable path if it is not on PATH. Open `game/project.godot` directly in the editor as an alternative, build the C# project, then press **F5**. The editor uses `game/LinesOfSupport.Game.sln`, which includes the pure Sim project by reference. Display title is set once in `project.godot`; the shell reads it from project settings.

## Expected build

A default-theme desktop shell: Day 0, unknown shop-hours/cash gauges, a left schematic hub-and-two-spokes map rendered inside a SubViewport, and a right desk with nine empty tabs. Use the tab-bar arrows to reach the later tabs, or keyboard focus. The split between board and desk can be dragged.

The map is an illustrative placeholder drawn with code; it does not load scenario data. The SubViewport can host the later 3D board without changing the desk. No textures or art assets are present.

Next day is connected to a **Game-local no-op bridge**, and reports that day advance is unavailable. It never advances a fake authoritative clock. Sim is referenced by the project but still has no callable session API in this revision. Replace `GameSessionBridge` when Fable's ADR-0005/session and view-model contracts land (Q5/Q6); no shared Sim contract is defined in Game.

## Runtime smoke check

```sh
godot --headless --path game --editor --import --quit
dotnet build game/LinesOfSupport.Game.csproj -c Debug
godot --headless --path game res://tests/ShellSmokeTest.tscn
```

Expected final line: `SHELL_SMOKE_PASS: nine tabs, board viewport, gauges, next-day no-op.` The separate test scene instantiates the real main scene, visits all tabs, and emits the real Next day button signal. CI runs this from a clean GitHub checkout with Godot 4.6.1 .NET and .NET 8.

For a screenshot, run that test scene **without** `--headless` and append `-- --screenshot <absolute-output.png>`. Screenshots need a graphics display. The normal main scene has no test switches or controls.
