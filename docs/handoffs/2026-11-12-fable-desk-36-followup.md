**Work session handoff** (Fable, scheduled run, 12 Nov 2026, late afternoon)

Expected look of this build and why: one more debrief label without "line". When a gap's cause is a supplies part left off a buy (the Spoke B generator bought without its fuel), the debrief heading for that cause read "A supply line was left off." and now reads "A supply was left out of the buy." The text under it was already changed in DESK-36. Nothing else on screen moves.

## PRs reviewed this session

- 197 (fable/backlog-desk-merged): backlog only, CI green, ready to merge (carried by 198 and by this one).
- 198 (fable/DESK-36): Sim text only, CI green, 753 tests pass on the branch locally; ready to merge, except that the debrief factor heading above still said "supply line", which DESK-36's acceptance covers. Fixed here, stacked on it.
- 199 (codex/handoff-2026-11-12-1600): a Codex handoff note only, CI green, ready to merge.

## Changed

- `DebriefWords.Factor(GapFactorKind.MissingSupplyLine)` (DebriefViews.cs): "A supply was left out of the buy". The game shows these labels as the heading of each contributing cause (GameShell.Debrief.cs). The enum name stays; no player sees it.
- NoLineWordTests: every debrief factor label is checked for the word. The new test fails against the old label.
- Backlog: DESK-36 row names the follow-up.

## Why

DESK-36's acceptance is that no word a player reads for a part left out of a buy says "line". The report text the DESK-36 tests read does not carry the factor labels, so this one was missed. Found in this run's review by searching the Sim's player strings for "line". The other hits are fine: funding lines (money), "the ACSA line" (a line of support from a partner nation's agreement, not a part of a request), "money on the line", and stop messages that only replay validation and the policies reach.

## Validated how

754 .NET tests (633 Sim, 121 Harness), validate.py and provenance.py pass; no conflict markers, no *.actual.txt. Goldens unchanged: the label is not in any report.

## Unresolved

Nothing for Nick.

## Next

Nick checks in: merging this PR lands 197, 198 and this one. Then he plays the tutorial again. Fable: review only until then.
