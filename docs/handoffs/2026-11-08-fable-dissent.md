**Work session handoff**

Expected look of this build and why: no build. Process documents only.

## Changed
- `docs/DISSENT.md`: the rules (artifact-grounded objections, lanes, one-counter cycle, closed stays closed without new evidence, disagree and commit, escalation format, no theater).
- `docs/objections/`: README with index, TEMPLATE, and OBJ-0001 to OBJ-0003 (Fable's objections to parts of this mechanism, escalated to Nick).
- `docs/REVIEW.md` and `docs/reviews/`: milestone review format with the objection health check; SPEC 11 milestone rule points to it.
- `.github/PULL_REQUEST_TEMPLATE.md`: new Objections section (default "None").
- `backlog/QUESTIONS.md` and `backlog/BACKLOG.md`: `[CONTESTED OBJ-NNNN]` and `[RESOLVED OBJ-NNNN -> ADR-xxxx]` tag conventions; M1-03 for Codex.
- `docs/decisions/ADR-0000-template.md`: status may read `Contested (OBJ-NNNN)`.
- `docs/CODEX-KICKOFF.md`: working rules point to DISSENT.md.

## Why
Nick asked for a dissent mechanism that resolves on its own most of the time (chat, 8 Nov 2026). Objections to it were raised before building, as he asked, and are filed rather than built in.

## Validated how
Read-through against Nick's six requirements; every one maps to a file above. CI: scenarios, test and provenance jobs unaffected.

## Unresolved
- OBJ-0001 (silence vs. self-merge), OBJ-0002 (small-number health check), OBJ-0003 (priority lane vs SPEC 12) await Nick.
- Nick's draft of the Codex loop paragraph did not come through; the version below is Fable's. Reconcile when Nick pastes his.

## Codex loop paragraph (add to the looping prompt)

> Disagreement follows docs/DISSENT.md. At the start of each session, open docs/objections/ and write a Response in any record waiting on you before starting new queue items. If an item you are about to build conflicts with a SPEC section, an ADR, a test or build result, or cannot be built as specified, do not build a different thing and do not stay silent: file docs/objections/OBJ-NNNN-<name>.md from the template, naming the artifact it rests on, link it in the PR's Objections section, and either build what was asked or set that item aside and take the next one. You can block only in your lane (cannot be built as specified, fails tests); in Fable's lane (design intent, scope, priority) you object and Fable decides. One response, one counter, then the lane owner decides. If the decision goes against you, implement it fully and write "implemented per ADR-xxxx over objection OBJ-NNNN" in the handoff. Never escalate a technical dispute to Nick; settle it with a test. Do not file objections on preference or style and do not invent concerns to fill the section; "None" is the normal answer. Add one stop condition: an objection in Fable's lane is open on the item you are about to start.

## Next
- Nick: decide OBJ-0001 to 0003 in chat; paste the Codex paragraph draft if it differs.
- Codex: M1-03 (respond to the three records), M1-01 (harness on the real Sim).
- Fable: M0-07, M1-02.
