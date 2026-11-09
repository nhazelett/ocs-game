**Work session handoff**

Expected look of this build and why: the game looks as it does on main (or with PR 130) except for wording. Wherever the ACSA partner appears mid-sentence it now reads "the partner force" (the approach 1 debrief: "The J4 was never asked, and the partner force had 4,000 gal a day to spare under the ACSA"), and "The partner force" where it starts a sentence. The debrief's repeated evidence under the paragraph stays until Codex builds M2R-30 on the new `Summary.Quoted`.

## PRs reviewed this session
- PR 130 (Codex M2R-28, the debrief as an after-action): Fable review: ready to merge. In lane, all checks green, two plain PNGs. As a first-time player the finished game now opens on Maj Erin Cole and the lesson is on screen without scrolling. Found two things for follow-up rows (below), neither blocking.
- PR 129 (Fable backlog status): already marked ready to merge last run; unchanged since. This PR is stacked on it.
- Merge order at the check-in: 129, then this PR (131 or the next number; it carries 129), then 130 with its M2R-28 status cell resolved to Done.

## Changed
- `DebriefSummary.Quoted` (ADR-0006 M2R-29 addendum): which of the lead gap's factors the paragraph already says word for word, so the game can show them once.
- The ACSA partner name in Spoke B, Spoke B 90 and the sustainment scenario is "the partner force"; Sim text capitalises it at a sentence start. Schema description says how to write it.
- Tests: `Quoted_names_exactly_the_factors_the_paragraph_says`, `The_partner_force_reads_as_a_noun_mid_sentence`, on every golden and policy of all four scenarios.
- Goldens: text and state hash only; scorecards unchanged.
- Backlog: M2R-29 (Fable, this) and M2R-30 (Codex, evidence says each thing once); M2R-26 marked blocked on FOR-NICK #5; status line and M2R-23 updated.
- `backlog/FOR-NICK.md` #5: Codex's M2R-26 is built but its upload was refused by Codex's approval check; the gate cannot start without it.

## Why
Third-walk principle (M2R-19): say each thing once. PR 130's screenshot shows the paragraph's two evidence sentences again, word for word, a few lines below. And "and Partner force had" read as a misprint.

## Validated how
`dotnet test src/LinesOfSupport.sln`: 490 Sim and 121 Harness tests pass. `tools/validate/validate.py`: 5 folders, 0 errors. `tools/validate/provenance.py` passes. Conflict-marker check clean.

## Unresolved
FOR-NICK #5 (Codex's M2R-26 upload). The inherited hub fuel vendor is still named "Partner force (ACSA)" as a vendor; left as a proper vendor name.

## Next
At the check-in: merge 129, this PR, 130; answer FOR-NICK #5 so M2R-26 lands; Codex builds M2R-30. Then Fable's fourth walk on main from fresh screenshots and the ready call for the gate.
