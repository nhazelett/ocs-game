**Work session handoff**

Fable, scheduled run, 15 Nov 2026 around 16:45 UTC. No Fable DESK row was open (DESK-11 and DESK-12 wait for the gate), so this run reviewed Codex's open PR.

Expected look of this build and why: unchanged. PR 261 was not merged, so the game keeps the old flat vector faces.

## PRs reviewed this session

- 261 (Codex ART-20, a new cast in 3D): CI green, not merged; sent back with a PR comment. Reviewed against D24 and the art review bar from the twelve before and after pictures and from the 27 cast PNGs laid out side by side at full size and at 48 px.
  What passes: one matte style and one light, the Tabletop palette, nothing photoreal or uncanny, nothing AI-looking, provenance rows, the packed resources load, the picker bounded.
  What fails: (1) the faces do not match the names. Most of the cast are women by name (in the tutorial Ortiz, Ruiz, Cole, Nair, Haddad, Tan, Ward and Joan Avery) and nearly all now wear one short cap of hair on the same broad collared bust, so they read as men; only Brooks and Carvalho have longer hair. The Today picture shows it on the first screen a player reads: MSgt Carla Ortiz's card goes from a woman with a bun to a man in a cap. (2) The hair is a smooth shell with a hard rim above the brow and reads as a knit cap. (3) In one scenario, people can be mistaken for each other at 48 px: tutorial Ruiz, Nair and Tan; Spoke B Lin and Park; and in the assignment list You and the junior teammate are the same figure.
  Smaller: the head is small in its frame (about 12 px tall in the 36 px strip), and nearly every bust wears the same collared shirt where D24 asks for uniform or civilian dress.
  Asked for with the next pass: the cast sheet at 48 px grouped by scenario, and the same before and after set.

No Sim or scenario change, so no balance check.

## Changed

- backlog/BACKLOG.md: status line.

## Next

Codex: ART-20's fix pass on PR 261. Then Nick plays again (docs/PLAYTEST.md). DESK-11, DESK-12 and other Sim depth wait for that.
