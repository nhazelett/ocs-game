**Work session handoff**

Expected look of this build and why: almost no visible change in the game. On spoke-b-90 the morning board gains comptroller notices (warning 10 days before om1's year end, the late-release notice between days 62 and 70, the expiry, "no funding line has money usable", and om2's release); the ledger already shows om2's later release day. Board items for funding are clickable but go nowhere until Codex M3-13. The debrief gains "No funding line with money could pay for ..." factors on spoke-b-90 gaps that fall in days 76 to the release.

## PRs reviewed this session
- PRs 67, 68 and 69 (Codex M2-21, M2-22, M2-23): unchanged since the last run's "ready to merge" comments (heads 44625e9, 98b5fab, 169c8e8; CI green; mergeable clean). No new comment needed. Merge order 67, 68, 69.
- PR 70 (Fable M3-06 IDIQ): green, clean; merge before this PR (stacked).
- Scheduled run: no merges attempted (FOR-NICK #1).

## Changed
- fable/M3-03 (PR 71), stacked on fable/M3-06 (PR 70). Next Fable item (M3-04) should branch from fable/M3-03. New `Session.Funding.cs`: funding clocks and records, the `funding.late` effect, line stops, the debrief's `FundingLapse` factor.
- Data: `fundingLines[].capabilities` (purpose); `replacementArrivesDay` removed from schema and spoke-b-90 (never read). spoke-b-90: om2 planned for day 76 (was a fixed 78) and event `fyFundingLate` (window 62 to 70, delay 3 to 10 days).
- Views: `FundingOption.PlannedAvailableDay`, `Capabilities`, `PaysForThis`; `BoardLinkKind.Funding`; `RecordKind.Funding`; `GapFactorKind.FundingLapse`. Textbook and greedy pick only lines whose purpose fits.
- ADR-0005 and ADR-0006 addenda; assumptions.md rows (Color of money, Fiscal year end, Debrief tags: Funding lapse); SPEC 530 one clause; new Codex row M3-13.

## Why
SPEC 4 lists "a funding line expires at fiscal year end and its replacement is late" as a disruption, and SPEC 3 asks for hard stops on "funds expired or wrong type". Purpose by capability is the wrong-type stop without appropriation law, which assumptions.md keeps out of 1.0.

## Validated how
- FundingTests (9): seeded late release over 5 seeds (release day 79 to 86, notice day 62 to 70, caused by the event); expiry warning and expiry record; board items linked to the ledger with Info for releases; editor stops for expired and unreleased lines and the late release in `FundingOption`; an award started day 75 rejected at completion ("expired with day 75") and named in an Avoidable FundingLapse factor with the late notice; an Unforeseeable lapse before any money is released; purpose stops for awards and IDIQ guarantees, `PaysForThis`, and textbook never trying a wrong line; the 12-month cap; load-time reference checks.
- Mutation: with the delay or the purpose check disabled, five FundingTests fail.
- Goldens: spoke-b-90 all regenerated and diffed by hand. Only new FUNDING and EVENT records and FundingLapse factors; every scorecard, strip and pass line unchanged. spoke-b textbook gains one record (no money usable from day 25, it spent om1); nothing else.
- 200 seeds: spoke-b-90 textbook median 0.926 (min 0.888), greedy 0.671, do-nothing 0.646; spoke-b textbook 0.9305, greedy 0.714, do-nothing 0.607. Ordering holds.
- 211 Sim and 97 Harness tests; validate.py, provenance.py, conflicts.py.

## Unresolved
- Bona fide need is not modeled: a service may be paid from a line whose year ends before it starts. I tried the stop (31 U.S.C. 1502(a)); it is correct law but it sent the textbook into the trap at seed 1 (its follow-on starting day 77 had no legal money, and an overlap from om1 hit the incumbent's capacity). The honest fix is subject-to-availability awards (FAR 32.703-2): award early, start when the money comes. Logged in assumptions.md for M3-07.
- Several purposed lines live only in tests; M3-07's scenario should carry them (for example a life-support line and a general line).
- validate.py does not yet check the new references (Codex M3-13).

## Next
- Fable: M3-04 (COR and report fidelity), M3-06 ACSA and HNS, then M3-07.
- Codex: M3-12 and M3-13 after PRs 70 and this one merge; M3-08, M3-10, M3-11.
