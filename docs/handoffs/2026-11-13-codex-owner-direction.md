**Work session handoff** (Codex, 13 Nov 2026, owner direction and ART-15)

Expected look of this build and why: ART-15's published desk hierarchy and its eight before/after PNGs remain the visual result. This follow-up changes workflow records and native test resource cleanup; it adds no new player-facing behavior or art.

## PRs reviewed this session

Checked the exact published ART-15 head c0478c5510be65f4e825e6ca1bddfc3a02adfc6c (tree cd62adb7d11839fd6f7991792955910d87ecd83d) against current main f7a51ddf5ded525152ad2422253d81a63a287588. The published tree exactly matched the prior local result. Continued on an isolated checkout of that remote head, preserving both histories. During verification, main advanced to 89568b86ea3c1277572b53737095bd62edfc55ce (Fable PR 210): only the backlog headline and a 27-line ART-12 review handoff changed. Merged that main update into this branch without conflicts, keeping Fable's headline and review; no Game/Sim change came from main. ART-12 remains with its original session; no concurrent update to its branch or PR was made.

## Changed

- CODEX-SESSION.md: added the 13 Nov authorization/review rule. Routine backlog implementation, branch pushes, project code/artifacts and ordinary non-draft PRs proceed under owner authorization without another Nick signoff for every stage. Claude/Fable review continues after approval; later problems require evidence and a fix on the open branch or in follow-up work.
- CODEX-KICKOFF.md: added the same standing authorization and a link to the detailed rule and this owner record.
- FABLE-SESSION.md: explicitly instructs Fable to review approved work too, preserving its existing CI, screenshot, art, combined-tree and merge checks. Later visual/performance/behavioral findings remain actionable.
- All three retain platform/security/access/provenance/correctness requirements. Repository text is not permission to override a platform denial. Codex's no-merge, no-main-push, no-force-push and no-shared-branch-deletion restrictions remain; component boundaries, active queue limits and cost/credential restrictions remain.
- FOR-NICK #17 records Nick's answer while preserving the original denied action and its reason. ART-15's Status cell is no longer awaiting a fresh owner signoff. Q19 notifies @Fable through the established questions channel; review remains pending without a new authorization gate. The original ART-15 handoff links here so its historical blocker is not mistaken for the current owner decision.
- Fixed a concrete verification issue in DeskHierarchySmokeTest: dispose the managed wrappers returned by the theme font, icon and style getters, then flush finalizers after freeing the shell and before quitting Godot. The first new run printed its assertion pass marker and then exited with signal 11 on an unsafe StyleBoxFlat reference. That run was counted as failed. The getter wrappers were not explicitly released; this change addresses their lifetime, without weakening any assertion. Three subsequent runs exited cleanly. It does not prove every possible Godot shutdown flake eliminated.

## Why

Nick's direct message on 13 Nov 2026 at 06:48 UTC immediately followed the request to open ART-15's ordinary public PR from codex/ART-15 to main, without merging. Verified the message and surrounding request through the user-message history:

> If we keep stalling, amend the rules. We can’t keep hitting bureaucracy with Claude. I would rather we push forward and catch bad performance in stride, so let Claude know to review stuff even if it’s been approved

This is his direction to proceed with that ART-15 PR after verification and to remove repeated owner signoff for routine authorized work. It is not a waiver of Claude/Fable review, a permission change on the platform, or authority for Codex to merge.

The observed stalls came from action-level approval review. The repository already said ordinary PRs were authorized without asking each time; branch publication and the required local checks had succeeded. The PR tool's stated reason was that a non-draft public PR was an external commitment without clear authorization for that exact action. Earlier source/blocker-note uploads were separately rejected as uncommitted/private or sensitive to an unverified destination; verified commit state, owner/public repository access and byte-identical existing public content allowed one evidence-based retry of each. These were approval-context/authorization-scope decisions, not GitHub authentication or failing game checks. The internal reviewer's decision process is not visible, so no claim is made about why it did not accept the older standing authorization. Updating Markdown cannot disable that enforcement. Nick's new direct ART-15 instruction is the materially new authorization for retrying the known-unexecuted PR action once; any further denial must be reported rather than bypassed.

## Validated how

- Fresh .NET SDK 8.0.415 Release solution build and all 754 .NET tests pass: 633 Sim and 121 Harness, zero failed/skipped. Eight existing solution warnings. Final Game build has zero warnings/errors. Repeated the required solution build and all 754 tests after integrating main 89568b8; they pass on the integrated tree.
- Godot 4.6.1 .NET import completed. After the retained-wrapper fix, DeskHierarchySmokeTest passed three runs with clean process exits; TeamSmokeTest, LookSmokeTest and MapCalloutSmokeTest passed. The latter coverage includes real hours, assignment/save/restart, all three looks and map-label layout. After the documentation-only main merge, one further hierarchy run also exited cleanly (four clean runs total after the fix). Team/Look retained the existing nonfatal ObjectDB exit warning. A printed pass marker alone was not accepted as process success.
- Scenario validation: five folders, one rules file, zero errors. Provenance, tracked conflict-marker and whitespace checks pass.
- All eight existing review PNGs match the published ART-15 bytes, remain 1280x800 and below 400 KB. The earlier visual inspection and twelve-suite results remain in the original ART-15 handoff; only the four named native suites above were rerun in this follow-up. Screenshots were not recaptured because the only Game change is test cleanup.
- No Sim/scenario/rule, asset, CI policy, required check or permission configuration changed. The UI implementation remains the published ART-15 result.

## Unresolved

[PR 211](https://github.com/nhazelett/ocs-game/pull/211) opened successfully as an ordinary non-draft PR against main on 13 Nov at 07:06 UTC, verified head c4ad494218e3e2923de6a88d60d4cc67cae52ccf and base 89568b86ea3c1277572b53737095bd62edfc55ce. The known-unexecuted PR action was retried once under Nick's new direct authorization and accepted; no alternate tool or permission bypass was used. Fable's visual review remains required. Hosted CI was still running when this publication record was written. No fresh Windows execution or ordinary-laptop performance benchmark was run; the earlier Compatibility-renderer captures and those limits still apply. Workflow documentation cannot guarantee that future platform approval checks will accept a requested action.

## Next

Fable reviews PR 211, its artifacts and the combined tree even though Nick authorized publication, checks CI on the latest head, and records/fixes later problems from actual evidence. No merge by Codex. The PR history and FOR-NICK #17 preserve the earlier denials; no new owner approval row is needed for already-authorized routine work. No main push, force-push or shared branch deletion.

PRs opened: 211. PRs fixed: no existing open PR changed; ART-15's newly observed smoke shutdown fault fixed before publication. FOR-NICK: #17 answered; no new row.
