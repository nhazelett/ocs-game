**Work session handoff** (Codex, 11 Nov 2026, DESK-22)

Expected look of this build and why: the tutorial's strip has three short, aligned personal-hour bars with clickable names. A name opens the team's portraits, available time and unfinished tasks; each task can be handed to a named colleague, with the Sim's hours at their pace beside the choice. Before leaving, "Your team" opens names and faces. The existing Tabletop look is retained. Three inspected native 1280×800 plain PNGs: [desk](images/2026-11-11-codex-team-desk.png), [names and faces](images/2026-11-11-codex-team-names.png), [handing over work](images/2026-11-11-codex-team-work.png).

## PRs reviewed this session

PRs [158](https://github.com/nhazelett/ocs-game/pull/158), [159](https://github.com/nhazelett/ocs-game/pull/159), [160](https://github.com/nhazelett/ocs-game/pull/160) and [161](https://github.com/nhazelett/ocs-game/pull/161): green and mergeable at the fix pass; Fable's latest rechecks accepted their earlier corrections. No submitted review, inline thread or undecided objection required a code fix. Earlier sessions' fixes are not counted again.

Read the session/kickoff instructions, required SPEC sections, ADR-0001 to 0003, DISSENT, decided objections, complete Codex backlog, QUESTIONS, FOR-NICK, the latest Codex handoff and Fable's DESK handoffs. Q18 was initially pending. Fable answered it on PR 161 during this session at d06d2fe: cross-agent stacking is explicitly permitted, DESK-22 on fable/DESK-02 (PR 162). The earlier 04:00 handoff has a continuation update on PR 161 at 3da6038. This new evidence unblocked implementation.

## Changed

DESK-22 is implemented on codex/DESK-22, for an ordinary PR to main. The remote claim was created before implementation. It combines PR 161's reviewed screen stack (carrying 158–160) with Fable's DESK-02 team contract at 08c3f97, as Q18 directs. Both dependency heads are retained as commit parents; no rebase.

The new Game team surface binds SessionView.Team, HoursIfAssigned and AssignAction. The anonymous hours controls are hidden. Each personal bar shows assigned hours out of that person's day; names open the read-only roster and work queue. A handoff happens only when "Give them this task" is pressed. New work remains with you by default. The Sim decides the new owner's pace and workload; Game contains no work-hour rules.

The preparation flow offers all three names and the existing fictional portrait set, including a neutral fallback. Names must contain 1–40 characters. Applying remembers preferences in user://team.cfg for future new deployments. GameSessionBridge passes TeamPick inputs to Session.Create; InputLog.Team owns the deployed choices, so save/resume and replay preserve that game's team rather than consulting later preferences. The player's chosen name is shown alongside "You". Older scenarios retain their actual single-person roster until Fable's DESK-12.

The team view covers the map, so harbor ambience fades while it is open. The developer quiet-day stop message no longer says an anonymous "someone". Added TeamSmokeTest to CI; existing Chrome checks now verify the visible personal bar, and Music checks the new map-covering surface. Updated only DESK-22's Status cell and Game documentation. No new Sim/scenario/asset edits: those changes in the PR are the inherited DESK-02 dependency. No asset generation or download spend.

## Why

D2 asks who is doing the work and makes staff time personal. The roster makes ownership, pace and available time inspectable; explicit task handoff keeps assignment concrete. Q18 authorizes building against the ready Fable branch without waiting for a merge. The owner-set five-open-PR limit overrides the repository's ten-open wording.

## Validated how

Solution and Game builds pass. All 642 .NET tests pass (521 Sim, 121 Harness); all 72 Python tests pass. Scenario validation reports five scenarios, one rules file and zero errors; provenance, conflict-marker and whitespace checks pass.

The complete native CI block passes with 44 pass markers and process exit 0, with no ERROR or FAIL lines. This covers both kit cases and all existing screen suites plus TeamSmokeTest. The final graphical Team suite also passes through actual controls, including invalid-name rejection and correction, portrait rendering, remembered settings in a fresh shell, three exact personal bars, aligned layout and an on-screen End the day button, read-only inspection, the exact assignment action and subsequent Sim day, save/resume, and the legacy roster. State hashes are compared with direct Sim play.

The final screenshots are plain PNGs outside LFS: desk 120,383 bytes; names 51,704; work 78,665. Each is 1280×800 and under 400 KB. Visual inspection found and corrected uneven bar alignment and stale setup feedback. Audio uses the dummy driver; no human listening verdict is claimed. Some pre-existing native suites still print nonfatal ObjectDB warnings.

Main was verified as 3c387cd through the connector. CLI fetch cannot authenticate in this shell; isolated local snapshots were hydrated with exact Git blob hash verification. The merged local source preserves both dependencies and both backlog updates. Publication uses the tested file contents and a fast-forward from the remote claim; no successful CLI pull is claimed.

## Unresolved

No implementation, account, tool or cost blocker. No FOR-NICK addition. No fresh Windows export. Fable/Claude review: placement of the team controls, names/faces flow, task-handoff wording and the default that new work stays with you. Spoke B's single-person roster is intentional in DESK-02 and waits on DESK-12, rather than inventing staff in Game.

## Next

Opening DESK-22 brings Codex to five open PRs, so new work stops. Fable reviews and merges on its own schedule; the screen stack 158–161 and Sim dependency 162 are named in the PR. After room opens, start with a fix pass, then DESK-23 on the Fable ref approved in Q18. Codex merged no PR, pushed nothing to main, force-pushed nothing and deleted no shared branch.

PRs opened: DESK-22. PRs fixed: none needed. FOR-NICK additions: none.
