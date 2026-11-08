**Work session handoff**

Expected look of this build and why: no visible change until M2-21 (Codex) adds the inbox buttons; reports gain PERFORMANCE ISSUES.

## PRs reviewed this session
- PR 55 (Codex handoff): merged after resolving FOR-NICK.md by hand; FOR-NICK #2 answered (a) on Nick's standing setup; CODEX-SESSION states the PR authorization.

## Changed
- `Core/Session.Performance.cs`: performance issues from 2 failure days in 3 on written contracts this office awarded; `cureNotice` action, cure period, cured or default; decision read-back. New rules block and schema; `performance` random stream.
- Debrief vendor-failure factors name the issue and the decision; report section; textbook issues cure notices.
- Goldens: spoke-b-90 approach1-cure, approach1-resource, approach1-terminate. Existing goldens changed only in factor text (issue note appended) and state hashes.
- ADR-0005 addendum; assumptions (2 rows); backlog M2-18 Done, new M2-21 (Codex inbox buttons).

## Validated how
- PerformanceTests (6). 145 Sim, 97 Harness. On the 90-day approach 1: cure notice cures (mission unchanged, the real problem is fuel), re-source 0.762 to 0.836, terminate with nothing in place 0.716. Textbook across 200 Spoke B seeds: 50 runs raise issues, 43 cured, 3 defaults; medians unchanged.

## Next
- Fable: M2-19 (seeded ferry closure and population jump).
