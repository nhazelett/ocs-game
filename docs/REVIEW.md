# Milestone review

Run at the close of every milestone (M0, M1, ...) and monthly if a milestone runs longer than a month. Fable writes it as `docs/reviews/YYYY-MM-DD-<milestone>.md`; Codex adds its build and test lines. Short: a page.

## 1. Against the plan
- Milestone acceptance criteria from SPEC 11, each met or not, with the evidence (test, report, screenshot).
- Harness balance run and golden tests: pass or fail.
- What slipped and why, in one line each.

## 2. Objection health check
Count every objection record in `docs/objections/` filed or decided in the period:

| Filed | Accepted | Rejected | Escalated to Nick | Withdrawn | Still open |
| --- | --- | --- | --- | --- | --- |

- Always report the counts. If every decided objection in the period went one way (all accepted or all rejected), say so. Call the mechanism decorative only when 4 or more objections were filed in the period, or when the same one-way pattern appears in two reviews in a row (OBJ-0002). Then name which side is rubber-stamping or filing noise, and what changes.
- Take up every record marked `Revisit at milestone review` as a team with Nick, and record the outcome in it.
- List any decision implemented "over objection" and whether playtesting has revisited it.
- List any objection open longer than one milestone and why.

## 3. For Nick
Anything that needs his call, in the escalation format from `docs/DISSENT.md` section 6. Nothing else.
