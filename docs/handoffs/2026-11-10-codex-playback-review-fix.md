**Work session handoff: playback review fix**

Expected look of this build and why: all three reported Spoke B shortfalls appear together in the playback strip. Held fuel is named at the closed lane's source shore. Fresh plain 1280 by 800 PNGs: [shortfall](images/2026-11-10-codex-playback-fix-shortfall.png), [held fuel](images/2026-11-10-codex-playback-fix-held.png), below 150 KB each.

## PRs reviewed this session

142 fixed in d255366, with main merged. PR 143 now carries that exact map correction. The two merge conflicts were resolved by preserving ART-05's reported-change rings, ART-04's tethered labels, Fable's new backlog rows and both items' status cells. No force push or PR merge.

## Changed

Merged the ART-04 fix into ART-05. The playback sentence groups known changes by site, kind and reported percentage, naming every affected capability instead of saying two more are in the records. It preserves the reported facts rather than inventing a causal relationship absent from MapDay. Unknown changes remain filtered.

## Why

Answer Fable's map findings and playback wording note on PRs 142 and 143. Nothing changes simulation state, the five-second duration, input guard, skip or timer behavior.

## Validated how

Game build has no new warnings. Native Map, DayPlayback and TabletopTheme suites pass and exit cleanly, including the repaired anchor and the complete shortfall sentence. Graphical DayPlayback passes; both new screenshots visually inspected. Required solution build, all 619 tests, scenario/provenance/conflict checks passed on the exact merged main source during this fix pass; no Sim or scenario files changed beyond that merge.

## Unresolved

Remote CI reruns on both updated heads. Seven inherited solution warnings; no new Game warnings. No FOR-NICK addition.

## Next

Claim M2R-36 and bind every award choice to Fable's new availability/reason view. Five open PRs remains the cap.
