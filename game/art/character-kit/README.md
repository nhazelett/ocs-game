# Original Tabletop character kit (ART-20)

The game uses 26 fictional people and an unassigned sculpt, rendered from one parameterized Blender kit. These are original mesh constructions: a continuous head with cheek and jaw planes, ears, inset eyes and lids, a sculpted nose, shallow jaw-following beards, hair masses, open glasses frames, shoulders and folded cloth. There are no photo references, purchased models, textures, national marks or insignia. Appearance is authored against stable portrait keys, not inferred from names or nationality.

`render_cast.py` contains the entire source and the explicit CAST table: skin, hair, clothing, hair silhouette, glasses, beard, face width, jaw, age, pose and collar. One orthographic camera, one soft area light and the same ambient world light render every person. Matte clay and cloth use the Tabletop palette. The anonymous fallback has no eyes, mouth or named identity.

From the repository root, with Blender 4.3.2:

```sh
blender -b --python game/art/character-kit/render_cast.py
# An individual key can be regenerated without touching the others:
blender -b --python game/art/character-kit/render_cast.py -- ruiz
```

The CPU Cycles renderer uses a fixed seed and 128 samples, with denoising disabled (the available Blender build has no OpenImageDenoise). Outputs are transparent 256×320 PNGs in `game/art/cast/`, each recorded separately in `assets/PROVENANCE.md`. Keep those PNGs as plain Git files. Godot imports mipmaps, and every live portrait/icon selects linear filtering with mipmaps. The 80×100 card, 48×60 assignment and 36×45 strip sizes are reviewed in native Godot sheets; small result rows keep their explicit 40×50 size. There is no 3D character rendering during play.

Review all faces at those sizes with the same native fixture before and after:

```sh
godot --path game --resolution 1280x800 --script res://tests/CastReview.gd -- --screenshots /tmp/cast
# Historical source in the same layout:
godot --path game --resolution 1280x800 --script res://tests/CastReview.gd -- --legacy --screenshots /tmp/legacy-cast
```

`PortraitSmokeTest.tscn` checks the complete selectable set, every scenario and default teammate key, native mipmaps and transparency, both eye openings, different raster results at 48 px and safe fallback handling. `TeamSmokeTest.tscn` checks that every picker icon is the exact portrait used during play, along with personalization and save/resume. Pixel hashes prove that the images differ; human review of the sheets judges whether people can be confused. Windows execution and ordinary-laptop performance still require their own verification.
