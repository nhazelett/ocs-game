# Dissent: how Fable and Codex disagree, decide, and move on

Both agents can disagree, collaborate and re-attack when it makes sense. The mechanism exists to catch real problems early and to settle them without Nick most of the time. It is not a ritual. Set by Nick, 8 Nov 2026; amended the same day by his decisions on OBJ-0001 to OBJ-0003.

## 1. What counts as an objection

An objection must rest on an artifact:

- a SPEC section,
- a test or build result,
- a cost or time estimate,
- a playtest observation, or
- a conflict with an existing ADR.

Preference or style is not an objection and is not filed. "I would have done it differently" is not filed. "This fails `RulesTests.X`" is.

## 2. Lanes

| Agent | Owns | Can block on |
| --- | --- | --- |
| Fable | Design intent, scope, and priority (the order of work inside a milestone) | "This is not what the game is" |
| Codex | Feasibility, build and test truth | "This cannot be built as specified" or "this fails tests" |

Each agent can block inside its own lane. Neither agent blocks in the other's lane. An agent may still object in the other's lane; it just cannot block, and the lane owner decides.

Nick owns what is in or out of a milestone and any trade between scope and schedule (SPEC 12; OBJ-0003).

Priority decisions are not a way to silence the other agent. When Fable decides a priority question over a Codex objection, the record is marked `Revisit at milestone review`, and the next review takes it up as a team with Nick, without needing new evidence. Work continues on Fable's decision until then (OBJ-0003).

## 3. Cycle

1. **Objection**, filed as a record in `docs/objections/` (format below).
2. **Response** from the other agent, in the same record.
3. **One counter**, if the objector has one. That is the only re-attack.
4. **Decision** by the lane owner, written in the record with who decided.

If the disagreement crosses lanes, or the lane owner cannot decide, it escalates to Nick.

## 4. Closed decisions stay closed

A decided objection, an accepted ADR or a merged PR is closed. A merged PR becomes closed only once the other agent's next session has passed; each handoff note lists the PRs that agent reviewed that session (OBJ-0001). It reopens only on new evidence: a test result, a build result or a playtest finding. A better argument is not new evidence. Reopening cites the evidence in a new objection record that links the old one. The one exception: records marked `Revisit at milestone review` are reopened at that review without new evidence.

## 5. Disagree and commit

The agent whose objection is rejected implements the decision fully, not a partial version. Its handoff note says `implemented per ADR-xxxx over objection OBJ-yyyy` (or `per OBJ-yyyy decision` when no ADR was written), so playtesting can revisit it. Playtest data is the final tiebreaker for design disputes.

## 6. Escalating to Nick

Escalations go to Nick in chat, in plain terms, and only for design or priority questions. Format:

- **The question**, one sentence.
- **The options**, lettered.
- **What each costs.**
- **Which options are reversible.**
- **Each agent's recommendation** (or "not yet asked").

Never escalate a technical dispute to Nick. It stays in the repo until a test proves it or one side concedes. Nick's answer is recorded in the objection record by whichever agent brought it.

## 7. No theater

No quotas, no devil's advocate roles, no mandatory concerns per PR. Silence on a PR means agreement once the other agent's next session has passed without an objection (OBJ-0001). Until then, an objection to a merged PR is a first objection, not a reopening.

## 8. Where things live

- Objection records: `docs/objections/OBJ-NNNN-short-name.md`, from `docs/objections/TEMPLATE.md`. Numbered in order, never reused.
- An objection to an accepted ADR sets that ADR's status to `Contested (OBJ-NNNN)` until decided; the decision either leaves it Accepted or produces a superseding ADR.
- Backlog items and questions under objection are tagged in `backlog/QUESTIONS.md` and `backlog/BACKLOG.md` (conventions in those files).
- A PR that objects to something it was asked to build links its objection record (PR template). It never silently builds a different thing.
- The milestone review counts objections (`docs/REVIEW.md`).
