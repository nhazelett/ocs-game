**Work session handoff** (Codex, 11 Nov 2026, vendor review captions)

Expected look of this build and why: the supplier card reads "Supply per day: 5,000 gal" and "Price per gal: $0.25", using the engine's unit. Equipment keeps its per-day price basis. [Native card](images/2026-11-11-codex-pr175-review-known.png), inspected 1280 by 800 plain PNG under 400 KB.

## PRs reviewed this session

171, 173, 174, 175 and 176. All five are mergeable; 171, 173, 174 and 175 have successful CI. Earlier request wording, default-face/punctuation and dialog-heading/height findings are already fixed in the current heads. PR 176's Session shutdown failure is being corrected on its own branch. No inline thread, question to Codex or undecided objection remains. Read session/kickoff, required SPEC sections, ADR-0001 through 0003, DISSENT, backlog/questions/FOR-NICK and recent Codex/Fable handoffs. The delegated five-open limit overrides the repository's ten-open wording.

## Changed

Accepted PR 175's quantity and unit-caption follow-up on its existing branch. Group capacity thousands with invariant formatting. Bind the quote caption to VendorCardOffer.Unit and the capability's consumable/continuous delivery metadata; consumables have a per-unit quote, while equipment retains its daily basis. No quantity, price, knowledge or simulation rule changed. The native regression still verifies nullable facts, actual meeting fill-in, typed IDIQ setup, stale controls, save/resume and independent Sim hashes; it also checks the two displayed price bases and grouped capacity.

## Why

Fable's 13:00 UTC review asks for a thousands separator and the offered unit instead of "unit". The suggestion to expose more vendor names is explicitly for consideration at the DESK gate; it remains a gate question, without changing the one-card screen now. The IDIQ density note requests no immediate change.

## Validated how

The local baseline tree 6cdbf67f4791cefc45da36090ec05d6d8c932c62 matches published PR 175. Live main is 2138840; CLI pull could not authenticate, so repository state and publication use the connected GitHub API. Solution and Game builds pass, all 684 .NET tests (563 Sim, 121 Harness), scenario/provenance/conflict validators and whitespace checks pass. Native headless and graphical Vendors suites exit 0, with the new screenshot inspected. Dummy audio; no Windows export or listening verdict claimed. Temporary .NET SDK/runtime and an authenticated loopback Xvfb provided local verification; no project dependency changed.

The first caption-fix CI run (305, 43036245525) passed Vendors and the other checks, then crashed after CONFIDENCE_SMOKE_PASS with two unsafe StyleBoxFlat wrappers and the same native !rc_owner fatal condition seen on PR 176. This is evidence for carrying the shared cleanup onto this branch too: the title, opening and lead panels release their managed style wrappers after native ownership is retained; SessionSmokeTest explicitly disposes its PackedScene and frees both shells before collection/quit. LookSmokeTest forces collection and verifies all three panel colors in each theme. The panel styles and visible layout do not change. PR 176 carries the same correction, so either dependency order retains it.

## Unresolved

Existing nonfatal ObjectDB/compiler/analyzer warnings. No new asset, spend, QUESTIONS or FOR-NICK entry. Fable review: offered units and per-unit versus daily price labels. The final standalone shared-cleanup source passes native Session, Look and Confidence checks before publication. PR 176 follows this commit through a merge parent so it cannot restore old captions or cleanup.

## Next

Publish to existing PR 175; finish PR 176's shutdown fix and session handoff. Stop new work with five open Codex PRs; DESK-27 is next when capacity permits. No PR merged, main push, force-push or shared branch deletion.
