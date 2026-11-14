**Work session handoff**

Fable, scheduled run, 14 Nov 2026 around 16:50 UTC. No Fable DESK row was open (DESK-11 and DESK-12 wait for the gate), so this run reviewed and merged Codex's two open PRs and fixed one thing its review found in the Sim's prose.

Expected look of this build and why: a request page now has "Who can help" entries (face, name, what they can tell or do, informs or opens, where it stands, one start button). With this fix the state line under each entry reads "Not asked yet, about 1 hour." or "SrA Eli Novak is on it; the answer should be in on day 4." instead of repeating the name above it. The results page after a quiet day is its headline alone, and the decisions list frames each decision with its status.

## PRs reviewed this session

- 249 (Codex DESK-48, who can help on the request): CI green, merged. Meets the row: each helper says what talking does, informs or opens, the hours and where it stands; asking starts from the line through `HelperView.Start`; the J4 form opens on the line's capability. As a first-time player, two reading problems in its pictures: every state line repeats the name printed above it ("SMSgt Ray Dunn: not asked yet"), which is Fable's Sim prose and is fixed here; and the answered entry still ends "This takes about 1 shop hour." under the answer, which is DESK-53 for Codex.
- 250 (Codex DESK-52, results and decisions at a glance): CI green, merged. The quiet day is one headline, a bad day shows only its two groups, the overflowing page shows its scroll bar, and each decision sits in one frame with its "In work" line.

Combined tree (main plus 249 plus 250) tested before merging: 687 Sim and 121 Harness tests pass; scenarios and provenance pass.

## Changed

- `Session.WhoCanHelp.cs`: `HelperView.Sentence` no longer starts with the helper's name; the J4's answered line names the partner instead of "The J4:". WhoCanHelpTests checks that no state line repeats its helper's name. Goldens unchanged (the sentence is not in any report).
- backlog/BACKLOG.md: DESK-48 and DESK-52 Done, DESK-53 added for Codex, status line.

## Next

Codex: DESK-44, DESK-53, ART-20. Then Nick plays again (docs/PLAYTEST.md).
