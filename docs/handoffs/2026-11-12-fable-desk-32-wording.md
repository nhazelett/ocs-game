**Work session handoff** (Fable, scheduled run, 12 Nov 2026)

Expected look of this build and why: the red sentence under a greyed Arrange support reads better when a vendor has too little. Codex's DESK-33 screenshot showed "Harbor Equipment Co can supply 5 forklifts and 0 is already committed on those days, so 5 is left", which states a zero and buries the number that matters. It now reads "Harbor Equipment Co has 5 forklifts in all. Ask for 5 or less, or buy the rest from someone else." No layout change.

## PRs reviewed this session

- 185, 186, 187, 188, 189, 190: already reviewed by the 00:52 run, no new commits since; verdicts stand (ready to merge; merging 190 lands 185, 187, 188 and 189).
- 191 (Codex DESK-33, one scroll per pane and the reason under the button): ready to merge. Reviewed from its three screenshots against D13 and D15: the award form and quote scroll as one piece, Arrange support sits at the bottom with its reason directly under it in red ("Choose an available supplier." when greyed; the vendor's capacity stop when refused), and the reason goes away when the terms are ready. Tab arrows remain until DESK-34, as the PR says.

## Changed

The vendor-capacity stop (DESK-32, D15) leads with what is left:

- Nothing committed: "Harbor Bottling has 5000 gal/day in all. Ask for 5000 or less, or buy the rest from someone else."
- Some committed: "Harbor Bottling has 500 gal/day left on those days, after 4500 already committed. Ask for 500 or less, or buy the rest from someone else."
- All committed: "Harbor Bottling has nothing left on those days, with all 5000 gal/day already committed. Buy from someone else, or pick days when they are free."

A count of one reads singular ("1 forklift"), through a small `Amount` helper beside `Rate`.

Golden move, stated: the tutorial textbook report, the same two rejected-buy lines DESK-32 moved (the forklift GPC bridge and the waste SF 44 bridge), and its state hash, because the stop text is part of the recorded rejection. Text only; no outcome moved.

## Why

D15: a refusal says what to do next, not only the fact. "0 is already committed" is a fact the player does not need.

## Validated how

New AlreadyInPlaceTests case for the nothing-committed reading; the two existing cases and IdiqTests updated to the new text. All 713 .NET tests pass, validate.py and provenance.py pass, no conflict markers, no test-failure output committed. No game code or test asserts the old text.

## Noticed, not changed

The quote's soft gap for a missing part still says "No operators line: daily failure odds rise while it runs." Since D3 there are no lines to tick, so "operators line" is a word from the old screen. Something like "Operators are not part of this request: daily failure odds rise while it runs." would read plainer. Left for Nick's UI and UX pass rather than added as scope now.

## Unresolved

Nothing for Nick.

## Next

Codex: DESK-34, then DESK-35. Fable: review only until Nick plays again.
