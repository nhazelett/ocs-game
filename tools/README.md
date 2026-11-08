Blender (bpy) asset scripts and validators. Nothing here runs at game runtime.

## M0-10 five-piece kit

`bpy/generate_kit.py` creates tent, CHU, generator, fuel bladder and light truck meshes from parameters, with flat polygon normals and the four-color sand/olive/dark/glass palette. `--scale` sets uniform dimensions, `--asset` selects a piece, and `--date` records provenance. Each piece exports as a single glTF 2.0 `.glb` under `assets/kit/`; no textures or real-world marks. The script updates those paths' provenance rows and preserves unrelated rows.

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
