**Work session handoff** (Codex, 10 Nov 2026)

Expected look of this build and why: Menu now has Look. Field atlas uses warm paper, rust controls and Barlow/Bitter; Night watch uses dark blue, pale type and mint controls; Tabletop remains the fresh-install default. All three use the live production 3D board and existing cast, with the selected palette and fonts carried across the title, preparations, desk, settings and after-action. This carries the existing directions without commissioning more art.

## PRs reviewed this session

The live open-PR list was empty, so no same-branch CI, review or conflict fix was needed. M2R-38 is already merged in PR 153; ART-08 audio is merged in PR 154. Read their resulting main files as the implementation baseline. No new objection.

Read the current CODEX-SESSION and CODEX-KICKOFF, required SPEC sections, ADR-0001 to ADR-0003, DISSENT and the four decided objections, art-direction and opening guidance, backlog, QUESTIONS, FOR-NICK and the latest Codex/Fable handoffs. No unanswered question to Codex or waiting objection was found. The explicit five-open-PR cap takes precedence over the repository's ten-PR wording.

## Changed

Claimed `codex/ART-07` at current main `544f03f9f2a58c4ea445c9c5a49eebca75eda9be` before implementation.

The keyboard-accessible Look list applies immediately and stores the preference in `user://look.cfg`. Missing, malformed or out-of-range settings use Tabletop; a failed write leaves the chosen look usable for that visit. Style changes preserve the live session, the current award assembly, map facts, selection and camera.

The shared theme uses the established ART-01 fonts and colors. Existing manually styled cards and backgrounds update too. Sound sliders now inherit their colors from the shared theme. Windows packaging includes every selectable font and its OFL; the release smoke check loads all of them from the packaged asset root.

Added LookSmokeTest and its CI step. Updated only ART-07's backlog Status cell. Seven inspected 1280 by 800 plain PNGs, each below 400 KB:

[Setting](images/2026-11-10-codex-look-setting.png), [Field atlas](images/2026-11-10-codex-look-morning-0.png), [Night watch](images/2026-11-10-codex-look-morning-1.png), [Tabletop](images/2026-11-10-codex-look-morning-2.png), [Night title](images/2026-11-10-codex-look-title-night.png), [Night preparations](images/2026-11-10-codex-look-prep-night.png), [Night after-action](images/2026-11-10-codex-look-debrief-1.png).

## Why

ART-07 is the next unimplemented Codex row after the merged M2R and ART fixes. It lets players choose the retained directions while keeping the proven map interactions and facts.

## Validated how

Solution build and all 629 .NET tests pass (508 Sim, 121 Harness), using one MSBuild worker in this environment. Game build passes. Scenario/provenance/conflict/whitespace checks and all 72 Python tests pass. All 38 native CI suites pass with exit 0: 36 smoke scenes including Look, plus both kit cases, under Godot 4.6.1 .NET.

The new regression covers repeated style switching, actual live control colors, all desks, unchanged state and award selection, exact MapDay facts and camera, outage wording, title/preparations/after-action, keyboard focus, restart restoration and invalid/unwritable settings. The graphical run also passes; its screenshots were visually inspected. Some existing suites retain nonfatal ObjectDB shutdown warnings.

No fresh Windows export is claimed. Packaging and its compiled release smoke assertions were updated; a tag/main export run must verify the actual Windows artifact.

Git CLI pull failed because HTTPS credentials are unavailable. Used the connected GitHub API to synchronize all 750 upstream blobs, verified every blob SHA and the complete tree, and reconstructed the exact signed main commit locally. Existing worktrees were left alone. No successful CLI pull is claimed.

## Unresolved

No development or publication blocker. No FOR-NICK addition.

For Fable/Claude review: inspect the seven PNGs, especially Night watch contrast and the decision to carry the optional ART-01 palettes/fonts onto the production 3D board with the existing cast rather than replacing its factual movement with the fixed art-study scene. No new generated or third-party asset was added.

## Next

Fable reviews and merges on its own schedule. ART-09 is the next unimplemented Codex row and is unblocked by merged ART-08. Its desk tracks are a playlist with fades and quiet gaps, not seamless loops. No ART-09 claim or implementation was started in this session. Codex merged nothing.
