**Work session handoff**

Expected look of this build and why: on spoke-b-90 only, and only when a log or a future OCS seat screen sends the new request. The morning board can show "External support request ... submitted through the SCO" and, 10 to 20 days later, "The external office awarded ..." as higher-HQ items. The game has no button for it yet: the OCS seat screen waits for the M2 gate (FOR-NICK #3). spoke-b (30 days) has no external support contract and is unchanged.

## PRs reviewed this session
- No open PRs at session start (the stack through PR 77 is merged). Scheduled run: no merges attempted (FOR-NICK #1).

## Changed
- fable/M3-05 (M3-05 Sim side, external support request). New `Session.External.cs`: `CheckExternalSupport`, `ApplyRequestExternalSupport`, submission when the shop work completes, `ExternalSupportClocks` (award and obligation by the external office on the approval day, or refusal when the line can no longer pay), `BuildExternalSupportPreview`, `BuildExternalSupportRequests`, debrief `ExternalNote`.
- Action `RequestExternalSupportAction` (`requestExternalSupport`). State `ExternalSupportState`, `ExternalSupportStatus`, `WorkKind.ExternalSupportRequest`, `RecordKind.ExternalSupport`. New random stream "external".
- Data: optional scenario `actors.externalSupport` (provider, offers with capability, lines, capacity, unit price, mobilization fraction, reliability, sites); schema and load-time reference checks. spoke-b-90 gets a generic "Theater base support contractor" for power (2,000 kW/day, $11/kW-day, 20% mobilization, 0.98 reliability).
- Views: `SessionView.PreviewExternalSupport`, `ExternalSupportRequests()`, `ExternalSupport`; `ExternalSupportRequestView`. Board classifies the new record kind as HigherHq / Info. Arrangement start text names the provider (`SupplierName`).
- Goldens: spoke-b-90 `approach4-external-only` (SPEC 5 approach 4 as written) and `approach4-external-with-uca-bridge` (paired with approach 3, the UCA sized as a 35-day bridge). `approach4-enduring-only` keeps its actions; only its note changed (it said the instrument arrived at M3).
- ADR-0005 and ADR-0006 addenda; assumptions.md external support row rewritten; backlog M3-05 status, new Codex row M3-15 (validate.py mirror).

## Why
SPEC 4.1: "External support contract (AFCAP-style): 10 to 20 days approval plus 14 to 30 days mobilization; large, enduring, self-managed; requested from the OCS seat, awarded by an external office, administered locally; minimum useful scope and duration". SPEC 5 approach 4: "Nothing about this helps day 7 ... the enduring solution you start now so the bridge can end".

## Validated how
- ExternalSupportTests (8): over 30 seeds the approval lag and mobilization stay in the rules ranges, the start falls inside the preview's range and after week three, the whole task order is obligated at award, the contract is supplied on the site's island and covers fuel through its supply line; the view shows an answer range, not the drawn day, until award; ten hard-stop cases, preview leaves the state hash unchanged, apply returns the same stops; a second request the line can no longer pay for is refused at award with the funding reason; approach 4 alone leaves Spoke B power down from day 7 to the day before the contract starts, tagged avoidable with the external support text; the paired golden beats both approach 4 alone and approach 3; save on day 10 and resume gives the same state hash; removing the request in a counterfactual removes the contract and lowers mission output; four bundle-validation cases.
- Mutation: with no mobilization delay 3 tests fail; with a one-day approval 1 fails; with the award-time funding check removed 1 fails.
- Seed 1 results: approach 4 alone 79% mission output, Spoke B power 39 days down (contract on site day 46); paired with approach 3, 93% with no power day down and nothing incurred unobligated (approach 3 alone 88%).
- All existing goldens unchanged except the approach4-enduring-only note line. 200 seeds unchanged: spoke-b textbook 0.9305, greedy 0.714, do-nothing 0.607; spoke-b-90 0.926, 0.671, 0.646. Crash run 1,000 games, 0 failed.
- 235 Sim and 118 Harness tests; validate.py, provenance.py, conflicts.py.

## Unresolved
- No policy uses external support, so the 200-seed ordering says nothing about it; M3-07 should give textbook the request.
- A request cannot be withdrawn before award, and the SCO never refuses or conditions one on the merits (SPEC 4.4 lists "approval with conditions"); both are assumptions.md rows.
- The unit-handshake event treats a pending contract as cover (`contract.absent`), including an external contract a month out. That matches how a pending UCA is treated today, but a desperate unit with nothing for 30 days would probably still go around contracting. A design question for M3-07 balance, not changed here.
- Price, capacity and reliability are placeholders to tune in playtest.

## Next
- Fable: M3-06 ACSA and host-nation support, M3-05 transition plan (Sim side), then M3-07.
- Codex: M3-12, M3-13, M3-14, M3-11; M3-15 after this merges.
