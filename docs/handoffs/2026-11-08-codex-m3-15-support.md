**M3-15 support-provider validation**

Expected look of this build and why: the existing debrief shows readable support-withdrawal and transport-delay labels, preserving the Sim's evidence. [Actual sustainment ACSA withdrawal](images/2026-11-08-codex-m3-15-support.png) is an inspected plain PNG under 172 KB. This is the backlog's explicitly gate-safe validator work, after M2R-10 through M2R-13, while naming/tutorial data is pending.

## PRs reviewed this session

Fixed PR 112's main base and both presentation comments; every check passed and Fable merged it. Opened PR 116 for the complete illustrated map after Fable accepted OBJ-0004 and delivered M2R-09. All four required checks passed on 116; main's subsequent merge of 112 requires another integration fix pass. Codex merged no PR.

## Changed

Python now mirrors the published ScenarioBundle checks for external support's unique capabilities, valid/core lines, sites and instrument; ACSA/host nation islands, stock/capacity, priced supply and conditional instruments; host nation sites; optional withdrawal targets; and vendor lift modes. The rules schema already validates the optional lift block; a regression proves valid parsing and rejection of invalid values rather than adding a second set of rules.

Added 16 validator tests covering valid/absent blocks, broken references, conditional provider presence, null/all-capability withdrawal, schema-valid CLI rejection and lift validation. CI runs them. Added readable labels for AcsaWithdrawn, HostNationWithdrawn and LiftDelay, and a native test of an actual sustainment withdrawal's expanded evidence.

## Why

Schema-valid reference mistakes should fail the tools check before they reach the game. The check is a mirror of Fable's existing contract; it adds no support mechanics or simulation depth.

## Validated how

.NET 8 Release solution build/tests and Godot 4.6.1 Game build pass. All 72 Python validator tests pass; four scenario folders validate; provenance and conflict-marker checks pass. Existing native DebriefSmokeTest and new SupportSmokeTest pass; expanded evidence is unchanged and read-only. Screenshot rendered and visually inspected.

## Unresolved

The all-panel identifier cleanup still awaits M2R-07, so the screenshot's other debrief rows retain their pre-existing technical wording. Four existing nullable warnings in Sim's map projection remain outside this item's lane. No protected sources, rules, schema or spec edited. No new assets; no FOR-NICK additions.

## Next

Refresh this branch and PR 116 against the newly merged main, rerun affected game checks and leave both for Fable. Then work the next unblocked M2R item as its contracts/data merge; never merge a PR.
