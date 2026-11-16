# Original Tabletop character kit (ART-20)

The game uses 26 fictional people and an unassigned sculpt, rendered from one parameterized Blender kit. These are original mesh constructions: a continuous head with cheek and jaw planes, ears, inset eyes and lids, a sculpted nose, shallow jaw-following beards, hair masses, open glasses frames, shoulders and folded cloth. There are no photo references, purchased models, textures, national marks or insignia. Appearance is authored against the scenario cast and the established vector identities. Skin and hair colours remain artistic choices, never surname or nationality rules. The scenario-grouped sheets label each actual name, including the default teammate aliases.

`render_cast.py` contains the entire source and the explicit CAST table: skin, hair, clothing, hair silhouette, glasses, beard, face width, jaw, age, pose and collar. `BUILD` authors shoulder and neck proportions; `HAIR_PART` chooses the part and tied-hair side. The hairline tapers into the forehead and descends to the ears. Visible high buns, offset ponytails, parted bobs, shoulder waves and volumetric curls have different silhouettes. Civilian V necks, cardigans, polo shirts and lapelled jackets accompany the plain workshirts and zip collars. The camera frames the head and shoulder tops at a fixed 2.20 orthographic scale. One orthographic camera, one soft area light and the same ambient world light render every person. Matte clay and cloth use the Tabletop palette. The anonymous fallback has no eyes, mouth or named identity.

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
# Full tutorial and Spoke B casts, including each default teammate at 48 px:
godot --path game --resolution 1280x800 --script res://tests/CastReview.gd -- --scenarios --screenshots /tmp/by-scenario
# Add --legacy for the matched historical scenario sheets.
```

`PortraitSmokeTest.tscn` checks the complete selectable set, every scenario and default teammate key, native mipmaps and transparency, the larger head framing, both eye openings, different raster results at 48 px and safe fallback handling. `TeamSmokeTest.tscn` checks that every picker icon is the exact portrait used during play, along with personalization and save/resume. Pixel hashes prove that the images differ; human review of the sheets judges whether people can be confused. Windows execution and ordinary-laptop performance still require their own verification.


## Cast audit after Fable's 15 Nov review

No scenario reference or team customization key changes. These are fictional art
choices, not demographic facts inferred from a name. Each named identity is checked
against the scenario and its earlier portrait; the women called out in the review
retain clearly visible tied, parted, long or curled hair and varied shoulder builds.
The same shapes remain available to players of any name through the picker.

| Key | Named person (default teammate alias where present) | Hair / dress |
| --- | --- | --- |
| bell | Marcus Bell / You | Close hairline, broad build / zip collar |
| brandt | Lena Brandt / Joan Avery | Blonde parted pixie, narrow build / workshirt |
| brooks | Lena Brooks | Chin-length brown bob / civilian V neck |
| carvalho | Ines Carvalho | Dark bob, rectangular glasses / cardigan and linen inset |
| cole | Erin Cole | Copper high bun, round glasses / blue workshirt |
| delgado | Rosa Delgado | Copper shoulder curls, glasses / civilian lapels and linen inset |
| dunn | Ray Dunn | Broken silver crop and beard / blue zip collar |
| haddad | Nadia Haddad | Long dark curls, rectangular glasses / green V neck |
| hale | Tom Hale | Receding crown, dark beard, glasses / workshirt |
| kowalski | Pete Kowalski | Receding silver sides, broad jaw and beard / civilian jacket |
| lin | Grace Lin | Brown parted bob, narrow jaw, round glasses / blue workshirt |
| mensah | Kofi Mensah | Close hairline, broad face, beard and glasses / blue workshirt |
| mills | Jordan Mills | Short brown curls, narrow face / workshirt |
| moreau | Andre Moreau | Receding silver sides, glasses / open polo |
| nair | Priya Nair | Dark high bun on the left, narrow build / green workshirt |
| okafor | David Okafor / Eli Novak | Tall dark coils, slim shoulders / crew neck |
| ortiz | Carla Ortiz | Brown high bun on the right, round glasses / khaki workshirt |
| park | Daniel Park | Short swept hair, broader jaw, rectangular glasses / blue workshirt |
| pryce | Owen Pryce | Bare crown, clean jaw / civilian crew neck |
| reyna | Tomas Reyna | Brown side sweep and beard / open polo |
| ruiz | Ana Ruiz | Dark offset ponytail, tapered jaw / green workshirt |
| sato | Hana Sato | Chestnut offset ponytail, narrow build / blue V neck |
| silva | Marco Silva | Dark curls and beard / blue open polo |
| tan | Grace Tan | Dark shoulder waves, wider face / ochre cardigan |
| ward | Helen Ward | Silver shoulder waves, glasses / green workshirt |
| whitfield | Sam Whitfield | Broken silver crop, broad jaw and beard / zip collar |
| fallback | Unassigned | Featureless clay head / plain crew neck |
