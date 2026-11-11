**Work session handoff** (Codex, 11 Nov 2026, review captions and native shutdown)

Expected look of this build and why: the reported map change reads "Fuel 0% supplied", making clear that the number is supply coverage. Other shortfalls use the same wording, including partial supply. Restoration and supplier-failure messages stay specific. The caption still sits above the site's equipment. [Day-eleven map](images/2026-11-11-codex-supply-caption.png). Smoke-test cleanup has no visible effect.

## PRs reviewed this session

Checked live main and the four open Codex PRs: [158](https://github.com/nhazelett/ocs-game/pull/158), [159](https://github.com/nhazelett/ocs-game/pull/159), [160](https://github.com/nhazelett/ocs-game/pull/160), [161](https://github.com/nhazelett/ocs-game/pull/161). All started green and mergeable. Conversation reviews were read; no submitted review or inline thread was pending. The prior 22:00 handoff and tested remote trees confirm the menu and ledger findings were already addressed. No new fix to 158 or 159 was needed.

Read the live session/kickoff, required SPEC sections, ADR-0001 to 0003, DISSENT and the four decided objections, backlog, QUESTIONS, FOR-NICK, latest Codex handoff and Fable's DESK handoff. No unanswered question to Codex or objection awaiting its response was found. The delegated five-open limit overrides the repository's ten-open wording.

## Changed

PR 160: SupportSmokeTest and PerformanceSmokeTest now free the full shell, clear the reference, wait two frames, collect managed finalizers while Godot is running, then wait another frame. They verify the shell is gone before printing the pass marker and quitting. Existing behavior assertions remain. This responds to Fable's reports of exit crashes after pass markers in both PRs. The fix and its review response live on the same DESK-25 branch.

The updated PR 160 CI run (267, 42929115996) then failed after PEOPLE_SMOKE_PASS with leaked Theme and StyleBoxFlat wrappers, while the first updated ART-09 run (268) passed. That concrete recurrence justified a shared ownership correction instead of a blind rerun: theme setup releases managed font, style and icon wrappers once the native Theme retains them, and the shell releases the initial/replacement Theme wrapper once the native Control owns it. The portrait gallery follows the same rule. LookSmokeTest forces collection after each style change, then checks the native paper style and arrow icon before continuing through every screen. Visual parameters are unchanged.

PR 161: carries the updated PR 160 as an additional commit parent, preserving both branches' history. Clarified the supply caption requested in its review. MapSmokeTest checks the real zero-supply caption, a partial-supply projection, unchanged specific restoration/failure messages, known filtering, equipment clearance and unchanged Sim state. Added the inspected native PNG and this handoff; carried the PR 160 handoff update.

No Sim, scenario, rule, asset, provenance, backlog Status, QUESTIONS or FOR-NICK change. No generation or download spend. No new PR or remote claim.

## Why

Fable noted that "Fuel short · 0%" could mean zero percent short. The supplied wording makes the reported number explicit. The native exit crashes can fail CI despite completed assertions; pass markers are accepted only with successful process exits.

The remaining Codex DESK screen rows are blocked on their Sim rows in main. DESK-02/03/04 exist as unmerged Fable PRs 162/163/164; they have not been treated as merged contracts. DESK-22 is the next screen row after DESK-02 lands; DESK-23/24/26/27 wait on DESK-03/05/06/07. DESK-20's warning binding waits on DESK-04 and the office vehicle on DESK-10. Other Sim depth remains paused through the DESK gate. No unblocked, unclaimed Codex row remained after the fix pass.

## Validated how

Verified the isolated local ART-09 and DESK-25 baseline trees against all paths in their live GitHub trees. Live main remains 3c387cdda4059b06cbca389321a4fc5b06a34fd4. CLI pull failed because this shell lacks GitHub HTTPS credentials; the working connector supplied live state and remote publication. No successful CLI pull is claimed and no credential change is needed.

Solution and Game builds pass; 632 .NET tests (511 Sim, 121 Harness), 72 Python tests, validate.py, provenance.py, conflicts.py and whitespace checks pass. A reused MSBuild worker initially returned a silent failure; build/test succeeded with isolated workers (-m:1 -nr:false). No source fix or assertion change was needed for that tool issue.

The final shared-resource correction passes the complete native CI block on both exact source trees: DESK-25 has 42 pass markers and ART-09 has 43, both exit 0 with no ERROR lines. This includes the original behavior assertions and both kit cases. People, Support and Performance additionally each pass ten consecutive native runs on the final ART-09 source, all exit 0 with no ERROR lines. The Look suite checks all three themes after forced collection.

The graphical Map suite exits 0; its refreshed screenshot is pixel-identical after the ownership fix. The committed capture is a visually inspected 1280 by 800 plain PNG, 151,460 bytes, outside LFS. Remote publication uses the tested file contents and fast-forwards with expected-head checks. Fresh CI runs follow the final branch updates; their final state is recorded in the session summary.

## Unresolved

Some existing suites still report nonfatal ObjectDB exit warnings. Deterministic wrapper release and repeated successful native exits address the observed shutdown problem; they do not prove every engine shutdown flake eliminated. No fresh Windows export or human audio listening verdict is claimed. Fable/Claude review: the supplied wording, map screenshot and shared native-resource ownership change. No development/publication blocker or FOR-NICK addition.

## Next

Fable reviews and merges in order 158, 159, 160, 161 on its own schedule. Four Codex PRs remain open, within the owner's five-PR limit. Resume with a fix pass, then DESK-22 once its Sim dependency is merged and the backlog unblocks it. Codex merged no PR, pushed nothing to main, force-pushed nothing and deleted no shared branch.

PRs opened: none. PRs fixed: 160 and 161. FOR-NICK additions: none.
