**Work session handoff** (Codex, 10 Nov 2026, 08:00 UTC fix pass)

Expected look of this build and why: the award form keeps its Tabletop look, with a visible moss scrollbar. A selected BPA has no unrelated cash reason beneath it. The empty supplier list counts the unavailable choices; pointing at one still gives the exact Sim reason. The complete award capture shows the vehicle and funding fields together.

## PRs reviewed this session

All five open Codex PRs (142, 143, 146, 148 and 149) were mergeable against live main a78f8b6 and had successful CI on their exact published heads. No unresolved inline review threads. Fable marks all five ready; its newest PR 146 follow-up asks for the two M2R-41 fixes. No new PR or backlog claim: the user's five-open cap governs this run.

## Changed

PR 146, same branch: fix both findings in Fable's 07:01 review (M2R-41). A valid selection has no unrelated reason; an empty list shows the sole disabled reason or one sentence counting multiple disabled choices. Focused disabled entries retain exact Sim reasons, and closing the list restores the selection's explanation. A cleared selection keeps its notice. No irrelevant vehicle reason is shown when no vehicle is required.

The form follows keyboard focus and gives its scrollbar a real themed width, with space reserved beside text. The split stays at its existing position. Native regression covers the actual scrollbar geometry, keyboard access to funding, choice/reason transitions, both supplier/instrument orders, invalid selection clearing, disabled signals and unchanged world hashes.

Five new native plain PNGs under 400 KB were generated locally at docs/handoffs/images/2026-11-10-codex-award-review-*.png: host-nation, bpa-first (supplier list open), bpa-selected (selected instrument and summary), full-award (vehicle and funding visible), and no-cash. Each was opened and inspected. These show the revised panel; the Sim wording is from this branch. PRs 147/150 supply the host-nation and sentence-case text updates separately.

## Why

The fifth pre-gate walk found an unrelated reason under a valid BPA choice and a form with hidden scrolling. Fable explicitly allowed fixing them on PR 146. This is the fix pass, with no sixth Codex PR.

## Validated how

Solution build passes with one MSBuild worker and build servers disabled; Game build passes without new warnings. All 619 .NET tests pass (498 Sim, 121 Harness). Scenario, provenance, conflict-marker and whitespace checks pass. Eight native suites pass with exit 0: AwardChoices, Action, Market, Funding, Cor, Bpa, PlainNames and TabletopTheme. The final graphical AwardChoices run also exits 0 and checks every captured form keeps text clear of the scrollbar. The unchanged Python validators pass all 72 tests in the companion sound fix checkout. No Windows export rerun for these UI-only changes.

Git CLI fetch could not authenticate; live GitHub metadata confirmed main's exact SHA is already present locally. The isolated checkout starts from the exact remote PR 146 head 729e5ac. Publication uses the GitHub connector, with a normal fast-forward and a checked expected head.

## Unresolved

The source fix is published in PR 146 at 8787776. Automatic approval review denied github_create_blob for the new host-nation screenshot, saying the standing coding instruction did not explicitly authorize sharing that nonpublic artifact to nhazelett/ocs-game. It did not execute. A fresh user-message read found no new approval, so the upload was not retried; the other four new captures were not uploaded either. All five remain local pending artifact-specific approval. Fable cannot complete the required screenshot review from the remote PR yet. The current report is FOR-NICK #11 in the companion sound fix checkout.

M2R-41's backlog row currently lives in Fable's unmerged PR 150, so this branch does not introduce a competing copy. Fable should mark that row addressed by this PR when reconciling the queue. The earlier M2R-36 status update, original handoff and previously blocked screenshot remain separate pending artifacts; they are not republished by this fix pass. No new FOR-NICK entry on this branch. The audio provenance follow-up and current session's final handoff are on PR 149.

## Next

Obtain approval to publish the five new native captures to this PR, then Fable can re-review them and land the queue on its schedule. Keep new work stopped at five open Codex PRs. No PR was merged; no main push, force push or shared branch deletion.
