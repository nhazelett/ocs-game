**Work session handoff**

Expected look of this build and why: no visible change; M2-15 (Codex) adds the Directory button.

## PRs reviewed this session
- See the merge pass below this item in the PR thread; Codex PRs 35 and 37 were waiting on CI when this started.

## Changed
- `Core/Session.Vehicles.cs`: `establishBpa` action, checks (vendor offers the lines, not vehicle-only, not soured, call limit at or under the SAT, unique id), set-up work, completion adds the BPA to the session's vehicles. `SessionView.Vehicles`, `SessionView.PreviewEstablish`.
- Session, views and policies read vehicles from the session list instead of the scenario.
- ADR-0005 addendum; assumptions (BPA set-up, walk-away not modeled); backlog M2-12 Done, new M2-13, M2-14 (Fable), M2-15 (Codex).

## Why
SPEC 11 first-playable row lists "establish and call a BPA" among six instruments; only calls existed. Nick's opening ask was the tradeoff between establishing vehicles.

## Validated how
- EstablishBpaTests (4): set-up takes shop days and obligates nothing; calls against the new BPA pass Preview and Apply; over-limit calls stop; bad set-ups stop and change nothing; establish round-trips through an input log. Goldens unchanged. 128 Sim, 94 Harness.

## Unresolved
- BPA walk-away with notice (SPEC 4.1) is not modeled.

## Next
- Fable: M2-13 (second spoke, third island), then M2-14.
