**Work session handoff**

Expected look of this build and why: no visible change in this PR. This corrects the ledger item's completion label and records the review queue accurately.

## PRs reviewed this session

Opened PRs 31, 33, 34, 35, 37, 39, 41, 42, 43, 44 and this M2-05 follow-up during Nick's check-in. Fable merged 31, 33, 34, 35, 37 and 39. Earlier fix passes refreshed 31/35/37 with main without force-pushing and fixed the morning navigation test after additional ledger buttons appeared. PRs 41/42/43 are mergeable, all applicable CI green, no comments; 44 was just submitted. No Codex merge, main push, force push or shared-branch deletion.

## Changed

This metadata-only M2-05 follow-up corrects Done to Blocked on Fable (Q11). The merged PR 37 contains a dependency note rather than the required panels. Q12 records the five-open-PR stop and asks Fable to reconcile its status headline and answer Q11. BPA-related known Directory/Vehicles UI is in PR 43; full funding/arrangement ledger remains undelivered.

## Why

A merged dependency request must not hide unfinished acceptance criteria. Game must not recreate Sim's accounting rule to manufacture the missing per-arrangement unobligated-incurred value.

## Validated how

Pulled main and verified Arrangement still has no UnobligatedIncurredCents and Q11 remains pending. Source baseline builds cleanly: 222 .NET tests, scenario validation and provenance pass. Other feature PRs add native tests comparing their commands/projections with direct Sim states; details in their individual handoffs. This diff changes only backlog metadata and this handoff.

## Unresolved

Five open Codex PRs after this submission; stop new work. M2-05 waits for an additive read-only Sim field/projection (Q11). Fable owns the headline and merges on its schedule. Nothing added to FOR-NICK.md; no technical dispute or Nick-only task.

## Next

Next session: pull main, fix own PRs first, count the open queue, then resume M2-05 once Q11 is supplied and there is room below five. Read current session/kickoff/backlog files for any new assignment; do not merge PRs.
