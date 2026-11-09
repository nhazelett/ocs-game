**Work session handoff** (Fable, scheduled run, 9 Nov 2026 06:45 UTC)

Expected look of this build and why: on screen nothing moves but words. The tutorial mentor card reads "Capt Mills on the cash box." followed by his note in quotes, instead of "Your mentor on the cash box. Inbox: ...". Event news on the board and in the debrief reads "Weather stops the barge. Harbor master: no barge movements for the next three days." with no "Inbox:" tag, and words from a cast member (the fuels flight, the Spoke B first sergeant, the comptroller, the Spoke C commander) appear in quotes without a speaker prefix, since the board item already names the person.

## PRs reviewed this session

- #122 (Codex, M2R-12 plain names): ready to merge. Screenshot opened: requirement panel names the request and the customer, off lines read as sentences, no identifiers on the frame.
- #123 (Codex, M2R-17 people and mentor, contains #122): ready to merge. Combined tree of main plus #121 plus #123: dotnet test 592 passed, validate 0 errors, provenance pass, no conflict markers. Three follow-ups noted on the PR: the visit preview's "before the 0 hours already queued" sentence and the map's seventh tile (both M2R-25, Codex), and the "Inbox:" prefix on mentor notes (M2R-24, this PR).
- #119, #120, #121 (Fable): unchanged since their ready-to-merge comments earlier today; still mergeable and clean.

## Changed

`Session.FireEvents` formats event records per the M2R-24 rule; Spoke B and Spoke B 90 events from a person lose their speaker prefixes; tutorial mentor labels name Capt Mills; the schema's `playerVisibleAs` description states the rule; EventTextTests; ADR-0006 addendum; backlog rows M2R-24 (Fable) and M2R-25 (Codex) and the status line.

## Why

Reviewing #123 as a first-time player: the first thing the tutorial's mentor says arrives as "Your mentor on the cash box. Inbox:", which is a data label, not a person talking (R9). The text came from the Sim, so the fix is in Fable's lane.

## Validated how

`dotnet test src/LinesOfSupport.sln`: 481 Sim and 121 Harness tests pass. Goldens regenerated with UPDATE_GOLDEN=1 and rerun clean: every changed line is event text or the state hash (record text is hashed); scorecards and pass criteria identical. validate.py 0 errors, provenance passes, tools pytest 72 passed, conflict-marker grep clean, no *.actual.txt committed.

## Unresolved

Nothing new for Nick. Codex's mentor card shows record text, so it picks this up with no game change once merged.

## Next

Check-in merge order: #119, #120, #121, this PR (or this PR alone, which contains them), then #123 (contains #122). Then Codex's M2R-22 and M2R-25, then Fable's pre-gate walk from fresh screenshots, then the gate (M2R-23).
