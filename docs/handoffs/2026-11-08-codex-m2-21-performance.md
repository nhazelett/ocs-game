**Work session handoff**

Expected look of this build and why: default M2 inbox now has persistent vendor-performance rows with the original report, Cure notice, Re-source and Terminate controls. Cure end day, decision and eventual outcome stay visible after the overnight record scrolls away.

## PRs reviewed this session

PR 64's applicable CI jobs passed. PR 66 (M2-20) is clean and mergeable, CI pending when last checked. No review comment or Codex fix was pending at start.

## Changed

M2-21: new performance desk binds PerformanceIssues and PerformanceDecision; submits CureNoticeAction/TerminateAction through SendAction; Re-source selects the same site's capability in the ordinary action editor. Buttons gate finished games, inactive arrangements and duplicate work. All issue reports and cure outcomes remain inspectable. Add PerformanceSmokeTest and CI invocation; update only M2-21's Status cell. Stacked on PR 66 because both extend GameShell wiring and CI.

## Why

M2-18 is merged and the player needs to act on under-delivery without editing an input log. Cure rules and outcomes remain wholly in Sim.

## Validated how

Local solution build, 161 Sim + 97 Harness tests, scenario validation and provenance pass. Godot .NET build and PERFORMANCE_SMOKE_PASS pass: the real 90-day approach reaches an issue; record text persists; Re-source is pure navigation; cure/termination match an independent Sim hash and expose ActionResult; duplicate commands are disabled; cure date and outcome are shown; finished games disable controls and scenario reset clears them. Temporary .NET 8.0.427 and Godot 4.6.1 on the connected-app snapshot described in the M2-05 handoff.

## Unresolved

None. No Nick-only entry or prohibited repository operation.

## Next

M2-22 Windows playtest builds, then M2-23 disruption display and validation. Stop new work at five open Codex PRs per the triggering request.
