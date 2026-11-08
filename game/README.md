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

The board's Scenario picker lists folders under the repository's `scenarios/` directory that contain `scenario.json`, preferring `_stub` initially. The board reads only existing presentation fields: islands (`id`, `name`, `isHub`), transport links (`from`, `to`, `mode`), and sites (`name`, `island`, `opensDay`). It draws shapes, mode-labeled lines and site markers. Parallel transport links are separated so both labels are readable. No textures or art assets are present.

Island positions are a deterministic screen layout based on authored array order, explicitly labeled as schematic rather than geography. No coordinates, fields or Sim behavior are invented. Read-only Game-local display records are not a simulation contract. Invalid JSON/references clear the old map and show an error; missing scenario folders disable the picker. Editor runs read `../scenarios/` relative to `game/`; exported builds read `scenarios/` beside the executable, independently of the working directory. The SubViewport can host a later 3D board without changing the desk.

Next day is connected to a **Game-local no-op bridge**, and reports that day advance is unavailable. It never advances a fake authoritative clock. Sim is referenced, and its session API has landed; connecting the shell and the forthcoming view models is separate work (Q5/Q6). No shared Sim contract is defined in Game.

## Runtime smoke check

```sh
godot --headless --path game --editor --import --quit
dotnet build game/LinesOfSupport.Game.csproj -c Debug
godot --headless --path game res://tests/ShellSmokeTest.tscn
godot --headless --path game res://tests/BoardSmokeTest.tscn
```

Expected final line: `SHELL_SMOKE_PASS: nine tabs, board viewport, gauges, next-day no-op.` The separate test scene instantiates the real main scene, visits all tabs, and emits the real Next day button signal. CI runs this from a clean GitHub checkout with Godot 4.6.1 .NET and .NET 8.

The board scene prints `BOARD_SMOKE_PASS` after checking folder discovery, authored labels and modes, picker switching, invalid JSON/reference handling and missing directories. Its test data is temporary and does not edit the authoritative scenarios.

For a screenshot, run that test scene **without** `--headless` and append `-- --screenshot <absolute-output.png>`. Screenshots need a graphics display.

## Windows package (M0-13)

CI exports only on pushed `v*` tags, after the build, test, scenario, provenance and Godot checks pass. It uses Windows x86_64, .NET 8, **Godot 4.6.1 .NET** and the matching **4.6.1 .NET export templates** from the official Godot release. `export_presets.cfg` is tracked. Exporting does not require rcedit or a signing service.

To reproduce on Windows:

1. Install the .NET 8 SDK and Godot 4.6.1 .NET. Put `dotnet` on PATH.
2. In Godot choose **Editor > Manage Export Templates > Install from File** and select `Godot_v4.6.1-stable_mono_export_templates.tpz` from [the official 4.6.1 release](https://github.com/godotengine/godot-builds/releases/tag/4.6.1-stable). CI downloads and installs it automatically.
3. From the repo root, use PowerShell and choose an output directory that does not exist yet:

   ```powershell
   ./tools/export/windows.ps1 -Godot 'C:/path/to/Godot_v4.6.1-stable_mono_win64_console.exe' -OutputDirectory "$env:TEMP/LinesOfSupport-build"
   ```

The script builds, imports, exports, copies runtime data, checks the real exported main scene, then creates `LinesOfSupport-build.zip`. The check runs headlessly from a temporary working directory, verifies the scenario picker/map and referenced shared rules, and requires `EXPORT_SMOKE_PASS`. Release templates prohibit scene-path overrides, so this uses the explicit `-- --export-smoke-test` automation argument on the real main scene; normal launches show no test controls.

The zip contains `LinesOfSupport.exe`, its PCK, the Godot-generated self-contained .NET data directory, and sibling `scenarios/<folder>/scenario.json`, optional `theme.json`, and `scenarios/rules/rules-*.json`. Keep these together. No SDK or Godot installation is needed to run it. Reference sources, golden logs, validators, handoffs and kit experiment scenes are excluded.

After an authorized `v*` tag is pushed, open the repository's **Actions > CI > the tag run > Artifacts**, then download `LinesOfSupport-<tag>-windows-x86_64`. Extract that artifact and the package zip inside it, and launch `LinesOfSupport.exe`. This work does not create a tag or publish a release. The package currently shows the existing schematic shell; Next day is still unavailable.
