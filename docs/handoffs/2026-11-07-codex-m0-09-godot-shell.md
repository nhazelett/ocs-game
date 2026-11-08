**Work session handoff**

Expected look of this build and why: plain dark panels, default Godot fonts and theme, three circles joined by lines on the board, and empty desk tabs. Day 0 and unknown resource gauges are intentional because no scenario or simulation session is connected yet. This is the expected M0 shell, not a playable game.

## Changed

- Added a Godot 4.6 .NET project, tested with the 4.6.1 maintenance release, targeting net8.0 and referencing `LinesOfSupport.Sim`. Separate Game solution keeps the engine project out of the pure headless Sim/Harness solution.
- Main scene: left SubViewportContainer/SubViewport with a 2D schematic Node2D; right tabs Board, Requirements, Actions, Directory, Vehicles, Ledger, Inbox, Reports, Debrief; top Day, shop-hours and cash gauges, Next day. The split is draggable. Later tabs use Godot's default scrolling tab bar.
- One display title in project.godot. No art, custom theme, textures, plugins, scenario mutation or Sim source edits.
- Next day emits to a Game-local bridge that currently returns no-op and leaves Day 0 intact. Added Q6 for the actual Game-facing Sim session/view-model contract; Q5 describes Fable's planned ADR-0005 but executable types are still pending.
- Added a separate scene-based smoke check and a CI godot-shell job using official Godot .NET and .NET 8. Windows export remains its existing tag-only placeholder; shipping/export setup is separate work.
- M0-08b is merged as PR 7. Updated backlog accordingly. User authorized this work and merging in chat.

## Why

M0-09 proves the engine/C# project boundary and desk/board layout before simulation and 3D content are ready. A no-op integration seam makes the missing session clear and avoids inventing authoritative state inside Game or editing Fable's component.

## Validated how

- Godot `4.6.1.stable.mono.official.14d19694e`, .NET SDK 8.0.427, Windows x64. Game Debug build: zero warnings/errors. Headless editor import succeeds.
- Runtime smoke scene passes: visits all nine named, empty tabs; checks the schematic viewport and both gauges; emits Next day's real button signal and verifies unchanged day plus feedback. The same scene runs in CI from a clean checkout.
- Captured and visually inspected a real 1280x800 OpenGL render, using the default Compatibility renderer on an NVIDIA RTX 4060 Laptop GPU. No runtime errors in the capture log.

![M0-09 shell: illustrative board and empty desk tabs](images/2026-11-07-codex-m0-09-shell.png)

## Unresolved

- No real day advance or resource readings until Fable delivers the session API and M0-07 view models. The Game project reference is present; a call into a nonexistent Sim API cannot be implemented honestly. The kickoff explicitly allows a no-op until Sim exists.
- No game art or playable mechanics. M0-10 (kit experiment) and M0-11 (stronger asset provenance) remain separate backlog items.
- Local portable Godot is at `C:\Users\nickh\.codex\tools\godot-4.6.1\Godot_v4.6.1-stable_mono_win64\`; SDK is at `C:\Users\nickh\.codex\tools\dotnet8\`. No persistent PATH changes were made; normal contributor instructions assume their own installed tools on PATH.

## Next

Merge the green M0-09 PR. Fable continues M0-05 to M0-07 and answers Q6. Codex's next separate task is M0-10, the five-piece Blender/Godot kit experiment, followed by M0-11.
