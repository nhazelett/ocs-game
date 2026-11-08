**Work session handoff**

Expected look of this build and why: no visible change. No shipped contract has a notice clause (their inherited rows are host nation support), and the new scorecard and debrief fields default to zero and empty.

## PRs reviewed this session
- Same session as docs/handoffs/2026-11-08-fable-m3-17-file-review.md and docs/handoffs/2026-11-08-fable-m3-18-reliability-drop.md: PRs 78 to 83 ready (unchanged); 84 red only on the `godot-shell` shutdown crash (Q13), rerun failed the same way; 85 (Codex M3-11) and 86 marked ready. No merges (FOR-NICK #1).

## Changed
- fable/M3-19, stacked on fable/M3-18 (PR 87). `TerminateAction.WithNotice`; `Arrangement.NoticeDays` from inherited `noticeDays`; with notice performance ends that many days after the termination is done; without notice it ends at once and the vendor files a claim (`ClaimState`, `SessionView.Claims`, Compliance records, `WorkKind.ClaimSettlement`), settled at rules `claims.settlementDays` of daily cost capped at the days cut short, charged to the arrangement's line. No claim in default. Inherited start record names the notice.
- `ScorecardView.Claims` and `ClaimCents`, `DebriefView.Claims` (`ClaimLine`), trailing and defaulted. Rules `claims` (16 hours, 10 days), schema, bundle checks. New `Session.Claims.cs`.
- ADR-0005 and ADR-0006 addenda, assumptions.md row beside Terminations (FAR 49), backlog M3-19 status, M3-21 widened.

## Why
docs/design/M3-07-representative-scenario.md chain 3b: terminating bFuel without its 15 days' notice draws a claim; the transition golden "re-source bFuel with a transition" needs the notice to be a choice.

## Validated how
- ClaimTests (7): handover names the notice; with notice it runs on for the notice, no claim; without notice it stops at once, the claim is filed, settled as shop work, incurred and paid from om1, and shows in the scorecard and debrief; claim capped at the days cut short; notice refused without a clause and no claim then; no claim in default (cure notice then default, found by seed search); negative notice refused.
- Mutation: dropping the default exemption fails a test.
- No golden changed: 326 Sim and 120 Harness tests; validate.py and its pytest suite, provenance.py, conflicts.py.

## Unresolved
- A transition plan's cut-over terminates without notice, so on a notice contract it draws a claim; the planner could offer notice instead. Decide when M3-07's transition golden is built.
- No metric for claims (a new metric key moves every golden's hash); the scorecard counts them.

## Next
- Fable: this is the tenth unmerged Fable PR, so new Sim and scenario code stops until a check-in merges the queue (78 to 84, 86, 87, then this). Then M3-07 (Sustainment), stacked on fable/M3-19. Meanwhile review, backlog and design work only.
- Codex: M3-20 and M3-21 after the Fable PRs merge; M3-12 to M3-15 as before.
