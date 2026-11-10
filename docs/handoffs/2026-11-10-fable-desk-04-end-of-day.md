**Work session handoff** (Fable, scheduled run, 10 Nov 2026, DESK-04, stacked on DESK-02 and DESK-03)

Expected look of this build and why: no screen change. DESK-20's End the day button (PR 158) can now show what is still open before the day ends, from `SessionView.EndOfDayCheck()`.

## PRs reviewed this session

See docs/handoffs/2026-11-10-fable-desk-02-team.md (same run).

## Changed

`SessionView.EndOfDayCheck()` returns sentences, empty when nothing is open:
- a request wanted within three days (or already late) with nothing arranged, in work, or already supplying it on that day: "Water for Hub is wanted from day 2 and nothing is arranged or in work yet." / "Water for Spoke C was wanted from day 5 and nothing is arranged or in work."
- then each teammate (not you) with free hours today: "MSgt Joan Avery has nothing to work on today; their 8 hours go to walk-in small buys." / "SrA Eli Novak has 4 free hours today."

Read only: no state change, no random draw (tested against the state hash).

## Why

D1: one button ends the day, and before it does it says what is still open.

## Validated how

530 Sim tests (4 new in EndOfDayTests) and 121 Harness tests pass; goldens unchanged; validate.py passes.

## Unresolved

The three-day window is a constant in code (`EndOfDayDueWithinDays`), not a rules number.

## Next

DESK-05 (vendor knowledge and more vendors to meet).
