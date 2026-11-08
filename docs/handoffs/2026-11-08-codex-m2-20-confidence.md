**Work session handoff**

Expected look of this build and why: default M2 desk and schematic map. A refused handshake now adds a gold square and confidence-lost text to the affected site, and text beside its capability rows. The debrief names shop work deferred after lost confidence.

## PRs reviewed this session

PR 64 (M2-05): all four applicable CI jobs passed; tag-only Windows export skipped, mergeable and clean. No open Codex PR required a fix at session start. Read the workflow, latest handoffs, required SPEC/ADRs and decided objections as recorded in the M2-05 handoff.

## Changed

M2-20: map and Board bind LostConfidenceSites; debrief labels PriorityDeferred. ConfidenceSmokeTest and CI cover refusal, only the affected site's badges, the existing Compliance inbox record, expanded gap label and new-game reset. Update only this item's Status cell. Stacked on PR 64 because these changes extend the same board and ledger file.

## Why

M2-14's Sim contract is merged. Display its consequences without reproducing queue-priority rules in Game. The inbox already displays Compliance through MorningBoard; the integration test proves that path works.

## Validated how

Local solution build, 161 Sim + 97 Harness tests, scenario validation (3 folders), provenance pass. Godot .NET build and CONFIDENCE_SMOKE_PASS pass against a matching independent Sim run: after refusal and a deliberately overloaded work queue, the exact shop-hours record appears, PriorityDeferred reaches an expanded debrief, final hashes match, and a new game clears marks. Diff whitespace checks pass. Temporary .NET 8.0.427 and Godot 4.6.1; connected-app snapshot of main 4603d92, shell clone/pull unavailable as recorded previously.

## Unresolved

None for M2-20. No Nick-only entry, merge, main push, force-push or shared branch deletion.

## Next

M2-21 performance inbox, then M2-22 Windows build and M2-23 disruption displays/validation. Stop at five open Codex PRs per the triggering request.
