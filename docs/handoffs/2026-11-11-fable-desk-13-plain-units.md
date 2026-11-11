**Work session handoff**

Fable, scheduled run, 11 Nov 2026 around 07:00 UTC.

Expected look of this build and why: no new screen. Wherever a request, an award or the debrief gives a forklift quantity it now says "3 forklifts" instead of "3 unit-days", and ground transport says "vehicles" instead of "vehicle-days". The forklift request's core part reads "Forklifts" (it read "MHE equipment"). Reason: DESK-23's screenshot shows "Peak demand: 3 unit-days" on the first request a new player works, which is the kind of word Nick stopped on at the M2R gate.

## PRs reviewed this session

- 168, 169, 170 (Fable DESK-08, 09, 10, opened by the 04:00 run): ready to merge as the Fable stack; CI green.
- 171 (Codex DESK-23, requirements without checkboxes; stacked on 161 and 168): ready to merge after its dependencies, with two non-blocking follow-ups in the PR comment (the ask button should name the person; one line before the ask list saying why you would ask).
- 158 to 167: heads unchanged since the 04:02 comments; no new comment.
- Combined tree checked locally: 171 merged with 170 is clean (backlog auto-merged, no conflict markers), 572 Sim and 121 Harness tests pass, validate.py and provenance.py pass, the game project builds.

## Changed

- `scenarios/rules/rules-2025-10.json`: MHE `unit` "unit-days" to "forklifts"; transport `unit` "vehicle-days" to "vehicles"; MHE core line label "MHE equipment" to "Forklifts".
- Goldens, text only plus each report's state hash (the hash covers record text): tutorial approach1-taught, approach2-cash-for-waste, policy-greedy-seed1, policy-textbook-seed1; sustainment approach1 to approach4, policy-do-nothing-seed1, policy-greedy-seed1, policy-textbook-seed1. Checked by script: after swapping the three words and masking the hash line, every report is identical. No number moved. Spoke B goldens unchanged.
- Backlog: new row DESK-13; status line mentions it.

## Why

docs/design/desk-redesign.md D9 (the customer's ask stays in view, in the customer's terms) and the M2R gate lesson that a cold player stops on jargon.

## Validated how

`dotnet test src/LinesOfSupport.sln` (572 Sim, 121 Harness), `python3 tools/validate/validate.py`, `python3 tools/validate/provenance.py`, `pytest tools/validate` (72).

## Unresolved

- The debrief can say "supply 1 forklifts against demand 1"; the plural was already wrong with "unit-days". Left for now; a plural helper is a larger text change across every capability.
- A stray claim branch `fable/DESK-12` exists at the DESK-10 head from this run (it was pushed before noticing DESK-12 is already Spoke B's team, after the gate). Deleting it was refused by the proxy. It holds nothing new; the next session to take DESK-12 after the gate should ignore its date.

## Next

Nothing unblocked for Fable before the DESK gate. Merges wait for Nick's check-in: Codex 158 to 161, then Fable 170 (or this PR, which carries 162 to 170), then Codex 171.
