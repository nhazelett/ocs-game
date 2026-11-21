**Work session handoff** (Codex, 21 Nov 2026)

Expected look of this build and why: three 28 px writer rows sit beside Who writes it, above the full summary. The buy column is 770 px wide at 1280 by 800; the request keeps more than one third of the page. [Avery selected](images/2026-11-21-codex-desk148-fix-avery.png) and [long forklift summary](images/2026-11-21-codex-desk148-fix-long.png) are inspected native plain PNGs under 170 KB. No font size changed.

## PRs reviewed this session

PRs 442–445 have the same red operations job and Fable review: the writer footer takes more than half the pane. No inline threads or additional reviews. Main pulled at 44b9932; merge into DESK-148 preserved main's history and this item's status. The active five-open-PR limit applies.

## Changed

DESK-148 fix on its existing branch: compact rows use a 22 px face and 28 px minimum height, zero vertical padding in every button state, and their caption beside the rows. Footer gaps are 2 px. Request/buy proportions are 37.5/62.5 so the long Sim summary fits four lines. The regression now requires writer rows at most 28 px; the existing half-pane requirement is unchanged. Layout diagnostics print actual sizes.

## Why

The former 42 px rows, caption above them and long wrapped summary left too little quote to read. Merely setting a smaller minimum did not remove inherited state padding or summary wrapping.

## Validated how

Solution build and all 1,044 tests pass (923 Sim, 121 harness, none skipped). Scenario and provenance validators pass. Game build, native BuyForm, AwardContext (including exact writer execution/save/replay/stale controls) and RequestBuyFit pass. Written-water and long forklift cases retain 249 of 498 px for scrolling, with every word and action inside the pane; water/initiative retain 387 px. Writer selection, known-part addition, monitor and pick two remain covered. Windows, audio and performance are untested.

## Unresolved

Hosted CI and Fable review remain. No new question or FOR-NICK entry. The original checkout had missing Git objects; the intact newer checkout fetched successfully.

## Next

Merge this correction forward through PRs 443, 444 and 445 without rewriting history; verify their affected screens, then take DESK-167 if the queue remains below five. Codex never merges PRs, pushes main, force-pushes or deletes shared branches.
