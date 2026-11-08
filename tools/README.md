Blender (bpy) asset scripts and validators. Nothing here runs at game runtime.

## M0-10 five-piece kit

`bpy/generate_kit.py` creates the five original pieces plus the M0-14 extension below, with flat polygon normals and the four-color sand/olive/dark/glass palette. `--scale` sets uniform dimensions, `--asset` selects a piece, and `--date` records provenance. Each piece exports as a single glTF 2.0 `.glb` under `assets/kit/`; no textures or real-world marks. The script updates those paths' provenance rows and preserves unrelated rows.

Intended Blender 4.4+ invocation from the repository root:

```sh
blender --background --factory-startup --python tools/bpy/generate_kit.py -- --scale 1 --date 2026-11-07
```

**Blender was unavailable on the authoring machine**: no executable on PATH or in the usual Program Files/per-user install locations. No Blender version or invocation has been proven. Python syntax compilation passes; bpy execution and Blender glTF output are unverified. The exporter uses documented vertex-color/material options: [Blender exporter API](https://docs.blender.org/api/4.4/bpy.ops.export_scene.html). A machine with Blender must run and inspect this script before its output is called Blender-validated.

The committed five `.glb` files are explicitly **generated Godot primitive fallbacks**, using the same palette, exported and reloaded by Godot 4.6.1 .NET. They are not Blender output. This follows the queue's fallback instruction. All have generated-origin rows in `assets/PROVENANCE.md`; project licensing remains TBD as stated in the root README.

What worked with Godot 4.6.1 .NET and .NET SDK 8.0.427:

```sh
dotnet build game/LinesOfSupport.Game.csproj -c Debug
godot --headless --path game --editor --import --quit
godot --headless --path game res://tests/KitExperiment.tscn -- --generate-fallback-kit --smoke-test
python3 tools/validate/validate_kit.py
godot --headless --path game res://tests/KitExperiment.tscn -- --smoke-test
```

`--generate-fallback-kit` overwrites only the five known kit files, never runs in the normal game scene, and reminds you to update provenance if the tool/origin/date change. Do not regenerate these after Blender output lands without changing their provenance back to the correct fallback origin.

Open `game/tests/KitExperiment.tscn` to inspect the scene. It uses one directional light and a fixed three-quarter orthographic camera. For a screenshot use a graphical run, adding `-- --smoke-test --screenshot <absolute-path.png>`. Headless runs verify five loaded glTF pieces and retained vertex colors. The Python kit validator checks GLB headers, embedded data, positions/normals/colors on every primitive, and absence of textures. Git LFS is required to retrieve the committed binary meshes; CI's Godot checkout enables it.

## M0-14 extended kit

Seventeen new pieces bring the inventory to 22:

| Group | New files in assets/kit/ |
| --- | --- |
| Terrain | terrain-sand.glb, terrain-scrub.glb, terrain-coast.glb |
| Connections | road-straight.glb, road-corner.glb, route-straight.glb, route-corner.glb |
| Buildings and site support | warehouse.glb, town-block.glb, antenna-mast.glb, guard-tower.glb, barrier-set.glb |
| Vehicles and equipment | fuel-truck.glb, bus.glb, forklift.glb, pallet.glb, water-buffalo.glb |

`bpy/kit_parts.json` is the shared authoring recipe for these pieces: meters, X right, Y up, Z back. Boxes have size/position/color; eight-sided cylinders have radius/length/axis/position/color. Both the existing Blender script and the explicitly disclosed Godot primitive generator consume these same dimensions and palette keys. Blender coordinates are converted to Z-up during authoring and back to Y-up at glTF export. No textures, marks, animation or simulation values are included. Four-meter terrain tiles and road/route ends align; rotate the corner pieces as needed. The water buffalo is a generic towable water tank.

**Blender remains unavailable and unverified.** Per Fable's Q7 answer, the primitive fallback is retained until real Blender output is needed at M3. All 22 committed meshes are generated Godot fallbacks, and their ledger rows say so. The original five binaries are unchanged. The extended bpy script has passed Python syntax compilation, not a Blender execution test. On a Blender machine the same invocation above now generates all 22 pieces; `--asset warehouse` selects one.

Commands proven with Godot 4.6.1 .NET and .NET SDK 8.0.427:

```sh
dotnet build game/LinesOfSupport.Game.csproj -c Debug
godot --headless --path game res://tests/KitExperiment.tscn -- --extended-kit --generate-extension-kit --smoke-test
python3 tools/validate/validate_kit.py
godot --headless --path game res://tests/KitExperiment.tscn -- --extended-kit --smoke-test
```

`--generate-extension-kit` writes only the 17 new pieces. Update their provenance rows if regenerating with a different tool, origin or date. `--extended-kit` displays the complete labeled gallery; omit it for the original five-piece experiment. For a graphical screenshot append `--screenshot <absolute-path.png>` after the `--`. The reviewed image is in `docs/handoffs/images/2026-11-08-codex-m0-14.png`. Normal game scenes never load these recipes or kit experiment assets. CI validates the recipe's complete inventory/positive dimensions/palette, all 22 GLBs, and both five-piece and extended Godot smoke runs.
