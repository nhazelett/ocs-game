**Work session handoff** (Codex, 10 Nov 2026, ART-09 and requested DESK pass)

Expected look of this build and why: Menu > Sound now shows separate Sound and Music sliders with visible rails, independent mutes and remembered settings. The front door plays the approved title once; the desk alternates the two approved beds with fades and three seconds of quiet; harbor ambience plays only while the live map is visible. Music and ambience lower under actual event cues. The shortfall caption now sits above the site's equipment, addressing Fable's PR 155 review note.

## PRs reviewed this session

No open PR needed a fix at the initial pass. ART-07 / PR 155 was merged and Fable's seven-image review was read. Its nonblocking map-caption finding is addressed here.

Opened DESK-20 / PR 158, DESK-21 / PR 159 and DESK-25 / PR 160, in Nick's explicit order. All three have green test, scenario, provenance and godot-shell checks; Windows export is skipped by the PR workflow. ART-09 stacks on PR 160, carrying both earlier rows. Each item has its own status change, screenshots, regression and handoff.

## Changed

Loaded only the four approved ART-08 Ogg files. No audio generation or download credits were spent, and no asset file or provenance row changed.

Added GameMusic for title/desk contexts, a genuine alternating playlist, fade-in/out and quiet gaps, a two-player equal-power harbor crossfade, and smooth ducking while GameAudio reports an event cue actually playing. Map-covering contract details and Menu fade out harbor ambience. Muting Sound leaves Music usable; muting Music stops music and ambience without stopping cues. Missing files remain silent.

Music volume/mute use user://music.cfg, independently of the existing sound.cfg. The default is deliberately quiet. Both slider tracks now have visible minimum height. Windows packaging copies the approved music folder; the compiled exported-main smoke check verifies four streams.

Added MusicSmokeTest to CI. Its real native streams cover title completion, desk transitions and alternating order, quiet gaps, harbor overlap, finite nonnegative gains, actual-cue ducking/recovery, independent mute/zero, preference restoration and missing-file silence. Source inspection and the graphical check caught and fixed a crossfade rounding endpoint and invisible slider rails.

Moved the reported shortfall label upward and added an equipment-clearance assertion to MapSmokeTest. Updated only ART-09's Status cell and added developer README notes.

[Settings](images/2026-11-10-codex-music-settings.png) · [Site equipment clear](images/2026-11-10-codex-music-map.png).

## Why

This completes the requested DESK-20, DESK-21, DESK-25, then ART-09 sequence. The soundtrack remains decoration and does not carry game state.

## Validated how

Solution and Game builds pass; all 632 .NET tests (511 Sim, 121 Harness), 72 Python tests, required scenario/provenance validators and conflict/whitespace checks pass. The complete stack passed all 42 native CI suites with exit 0 (40 smoke scenes plus both kit cases). After the final slider-height fix, Music, Audio, Look and TabletopTheme were rerun and pass. The Music logs contain no ERROR lines; some existing suites retain nonfatal ObjectDB exit warnings.

The graphical Music and Map suites pass. Both committed 1280 by 800 plain PNGs were inspected and are under 400 KB. Playback verification used Godot's dummy audio driver; no human listening verdict is claimed.

## Unresolved

No development or publication blocker; no FOR-NICK additions. The initial DESK-20 screenshot upload was held by automatic review. Nick's standing "Codex can push whatever is needed" instruction and private repository ownership were verified; that exact action was retried once and succeeded. Nothing remained blocked and no alternate upload route was used.

For Fable/Claude review: listen to default levels, cue ducking, playlist gaps and the harbor seam; inspect the lifted map caption. No fresh Windows export was run, so the actual Windows artifact still needs its normal main/tag export verification.

## Next

Review and merge in order: PR 158, PR 159, PR 160, then this ART-09 PR. Codex never merges, pushes main, force-pushes or deletes shared branches. Remaining DESK screen rows follow their Fable Sim contracts; DESK-20's end-of-day sentences bind DESK-04 when available.
