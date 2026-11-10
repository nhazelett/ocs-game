**Work session handoff: ART-06**

Expected look of this build and why: a paper Sound panel with a volume slider, mute checkbox and cue preview is reachable from the title and Menu, with remembered settings. [Fresh native 1280 by 800 PNG](images/2026-11-10-codex-sound-refreshed.png), below 40 KB. Four brief original cues accompany actual new days, visitors, shortfalls and accepted awards; existing visual messages remain.

## PRs reviewed this session

142 and 143 are green, mergeable and cleared by Fable. 146 is green and mergeable; added the newly requested open supplier-list PNG with the greyed host-nation entry and exact native tooltip, with the AwardChoices suite passing. Its previously local BPA-first capture, handoff and status update remain pending the existing publication approval; those artifacts were not retried. 148 (M2R-34 quiet morning) is open and green, with its status update, handoff and plain PNG. No PR merged.

## Changed

Resumed the existing ART-06 implementation rather than rebuilding it. Merged current main a78f8b6 and reviewed ART-05 head 7c59a9a, retaining both the four-cue sound feature and all reviewed map/playback fixes. Only ART-06’s backlog Status cell changes relative to that dependency. Explicitly disposed the sound slider resource wrappers and stopped/detached audio streams when their owner leaves the tree, hardening native cleanup after shutdown leak warnings were found in the merged regression pass.

The existing implementation includes deterministic original mono PCM files, an MIT redistribution grant, provenance rows, source generator, remembered volume/mute, read-only cue preview, CI Audio regression and Windows packaging checks. Accepted commands and completed-day MapDay facts drive cues; redrawing and navigation do not. No new simulation rules or scenario edits.

## Why

A queue slot opened for the sound work completed by the prior claiming session. The branch needed current main and Fable’s map/playback corrections before review. The user’s five-open limit overrides the repository’s older ten-open wording.

## Validated how

Solution and Game builds pass, all 619 .NET tests pass (498 Sim, 121 Harness), and scenario/provenance/conflict/whitespace validation passes. Seven affected native suites pass: Audio, DayPlayback, Map, FrontDoor, TabletopTheme, Lead and Save. The graphical Audio run passes and its fresh capture was inspected. Every assertion and native process exits successfully. Audio and Map spot checks after cleanup were warning-free, but the final group still intermittently reports ObjectDB instances at shutdown in several older suites; this is not proven eliminated, and no unsafe-reference crash occurred. Four WAV files regenerate byte for byte from their source and do not clip. Seven inherited solution warnings; no new Game warning. The earlier full Python validator suite remains recorded in the original ART-06 handoff; this refresh runs the required validators. Windows export was not rerun on this Linux refresh; the existing packaging code and exported smoke assert all four files and their license.

Current main and branch heads were verified through the connected GitHub app because shell Git lacks credentials. The tested merged tree is published as an ordinary two-parent commit on codex/ART-06, using the current sound head as first parent and reviewed ART-05 as second parent; no force push.

## Unresolved

No new FOR-NICK entry. The earlier PR 146 publication blocker remains recorded locally in the previous session. Opening the sound PR fills the fifth Codex slot (142, 143, 146, 148 and sound), so no further backlog item starts.

## Next

Fable reviews and merges on its own schedule. The remaining PR 146 artifacts require the already requested owner approval before publication resumes. Once a slot opens, start with the fix pass and the current backlog order; do not restart ART-06 or duplicate the pending artifacts. M2R-38 is proposed in Fable PR 147 and ART-07 remains low priority.

## CI fix pass on PR 149

The initial CI run 42755198972 passed the .NET/scenario/provenance jobs and gameplay assertions through Support, then aborted during Support’s native shutdown (exit 134). Its repeated unsafe ImageTexture and StyleBoxFlat references came from portrait textures and regenerated person cards. PersonPortrait now disposes both read/create texture wrappers after the native control owns them; customer and tutorial person cards likewise dispose their style wrappers after native ownership. Visual content and simulation behavior are unchanged. Game build and the full Support, Portrait, People, GoingToPeople, TabletopTheme and Audio native suites pass after this fix. The earlier paragraph’s no-crash observation applies to the local runs; CI exposed this additional failure and the fix stays on PR 149. Some successful suites still report audio stream/playback ObjectDB warnings, without unsafe-reference errors; do not claim those warnings are eliminated. The updated CI must pass before Fable merges.
