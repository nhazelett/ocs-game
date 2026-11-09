**Work session handoff**: Fable, scheduled run, 9 Nov 2026 01:45 UTC. M2R-01 people in scenario data.

Expected look of this build and why: no change on screen yet. The game still shows the requirement editor's `SubmittedBy`, which now reads "Capt Ana Ruiz, 3 CES" instead of "Spoke B advance team" for Spoke B's cards. People appear on screen when Codex builds M2R-12 and M2R-14 on these views.

## PRs reviewed this session
- #92 (M3-07 Sustainment) and #93 (M3-22): found conflicted against the M2 gate redesign at the start of the run; while this run was resolving it, Nick's chat check-in merged #93 into #92 and #92 into main. This run's resolution and a FOR-NICK entry about holding them were dropped unpushed. No other PRs were open.

## Changed
- Schema: top-level `people[]`; cards take `person` and `message` (`submittedBy` only when there is no person); `actors.sco/fm/commander.person`; events take `from`.
- Sim: `PersonData`, bundle checks (unknown references, wrong actor role, vendorRep without vendor, duplicate ids, a card with neither person nor submitter); `PersonView`; `BoardItem.Person`; `RequirementEditorView.Title/Person/Message`; `SessionView.People/Person/CardPerson/CardTitle/CommitmentPerson/RecordPerson/ScoContact/FinanceContact/Commander/OutgoingCco`. ADR-0006 addendum.
- Casts: spoke-b (12 people) and spoke-b-90 (9). Names are invented. Unit names follow the design doc (3 CES, LRS fuels flight) pending Nick's open question 5 in docs/design/opening-and-guidance.md 11.
- Backlog: M2R-01 implemented; new Fable row M2R-06 (plain names in Sim text) and Codex row M2R-18 (validate.py mirror).

## Why
docs/design/opening-and-guidance.md R2 and R6: work arrives from people; no identifiers on screen.

## Validated how
`dotnet test src/LinesOfSupport.sln` 361 Sim and 120 Harness pass (18 new PeopleTests); every golden byte-identical (people are not in the state hash); `validate.py`, `provenance.py`, `conflicts.py`, tools pytest pass; harness `validate` passes for spoke-b, spoke-b-90, sustainment.

## Unresolved
- Event and record text still carries speaker prefixes and ids ("Spoke B first sergeant: ...", "Ratification of uc1 ... from om1"). Left alone so goldens stay identical; M2R-06 does the text pass.
- Sustainment has no cast yet; it still uses `submittedBy`, which stays valid.
- FOR-NICK has two rows numbered 4 (Codex's M3-12 upload question and the playtest verdict). Left as is; the playtest row is the one the backlog cites.

## Next
M2R-02 (turnover and briefing data), stacked on this branch.
