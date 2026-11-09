**Work session handoff**

Expected look of this build and why: the top strip can show the small-buy line ("CE small buys: 4 worked today, 2 h; 0 waiting") and the card program state; the morning board gets a daily CE line. Codex binds these in M2R-13 and M2R-17.

## PRs reviewed this session
- PR 96 (M2R-01, scheduled run): cast and messages read as prose; merged.

## Changed
- `Core/Session.SmallBuys.cs`: the stream, hold/release, the card program. Rules and schema blocks `smallBuys`, `gpcProgram`.
- Shop day order: queued work, then the card program from leftover hours, then small buys from what is left.
- Textbook stands up the card program on day 1 when cards exist.
- Goldens: hours-worked lines and hashes only; every SPEC 5 story and tag unchanged (checked against main: 88/83/93/93 on the 90-day approaches).
- ADR-0005 addendum, assumptions (3 rows), backlog.

## Validated how
- SmallBuyTests (6). 367 Sim, 120 Harness. 100 seeds: spoke-b textbook 0.930 (91 pass), greedy 0.711, do-nothing 0.613; spoke-b-90 0.926 (94), 0.670, 0.646.
- First model tried (small buys before queued work) moved approach 3's clock and gave it a gap; rejected for the leftover-hours model, recorded in assumptions.

## Next
- Fable: M2R-04 (going to people, engage J4), then M2R-05, M2R-06, M2R-07.
