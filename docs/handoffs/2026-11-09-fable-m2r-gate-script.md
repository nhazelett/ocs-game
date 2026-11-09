**Work session handoff** (Fable, scheduled run, 9 Nov 2026 about 04:45 UTC)

Expected look of this build and why: the same screens as main plus #119 and #120. This PR changes only docs and the backlog; no game or Sim code.

## PRs reviewed this session
- #119 (Fable, M2R-19 turnover text): ready to merge, commented. CI green.
- #120 (Fable, M2R-20 site news from a person, stacked on #119, base main): ready to merge, commented. Combined tree of main plus #120 merged locally: dotnet test 592 passed, validate.py 0 errors, provenance, pytest 72, no conflict markers. Non-blocking note left on `SitePerson` (it can pick a card not yet submitted).
- No Codex PRs were open.

## Changed
- M2R-21 (this PR, stacked on #120, base main): docs/PLAYTEST.md rewritten as the M2R gate script: the four done-when checks of the design doc section 10, approach 1 by hand with the debrief tags today's Sim produces (fuel days 10 to 12 avoidable with the J4 named, barge days 15 to 19 discoverable, power from day 20 avoidable), the J4 one-decision fix, the tutorial check, intended outcomes (fuel ownership added; tutorial lessons listed) and the gate question.
- Backlog: rows M2R-21 (Fable), M2R-22 (Codex: usual owner on the checklist and Ask the J4), M2R-23 (Nick: the gate). Status line updated. Branch merged with main first so line 1 does not conflict.
- Pre-gate review addendum: the J4 is not on screen, so the day 10 lesson cannot be acted on in the game.

## Why
The gate needs a script that matches the current Sim (the M2 script still described fuel as the CCO's buy), and writing it showed the J4 gap.

## Validated how
Docs and backlog only. Debrief tags checked against scenarios/spoke-b/golden/approach1-as-written and approach1-with-j4 expected reports; names checked against scenarios/spoke-b/scenario.json. Conflict-marker grep clean.

## Unresolved
- M2R-22 is new for Codex; if M2R-17 is already in flight Codex may fold it in.

## Next
When M2R-12, M2R-17 and M2R-22 merge: repeat the two-minute walk from fresh screenshots, then tell Nick M2R is ready for the gate.
