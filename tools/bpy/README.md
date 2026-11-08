# Spoke B diorama (M3-11)

The 22 kit meshes are now Blender 4.3.2 output, replacing the disclosed M0 Godot primitive fallbacks. All retain the four-color vertex palette, flat normals and embedded geometry; no textures, rigs, animation or external files. The original five-piece and complete 22-piece Godot 4.6.1 .NET smoke scenes both load them successfully.

`assets/diorama/spoke-b.layout.json` places 25 kit instances in meters (X right, Y up, Z back). It is an illustrative coastal site, not scenario data or a representation of simulated quantities. `assemble_diorama.py` loads the exported kit in Blender and writes a single self-contained GLB. Godot creates the planned three-quarter orthographic camera and one directional key light from the layout. Ambient illumination adds no second directional light. The normal game keeps its schematic board.

From the repository root:

```sh
blender --background --factory-startup --python tools/bpy/generate_kit.py -- --scale 1 --date 2026-11-08
blender --background --factory-startup --python tools/bpy/assemble_diorama.py
python3 tools/validate/validate_kit.py
python3 tools/bpy/validate_diorama.py
godot --headless --path game --script ../tools/bpy/render_diorama.gd -- --smoke-test
```

For a graphical preview, omit `--headless` and `--smoke-test`. For a screenshot on a machine with a display:

```sh
godot --path game --script ../tools/bpy/render_diorama.gd --rendering-method gl_compatibility --resolution 1440x900 --audio-driver Dummy -- --screenshot /absolute/path/spoke-b.png
```

Proven here: Blender 4.3.2; .NET SDK 8.0.427; Godot 4.6.1 .NET kit tests; Godot 4.6.3 OpenGL compatibility screenshot on Mesa llvmpipe. The container has no active display; a temporary authenticated Xorg dummy display was used for the screenshot and stopped afterward. No system package was installed. Blender prints a missing optional Draco-library message; these exports use uncompressed GLB and pass the embedded-buffer check.

The review image is [spoke-b.png](../../assets/diorama/spoke-b.png). Every asset, including the layout, screenshot and attributes metadata, has a provenance row. The kit is about 263 KB and the complete diorama GLB about 221 KB. `assets/.gitattributes` stores these small generated binaries and this screenshot directly in Git: the connected GitHub API can publish them without a separate LFS-upload credential. Other asset families keep the root LFS policy. Regeneration updates known kit provenance rows and preserves the diorama rows; update renderer/version/date metadata if the tools change.
