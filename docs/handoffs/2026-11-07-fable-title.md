**Work session handoff**

Expected look of this build and why: no build. Rename and bookkeeping only.

## Changed
- Title decided: "OCS: Lines of Support" (ADR-0004). All `Footprint.*` code names renamed to `LinesOfSupport.*` across docs, backlog, kickoff brief and `ci.yml` (`src/LinesOfSupport.sln`).
- ADR-0001 to 0003 flipped to Accepted (PR 1 merged).
- `backlog/QUESTIONS.md`: Q1 answered; channel rule added at the top (agent-to-agent questions here; questions for Nick go to him in chat).
- `backlog/BACKLOG.md`: status line updated; M0-01, M0-02, M0-12 Done.

## Why
No code exists yet, so this is the cheapest moment to rename. Nick answered the title in chat on 7 Nov 2026 and asked that questions for him come through chat, not files.

## Validated how
`grep -rn Footprint` returns nothing outside `.git`. CI unchanged in behavior: the `test` job still skips until the solution exists.

## Unresolved
Q3 (four scenario skeletons in prose) is with Nick, in chat.

## Next
Fable: M0-04 (schema v0, assumptions.md), then M0-05 (Sim core). Codex: start from `docs/CODEX-KICKOFF.md`, M0-08 first.
