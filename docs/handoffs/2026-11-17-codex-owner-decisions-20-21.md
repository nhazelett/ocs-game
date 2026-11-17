**Work session handoff**

Expected look of this build and why: no visible change. This documentation-only update records owner decisions; it does not implement gameplay changes.

## PRs reviewed this session

PR 307: addressed Fable’s 17 November 18:49 UTC request for the owner’s words and source. Its existing head passed hosted CI; no conflicts or inline review threads were open. This is a decision-record fix, not implementation review.

## Changed

Recorded answers and dates for FOR-NICK #20 and #21 only; all other decision rows are unchanged.

## Why

Nick approved both recommendations in his direct OCS project reply to ChatGPT on 17 November 2026 at 18:19 UTC. His decision was: “Concur with both”. The two recommendations in that reply were the replacement workaround now with contract modification after the playtest gate (#20), and the full-page request and purchasing workspace (#21). Only this project decision is recorded here; no surrounding conversation is reproduced.

Nick approved both recommendations on 17 November 2026. For #20, explain the replacement workaround now and add the bounded contract modification action after the playtest gate. For #21, use a full-page request and purchasing workspace rather than widening or retaining the narrow pane.

## Validated how

Checked the current decision rows and backlog. DESK-78 is already recorded as Done in PR 286, so this handoff does not reopen it or claim a new implementation. Checked that unrelated FOR-NICK rows remain unchanged. No code, simulation, scenario or asset changes; local builds, tests and validators were not run for this connector-only documentation update. Required CI and Fable review remain pending.

## Unresolved

The current playtest gate still applies to the new contract-modification action. The workspace implementation still needs scoping, ordering and Codex's feasibility/cost assessment through the existing workflow.

## Next

Fable: review this decision record, verify the existing replacement-workaround guidance, and scope/order the full-page workspace work. Queue the bounded Add to this contract action after the playtest gate. Codex continues the established backlog and component boundaries. No merge or main-branch push was performed by Codex.
