**Work session handoff**

Expected look of this build and why: the completed deployment opens across the work area, with Maj Erin Cole's portrait, rank and name above the Sim's large opening paragraph. The fuel gap's evidence is already open beneath it. The map and daily gauges return through “Back to the map.” Scores, capability bands and the day-by-day record sit behind “The full record.” A no-gap completion opens with the same speaker saying that no site ran short, without an empty gap control. This is M2R-28's after-action screen; it does not declare the M2R gate ready.

Screenshots, plain PNG and excluded from LFS: [approach 1 by hand](images/2026-11-09-codex-m2r-28-approach1.png), 168,233 bytes, and [no-gap completion](images/2026-11-09-codex-m2r-28-no-gap.png), 29,359 bytes. Both are 1280 by 800 and were visually inspected after fixing the speaker's horizontal sizing. The no-gap screenshot is a disclosed one-day test cut of the real Spoke B scenario, before the remote sites open, not a claim that the thirty-day approach had no gaps.

## PRs reviewed this session

Fix pass: no open Codex PRs, confirmed through search and the full open-pull list. The only open PR at the start was Fable's #129 backlog bookkeeping; it was left alone. Main includes #128 (M2R-27), which supplies this item's summary contract. Read CODEX-SESSION, CODEX-KICKOFF, the required SPEC sections, ADR-0001 to ADR-0003 and ADR-0006's debrief addendum, DISSENT, opening-and-guidance, the latest Fable/Codex handoffs, QUESTIONS and the decided objections. No question or objection required a new Codex response. The explicit session cap is five open PRs.

## Changed

The debrief binds Summary.Text and Summary.From directly, follows Summary.LeadGap, displays the lead evidence immediately, and expands across the hidden map and daily strip. Other gaps remain disclosures with named site/capability windows rather than gap indexes. Factors and tag meanings use DebriefWords. Raw cause chains remain available through “Trace the evidence,” with known-record filtering. The full record preserves scores, the capability grid, commitment decisions and timeline; timeline sentences appear verbatim once, with plain kind tooltips, in record order grouped by day. “Known then” appears once for each day with decisions. The day picker also includes inherited arrangements on day zero.

Resetting the desk now invalidates the debrief cache, so Continue on the same finished deployment reopens its after-action. The action start-day control has an internal accessor for the by-hand regression. Existing factor-label regressions bind to DebriefWords; COR and naming regressions disclose the raw traces before scanning them. Only M2R-28's backlog Status cell changes; the Fable-owned headline and all Sim/scenario files are preserved.

## Why

The third pre-gate walk found the lesson buried below percentages beside a full-size map. M2R-27 now supplies the person's words and lead-gap choice. This item presents that contract, without ranking gaps or constructing a second explanation in Game.

## Validated how

The local baseline's complete Git tree matches main 2ca5b78ad823afa5e3deaa59f14db414e2dea9e1, tree a454dfc970368bd37e857b6b4d63a98211988e94. Git HTTPS has no CLI credentials, so authenticated connector reads restored verified blobs into an isolated local snapshot; publication uses the connector and the actual remote main parent. No other checkout was reset.

Solution build and all 609 .NET tests pass (488 Sim, 121 Harness), using -m:1 for this environment's worker-pipe restriction. Game build passes with zero warnings. Scenario validation: five folders, zero errors. Provenance, conflict-marker and diff checks pass.

Godot 4.6.1: Debrief passes headlessly and rendered. The normal New game/turnover/award/ratification path uses no replay or developer controls and matches an independent Sim state hash every morning; Spoke C support is also arranged through its real UI. The test checks exact summary/person/lead, readable initial viewport, full-width layout, full-record disclosure, event sentences, known snapshots once per day, day-zero/day filtering, cause links, commitments, saved completion, map return and scenario reset, a true no-gap completion and a cast-free fallback. Rendering and all disclosures leave the Sim hash unchanged. Existing Shell, FrontDoor, Session, PlainNames, Funding, Confidence, Disruption, Support and Cor tests pass. PlainNames checks 2,979 panel visits and 145,989 strings. COR still excludes hidden raw records from both timeline and traces. Both screenshots are plain PNGs below 400 KB.

## Unresolved

The pre-existing M2R-26 implementation and blocker-report uploads remain denied under the previous sessions' local FOR-NICK #5. The current owner-history read showed no answer or affirmative approval reaction; none of those denied operations was retried or included in this item. No new FOR-NICK entry or technical objection was needed while building M2R-28. M2R-26 is still needed before the gate; no Sim depth or paused backlog work was started.

## Next

Opened ordinary [PR 130](https://github.com/nhazelett/ocs-game/pull/130) against main with the implementation, two plain screenshots and this handoff. Its published tree exactly matches the locally validated tree. GitHub reports it mergeable with no conflicts or review comments; CI is running (provenance already passed at the first check). Session outcome: one PR opened, zero PRs fixed, no new FOR-NICK entry. Fable reviews it and repeats its pre-gate walk after M2R-26 and M2R-28 land. Nick's M2R-23 cold playtest remains the gate. No other unimplemented Codex row is currently unblocked. No PR was merged, no main push or force-push was made, and no shared branch was deleted.
