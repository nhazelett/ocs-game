**Work session handoff** (Codex, 14 Nov 2026)

Expected look of this build and why: this layer has no visible change. The stack below it adds the actual daily results / Yesterday page (PR 245) and the recent/all decisions lists on Today and requests (PR 246). DESK-48 is not presented as complete; its J4 helper needs the typed target recorded in Q25.

## PRs reviewed this session

- Initial fix pass: zero open PRs, no review finding, CI failure or merge conflict to fix. Read the required session/kickoff/SPEC/ADRs, latest handoffs, questions, FOR-NICK and decided objections. Nothing awaited a Codex answer.
- Opened PR 245, DESK-46: every required hosted job is green, including godot-shell; mergeable. Four inspected native screenshots and completed-day snapshot/save regressions are in its handoff.
- Opened PR 246, DESK-47, stacked on 245: submitted with all local checks and three inspected native screenshots; hosted checks and Fable review remain pending.
- No existing PR needed a code fix. No merge, main push, force-push or shared branch deletion.

## Changed

Claimed codex/DESK-48 from published DESK-47 head d961158, then inspected D22 and the actual helper contract before changing Game. Added Q25 for Fable and changed only DESK-48's Status cell in this layer. The private Sim selection in `src/LinesOfSupport.Sim/Core/Session.WhoCanHelp.cs` chooses a stocked capability from the request and its dependencies; `HelperView` (Views/ViewModels.cs) omits that selected id. Sustainment has water and fuel in its partner stock. Game's existing J4 form is fuel-only. Binding every helper to fuel would therefore start the wrong action for some existing requests; copying the selection rule or parsing the sentence would put Sim logic in Game.

Requested an additive typed capability/target field and confirmation that the Game form should bind it. No Sim, scenario or partial Game implementation is shipped. This is a Fable interface question, not a Nick approval gate or an objection.

## Why

D22 needs the helper's words and the action opened from the row to refer to the same support. A typed binding keeps that choice in Sim and lets Game show the hours, pending/answered state and answer without inventing an action target.

## Validated how

This layer changes Markdown only on the exact tested DESK-47 stack. Required Release build and all 806 .NET tests pass (685 Sim, 121 Harness); Game Debug build and Godot 4.6.1 .NET import pass. DecisionsSmokeTest passes natively against visible controls, valid draft quote terms, exact accepted history, links, save/reset and layout. Lead/Standing, Requirement, DaySummary and EndDay checks pass. Scenario validation (five folders, one rules file), provenance, tracked conflict markers and whitespace checks pass again before this publication. No fresh screenshot is needed for this documentation-only layer; the seven inspected PNGs are in the two implementation handoffs. Windows execution and laptop performance remain unmeasured.

## Unresolved

- DESK-48 waits on Fable Q25. DESK-44 was skipped under the three-hour claim rule: its existing branch was updated at 08:49 UTC by Fable, shortly before this session began.
- No new FOR-NICK entry, account/install/cost blocker or objection. The initial live queue was empty; this session submits two completed screens and one dependency record, within the active five-open limit.
- Session implementation/verification budget is spent after the two screen changes and their integration/native checks. ART-20 is unclaimed and remains Next; no new art work was started or left half-published.

## Next

Take ART-20 unless a cleared earlier row is available. Resume DESK-48 when Fable supplies Q25's target; preserve the ask/vendor/J4 distinctions and the existing request/assignee contracts. DESK-44 stays with its recent claim until that window clears. Fable reviews and merges on its own schedule; the tutorial replay gate and paused Sim-depth rows stay as specified.
