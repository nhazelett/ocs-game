**Work session handoff**

Expected look of this build and why: no visible change. This session records the fix pass and the remaining M2-05 dependency; PR 43's default-theme Directory and Vehicles UI is unchanged.

## PRs reviewed this session

- PR 43 (M2-15): the only open Codex PR. Head `77bf543f948d967fcfab69d69a154d7d466c51af` is mergeable, with no conversation comments, submitted reviews or inline review threads. The test, scenarios, provenance and godot-shell checks succeeded; export-windows was correctly skipped. No code fix was needed.
- Read Fable PR 46 (Q11) and PR 47 (M2-14) to identify the pending ledger contract and debrief addition. Neither is a Codex PR; neither was modified or merged.

## Changed

Added this handoff on `codex/m2-15`. Read CODEX-SESSION, CODEX-KICKOFF, the required SPEC sections, ADR-0001 to ADR-0003, DISSENT, the decided objection records, backlog/questions, and the current Codex/Fable handoffs. No objection is waiting for a Codex response and no unanswered question is directed to Codex.

## Why

The fix pass is clear and the queue is below five, but all other listed Codex items already have implementations or merged PRs. M2-05 remains blocked on Q11 in main. Fable has supplied `Arrangement.UnobligatedIncurredCents` in open PR 46; main still lacks it. Do not duplicate Sim accounting in Game or start an unassigned feature.

## Validated how

Read current main `80e0e3bb68774681cf0426fd30e1b77b8a0ab2c4` and PR state through the connected GitHub app. Verified that main's Arrangement lacks the Q11 field and that PR 43's backlog cell already marks M2-15 implemented. Shell clone failed because Git/gh has no authenticated GitHub session; the app provided repository access. No local build or test is claimed: the .NET SDK is absent in this environment, and this commit changes documentation only. The CI results above belong to PR 43's pre-handoff head.

## Unresolved

M2-05 resumes once Fable's Q11 change reaches main. PR 43 remains open for Fable. No Nick-only action was identified and nothing was added to FOR-NICK.md. No PR was merged, no main or forced push was made, and no shared branch was deleted.

## Next

Next Codex session: read current main, perform the fix pass, then claim M2-05 if Q11 has landed (respect the three-hour branch claim rule). Bind the ledger to the supplied read field and use the existing vendor/vehicle projections; run all required local checks before opening the feature PR. Read any updated backlog first.
