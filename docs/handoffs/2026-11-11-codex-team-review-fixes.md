**Work session handoff** (Codex, 11 Nov 2026, team review fixes)

Expected look of this build and why: a new team's names screen starts with three different existing fictional portraits instead of two silhouettes. Saved face choices, including an explicit blank, remain the player's choices. Task text has one separator after its hours. [Native names screen](images/2026-11-11-codex-team-review-names.png), inspected plain PNG under 400 KB.

## PRs reviewed this session

171, 173 and 174: all CI green, mergeable and without inline threads. PR 171's earlier follow-ups are accepted in Fable's latest review. Accepted PR 173's new default-face and punctuation follow-ups here; the hours-at-pace rounding is Fable's DESK-14. PR 174's dialog presentation is being fixed on its own branch. Read session/kickoff, required SPEC sections, ADR-0001 to 0003, DISSENT, decided objections, current backlog/questions and available Codex/Fable handoffs. No question or objection awaits Codex. The delegated five-open limit governs.

## Changed

PrepareTeamChoices assigns unused existing portrait keys when no valid authored default exists; it still loads remembered names/faces. The task label drops a trailing full stop before the separator. TeamSmokeTest now checks that a clean configuration starts with three different non-fallback portraits, then retains its real-control, independent-Sim, save/resume and fresh-shell checks.

## Why

Fable's 10:00 UTC review on PR 173 asks for three people on the first screen and one separator in work text. No Sim/scenario/asset change is needed.

## Validated how

The isolated checkout's complete tracked tree exactly matched published PR 173 before editing. Main 2138840 and the three live PR heads were read through the GitHub connector; CLI clone failed for missing HTTPS credentials. No successful CLI pull is claimed. Solution/Game builds, all 642 .NET tests (521 Sim, 121 Harness), scenario/provenance/conflict validators and whitespace checks pass. Native headless and graphical TeamSmokeTest pass; its screenshot was inspected. Single-process MSBuild accommodates this shell's socket limits. The graphical test uses the managed desktop and its installed 4.6.1 toolchain.

## Unresolved

Existing nonfatal ObjectDB/compiler/analyzer warnings. No fresh Windows export or human listening verdict. No new QUESTIONS or FOR-NICK entry, asset, or spend. Fable review: default portraits, preserved saved choices and revised work punctuation.

## Next

Publish on PR 173 without rewriting history; finish PR 174's presentation fix, then take DESK-24 under Q18. No PR merge, main push, force-push or shared-branch deletion.
