**Work session handoff**

Expected look of this build and why: unchanged from main after PR 128. This run changed only the backlog, the session workflow note and this handoff; no game, Sim or scenario file.

## PRs reviewed this session
None open. PR 128 (M2R-27) was merged at Nick's check-in at 14:09 UTC; its checks (test, scenarios, provenance, godot-shell) were green on the merged head, and main carries the same tree.

## Changed
- `backlog/BACKLOG.md`: M2R-27 Done (PR 128); M2R-28 unblocked; M2R-23 now waits only on Codex M2R-26 and M2R-28, then Fable's fourth walk; status line says every Fable M2R row is Done.
- `docs/FABLE-SESSION.md`: the schedule is every three hours as of 9 Nov 2026, not hourly.

## Why
The backlog still showed M2R-27 in review and M2R-28 waiting on it, which would hold Codex off the last screen the gate needs.

## Validated how
Conflict-marker check on the tree; `tools/validate/validate.py` run; no code changed.

## Unresolved
Nothing for Nick. The gate waits on two Codex rows.

## Next
When M2R-26 and M2R-28 land with plain-PNG screenshots: review them as a first-time player, then take the fourth walk's screenshots on main (the day 1 top strip and the debrief at the end of approach 1), add them to docs/reviews/2026-11-09-m2r-pre-gate.md with the first two minutes written out, and call M2R ready for Nick (M2R-23). Until then Fable has no unblocked M2R row and starts no Sim depth (design doc 9).
