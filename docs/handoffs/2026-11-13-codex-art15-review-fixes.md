**Work session handoff** (Codex, 13 Nov 2026, ART-15 review fixes)

Expected look of this build and why: faces/names, filled hours bars, strong headings and readable figures remain. The morning fits one line. The combined ART-12/ART-15 desk keeps water and food service outage words on Today, the request and the award form at the default view. A real finance visit shows 3 of 8 hours; the footer says "The task is on your team's work list."

## PRs reviewed this session

- [209](https://github.com/nhazelett/ocs-game/pull/209): fixed all three findings on head 1114ed7; hosted CI run 362 is green. [Its detailed handoff](2026-11-13-codex-art12-final-review-fixes.md) records the sixteen map images and validation.
- [211](https://github.com/nhazelett/ocs-game/pull/211): initially green/no review, then Fable's 08:50 comment 6848360942 arrived during publication. Accepted the blocking missing-facts finding plus the requested partial-hours capture and complete footer wording. Main is 977abfd (Fable PR 212); its additional review and headline are retained.

## Changed

Merged published ART-12 head 1114ed7 into the existing ART-15 history. This deliberately stacks PR 211 on PR 209: the shared map correction is tested on the actual combined desk, including its smaller viewport. Fable merges 209 first, then reviews 211's remaining desk changes. Resolved the FOR-NICK conflict by keeping both approved #16 and authorized #17, with their histories; no new row or owner request.

- Morning heading: 22 px, 185 px minimum width, no wrapping. It remains stronger than body copy and is one line in all three looks. Navigation, teammate names, cash and the day button still fit. The fix retains all outage facts in the existing reduced-height map; it does not claim the old map height is restored.
- The player footer expands only the exact generic "in work" acknowledgement to a complete sentence. Stored Sim results and developer diagnostics retain their original text. This is presentation formatting; no Sim action, knowledge, rule or state change.
- The hierarchy fixture now captures a partial bar from the real finance action and Today on day 4, then verifies map layout/visible outage facts on every state and look. J4's player-text expectation changes to the requested sentence; exact-action and hash checks stay intact. Existing developer action-result tests still require the original Sim messages.
- Twelve new matching PNGs compare the tested ART-12 dependency with the combined desk, isolating ART-15's changes. Only ART-15's Status cell was edited; Fable's headline is preserved.

## Why

Fable accepted the desk hierarchy but rejected its loss of map facts, then requested a partial bar and a complete footer. Sharing the already tested root fix avoids two different fallback implementations. The new captures show the combined result, without fabricating hours or support state.

## Validated how

- Required Release solution build and all 754 .NET tests pass (633 Sim, 121 Harness; zero failed/skipped). Eight existing solution warnings; final Game build is clean.
- Sixteen native suites pass with successful exits: DeskHierarchy, Team, Chrome, Morning, Look, MapArt, MapCallout, Map, MapLayout, DayPlayback, J4Dependency, Performance, Bpa, OtherActions, Action and Commitment. Coverage includes assignment/save/restart, all three looks, six Fit/zoom art states, twenty replay days, exact previews/actions, player/developer result text and unchanged Sim hashes during rendering.
- Scenario validation: five folders, one rules file, zero errors. Provenance, tracked conflict markers and whitespace pass.
- All twelve comparison PNGs are plain 1280x800 files below 400 KB (largest 151,373 bytes); all six after states were visually inspected. Both sides use the same capture fixture and real inputs. The before runs its --baseline mode, which skips new hierarchy/style assertions while preserving actual hours and Sim-purity checks.

Renderer: Godot 4.6.1 .NET, OpenGL Compatibility on Linux Mesa llvmpipe 25.0.7, matching Windows settings. No fresh Windows run or ordinary-laptop benchmark. The new changes add no mesh, texture, shader or light beyond the carried ART-12 dependency. Look/Bpa retain existing nonfatal ObjectDB exit warnings; no final assertion-pass process crashed.

| State | ART-12 dependency before | Combined after |
| --- | --- | --- |
| Today, day 1 | [PNG](images/2026-11-13-codex-art15-fixed-before-today.png) | [PNG](images/2026-11-13-codex-art15-fixed-after-today.png) |
| Actual finance visit, 3 of 8 hours | [PNG](images/2026-11-13-codex-art15-fixed-before-partial-hours.png) | [PNG](images/2026-11-13-codex-art15-fixed-after-partial-hours.png) |
| Actual full and empty personal hours | [PNG](images/2026-11-13-codex-art15-fixed-before-hours.png) | [PNG](images/2026-11-13-codex-art15-fixed-after-hours.png) |
| Today, day 4, site down | [PNG](images/2026-11-13-codex-art15-fixed-before-today-down.png) | [PNG](images/2026-11-13-codex-art15-fixed-after-today-down.png) |
| Forklift request, day 4 | [PNG](images/2026-11-13-codex-art15-fixed-before-request.png) | [PNG](images/2026-11-13-codex-art15-fixed-after-request.png) |
| Award form, day 4 | [PNG](images/2026-11-13-codex-art15-fixed-before-award.png) | [PNG](images/2026-11-13-codex-art15-fixed-after-award.png) |

## Unresolved

Fable's visual re-review and hosted CI on the new PR head remain required. The map uses ART-12's current terrain/lighting; ART-13 is next. Windows/performance limits above remain. Publication follow-up: the source/evidence branch update succeeded at 09bb011. Updating public PR 211's description was rejected by automatic approval for lacking specific authorization to publish that detailed communication. Checked direct owner history (Nick, 13 Nov 06:48), then retried only that known-unexecuted operation once under the newly verified standing direction; it was rejected again. The description remains unchanged, with no alternate publishing route attempted. FOR-NICK #18 requests the description-only approval; its exact prepared text is retained locally. Game implementation and other authorized work continue. No unanswered question to Codex.

## Next

Take ART-13, the highest ready Codex row, with the corrected map and desk as its named dependencies. The direct five-open-PR limit applies; two PRs are open. No PR merged by Codex, main push, force-push or shared branch deletion.
