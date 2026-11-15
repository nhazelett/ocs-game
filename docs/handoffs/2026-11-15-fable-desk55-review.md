**Work session handoff**

Fable, scheduled run, 15 Nov 2026 around 00:45 UTC. No Fable DESK row was open (DESK-11 and DESK-12 wait for the gate), so this run reviewed and merged Codex's open PR.

Expected look of this build and why: after an action such as sending someone to finance, the one-line message ("The task is on your team's work list.") shows under the desk pane, below its frame. The map, Fit map and Show today's movement stay exactly where they were, so nothing on the left moves when a message comes and goes (D13).

## PRs reviewed this session

- 259 (Codex DESK-55, the status line does not take height from the map): CI green, merged. Read as a first-time player against D13 and the row's acceptance, from the tutorial day 4 before and after pictures. The map, its card and its two buttons are pixel for pixel in the same place in both; the message reads whole at the foot of the desk, under the frame, clear of the scroll and of the map. The cost moves to the desk: its frame is about 36 px shorter while the message shows, so the bottom of the Today list moves up a line. That is the "foot of the desk pane" the row allowed, the desk is the pane already being read when the message appears, and its contents do not jump, so it is left alone. If Nick notices the desk edge moving in his replay, the next step is to reserve that line all the time.

Combined tree tested before merging: 687 Sim and 121 Harness tests pass; scenarios and provenance pass; no conflict markers. The PR does not touch the Sim or a scenario, so no balance check.

## Changed

- backlog/BACKLOG.md: DESK-55 Done, status line.

## Next

Codex: ART-20. Then Nick plays again (docs/PLAYTEST.md). DESK-11, DESK-12 and other Sim depth wait for that.
