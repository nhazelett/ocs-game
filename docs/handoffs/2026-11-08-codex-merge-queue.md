**Work session handoff**

Expected look of this build and why: no visible change. This session records the fix pass and the pending prerequisite merges; PR 43's Directory and Vehicles panels are unchanged.

## PRs reviewed this session

- PR 43 (M2-15), the only open Codex PR: head `43ad992accf1ab59906fe82e15a911d86afdd529` is mergeable. The test, scenarios, provenance and godot-shell checks passed in CI run 42461184664; tag-only export-windows was skipped. There are no conversation comments, submitted reviews or inline review threads. No fix was needed.
- Read Fable PRs 46 through 51 and the latest Fable handoffs for dependency and queue status. Their implementation and merge ownership remain with Fable.

## Changed

Added this handoff to `codex/m2-15`. Read CODEX-SESSION, CODEX-KICKOFF, required SPEC sections, ADR-0001 through ADR-0003, DISSENT, decided objections, backlog/questions, and current Codex/Fable handoffs. No objection or unanswered question requires a Codex response.

## Why

Current main is still `80e0e3bb68774681cf0426fd30e1b77b8a0ab2c4`. M2-05's Q11 contract is supplied in open PR 46 but absent from main. Fable's newer backlog in PR 51 also assigns M2-20, explicitly dependent on PR 47 being merged. All other Codex items are implemented or merged. Fable's latest handoff asks later runs to end promptly instead of stacking more code while the merge queue is blocked. The five-PR cap is not the blocker: only one Codex PR is open.

## Validated how

Verified current main, PR 43 mergeability, comments/reviews/threads and checks through the connected GitHub app. Compared main's Arrangement with PR 46's `777b0f18ce7d7c13ddfe196bc0c2b4832a47a7aa`: only the latter supplies `UnobligatedIncurredCents`. Reviewed this documentation-only change for conflict markers and template coverage. Local clone/pull could not authenticate; the connected app supplied repository access. The .NET SDK is absent locally, so no local build or test is claimed. CI evidence above belongs to the pre-handoff head.

## Unresolved

Prerequisite merges remain pending. The existing FOR-NICK #1 in Fable PR 51 records the scheduled merge permission refusals; nothing was added to FOR-NICK.md this session. No new PR or code fix was needed. No PR was merged, main updated, force-push made or shared branch deleted.

## Next

Resume with the fix pass, then claim M2-05 once Q11 is integrated, observing the three-hour claim rule. Bind the supplied field and existing vendor/vehicle projections; run all required local checks before opening the feature PR. Take M2-20 after PR 47 is merged and its backlog assignment reaches main.
