**Work session handoff**

Expected look of this build and why: the existing M2 desk gains funding detail. Ledger rows show purpose (any for unrestricted funds), planned and delayed release days, and a visible selection when opened from a funding notice. Unusable or wrong-purpose funding choices are disabled with reasons. Gap evidence says Funding lapse. No new screen.

## PRs reviewed this session

PR 85 (M3-11): CI run 42532018043 succeeds, mergeable, no inline threads; Fable's review says ready to merge. No fix required. No PR merged. Main is still 353967d. Read CODEX-SESSION, CODEX-KICKOFF, required SPEC sections, ADR-0001 through 0003 and funding contracts, latest Codex/Fable handoffs, QUESTIONS, FOR-NICK and the three decided objections. No unanswered question to Codex or pending objection. The triggering job's five-open-PR ceiling overrides the documents' ten.

## Changed

Claimed remote codex/M3-13 from main before implementation; local worktree /workspace/scratch/d25863e22b41/ocs-m3-13. M3-11 is submitted in PR 85. M3-12's tested local implementation remains blocked by the previous session's upload denial (FOR-NICK #4); the parent-thread history shows no approval reply. It was not retried or included here. M3-09 remains behind the M2 gate.

M3-13 binds funding purpose and planned/current availability in the ledger, disables funding picker entries using FundingOption.PaysForThis and UsableToday with purpose/release/expiry reasons, links funding board items to the selected and focused ledger row, handles the general no-funds notice, and labels FundingLapse in the debrief. Selection clears on scenario changes. Sim remains authoritative.

Python validation mirrors the C# checks for funding capability references, release before expiry, funding.late targets and a positive days or daysRange. Added six validator tests and a Godot smoke scene wired into CI. Only M3-13's Status cell changed; Fable's headline stays untouched. Copied the previously local-only FOR-NICK #4 record so its existing unresolved blocker is available with this branch.

## Why

M3-13 is the highest unblocked Codex item after the fix pass. It explicitly permits extending existing M2 panels before the gate. Funding expiry and release notices were already in Sim but the desk lacked purpose/delay context and their links had no navigation case.

## Validated how

.NET SDK 8.0.427: solution and game builds pass with zero warnings/errors; 223 Sim and 116 Harness tests pass. validate.py: three scenario folders, one rules file, zero errors. All 28 Python tests, provenance.py, conflicts.py, actionlint and git diff --check pass.

Godot 4.6.1 .NET: FUNDING_SMOKE_PASS across the full 90-day seed-1 scenario, covering late notice, fiscal-year expiry, both funding lines disabled on day 77, release, each funding notice's real button and selected/focused ledger row, generic no-funds notice, readable debrief factor and unchanged final Sim hash. Temporary purpose-restricted scenario verifies a usable wrong-purpose line is disabled with a reason, correct-capability funds re-enable, and edits leave state unchanged. Existing Action, Ledger, Morning, Debrief and BPA smoke scenes pass.

Shell git pull was attempted and failed because git/gh has no credentials. Connected GitHub main metadata and the complete recursive blob listing were checked against local snapshot c61ff0a: all 342 local tracked files match main exactly; sources/README.md is the one remote-only file and is neither read nor imported. Publication will preserve the actual remote parent and base tree, including that file. No asset or Fable component changed. Unrelated generated UID files are excluded.

## Unresolved

M3-12 publication remains pending Nick's approval under existing FOR-NICK #4; no new technical question. An unparameterized create_tree call was rejected by automatic approval review before any file payload was supplied, citing unspecified upload contents/authorization and the older M3-12 blocker. The concrete M3-13 payload was then accepted after verifying the standing job through user_message_read. Opened ordinary PR 89 from codex/M3-13 with the actual main parent and base tree, using an expected-head, non-force update. No new publication blocker or FOR-NICK row was created. GitHub CI is pending at handoff.

## Next

Fable reviews/merges on its schedule. Next Codex item is M3-14 (CORs), after a fix pass and subject to five open PRs. M3-12 waits for the named approval; M2 human playtest remains Nick's task. No merge, main push, force-push or shared-branch deletion was performed.
