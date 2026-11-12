**Work session handoff** (Fable, scheduled run, 12 Nov 2026)

Expected look of this build and why: no visible change. `SessionView.WhereThingsStand()` is not on screen until Codex builds DESK-35; this fixes three of its readings before that screen is built on them.

## PRs reviewed this session

- 185 (backlog and playtest page for the gate): ready to merge; carried by 187, so merging 187 lands it.
- 186 (Codex handoff, gate check): ready to merge. Docs only.
- 187 (DESK gate findings, D13 to D16, rows DESK-31 to DESK-35): ready to merge. Nick's words and his agreement are recorded; the left-out list is there.
- 188 (DESK-31, where things stand): right, with three readings fixed here (below). Ready to merge with this follow-up on top.
- 189 (DESK-32, the quote says what is already in place): ready to merge. The capacity refusal now says what is left and what to do; the tutorial textbook golden moved in its two rejected-buy lines and its hash, as stated.

## Changed

Found by printing every morning's list for the tutorial and Spoke B approach logs and the three policies:

- A late contract on its first day read as failing. Spoke B approach 4: Meridian's power starts on day 17 after ten dark days, and day 17 said "Meridian Site Services is meant to deliver 470 kW, but power at Spoke B was at 0% yesterday." Yesterday's level now counts only for support that was already running yesterday, so day 17 reads "Meridian Site Services is delivering 470 kW."
- A request missed altogether stayed at the top of the list for the rest of the game. Tutorial, do-nothing: on day 20 the first line was still "Nothing is arranged, and it was wanted from day 2" for water wanted days 2 to 8. Once its days are over it is now ended: "Its days are over: it was wanted from day 2 to day 8, and nothing was arranged.", sorted with the done ones.
- Support arranged to start after the day wanted, with nothing running, sorted below "nothing yet" and "buying" while the site was dark (Spoke B approach 4, days 7 to 16). It now sorts with running short.

Not changed, noted for later: waste removal reads "3600 gal" without "/day" in previews and in this list, while the day records say "gal/day". The `Rate` helper adds "/day" only to consumables; that predates DESK-31 and is used by every preview.

## Why

D16: make the present readable. A list that blames a new contract for yesterday, or nags about a request that is over, is the confusion Nick named at the gate.

## Validated how

Two new StandingTests (first day of a late contract; a missed request over and last). All 712 .NET tests pass, validate.py and provenance.py pass, no conflict markers. Goldens unchanged: the list is read only.

## Unresolved

Nothing for Nick.

## Next

Codex: DESK-33, DESK-34, then DESK-35 on top of this. Fable: review only until Nick plays again.
