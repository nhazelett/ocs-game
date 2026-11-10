# Tabletop

Nick selected Tabletop in chat on 9 November 2026 after the three ART-01 renders. This is the production UI direction: bone paper, slate ink, moss controls, terracotta stops, and Atkinson Hyperlegible. `TabletopTheme.Create()` is the shared Godot theme from title/preparation/turnover through all desks, dialogs and the after-action. Body type is 18 px; existing larger narrative text remains larger. Stops, gaps, percentages, disabled states, checkmarks and focus borders retain their words/shapes as well as color.

Font sources and OFLs remain canonical under `assets/art-studies/fonts/` with provenance. The Windows exporter includes the two Atkinson fonts and their license; the release smoke check loads them from the package outside the checkout. Widgets and generated checkbox/navigation icons live in source; no additional raster assets.

ART-02 is the interface theme. Individual faces, the production 3D map, day playback and sound are separate ART rows. The old map has the chosen sea/ink colors and font while its native 3D replacement is built. The developer-only ART-01 comparison intentionally preserves all three archived directions.
