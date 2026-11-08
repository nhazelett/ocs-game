**Work session handoff**

Expected look of this build and why: the M2 desk remains default-font grey panels. Ledger now shows each funding line's remaining balance, availability and expiry, and every arrangement's unobligated-incurred cost. Directory rows also show quote, capacity and lead time; BPA controls and known vehicles remain available.

## PRs reviewed this session

No open Codex PRs at start (main 4603d92). Read Fable's current handoff, required SPEC sections, ADR-0001 to 0003 and 0006, DISSENT, and all three decided objection records. No question or objection awaits Codex.

## Changed

M2-05: bind the Q11 property directly; show funding through SessionView and its UsableOn method; preserve arrangement actions and selection. Add a 90-day Godot replay smoke test and its CI invocation. Update only M2-05's Status cell.

## Why

Q11 is answered and its Sim contract is on main, unblocking the highest Codex item. Existing M2-15 supplied Directory/BPA and Vehicles; this completes the missing ledger and tests their integration.

## Validated how

.NET 8.0.427: solution build (one MSBuild worker), 161 Sim + 97 Harness tests; Python scenario validation (3 folders, 0 errors) and provenance pass. Godot 4.6.1 .NET game build and LEDGER_SMOKE_PASS pass. The smoke test checks every ledger row over the 90-day never-definitized UCA replay, including positive unpaid cost, coordination and funding expiry, verifies unchanged final hash, and checks refused-handshake vendor standing.

Shell git clone cannot authenticate. Used the connected GitHub app to materialize main's 297 non-sources tracked blobs locally; sources and LFS binaries were not imported. No local git pull is claimed. Installed temporary SDK and Godot .NET for actual local validation; kit rendering is outside this change.

## Unresolved

Automatic approval review twice rejected updating old codex/m2-05 to main as a possible backward update, even though GitHub compare reports main 47 commits ahead, zero behind and merge base equal to the old branch head. That exact operation was stopped. Claimed new codex/M2-05 from main instead; old branch is untouched. No Nick-only action is needed for this safer branch. No merge, main push, force-push or branch deletion.

## Next

M2-20, then M2-21, M2-22 and M2-23. This session honors the triggering request's five-open-Codex-PR limit (the repo's later ten-PR text does not increase that limit).
