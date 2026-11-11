**Work session handoff** (Codex, 11 Nov 2026, DESK-24)

Expected look of this build and why: Vendors replaces the visible Directory and Vehicles tabs. Choose one vendor's card to read at a time; facts the office has not learned show a dash. A meeting fills the same card in. BPA and IDIQ setup starts with that vendor and keeps the engine's costs and stops in view. Inspected native plain PNGs, all under 400 KB: [unknown facts](images/2026-11-11-codex-vendors-blank.png), [after the meeting](images/2026-11-11-codex-vendors-known.png), [IDIQ preview and submission](images/2026-11-11-codex-vendors-idiq-preview.png).

## PRs reviewed this session

171, 173, 174 began green, mergeable and without inline threads. PR 171's earlier corrections were already accepted in Fable's latest review. Fixed PR 173's two Game follow-ups in 838cd7e (different default portraits; one separator in task text). Fixed PR 174's presentation follow-ups in a17937c and 8b71f6b (heading inside the compact dialog; wrapped-text sizing without anchor warnings). CI passed on the final heads: runs 43002408830 and 43004158621. The pace rounding belongs to Fable's DESK-14.

Read session/kickoff, required SPEC sections, ADR-0001 to 0003, DISSENT, the four decided objections, backlog/questions/FOR-NICK and available Codex/Fable handoffs. No question or objection needed a Codex response. The delegated five-open limit governs this session.

## Changed

Claimed codex/DESK-24 before implementation. It carries corrected PR 174, which carries PR 171 and Fable's reviewed 162–168 stack under Q18. Its base is current main 2138840 through those dependencies. Independent team PR 173 and Fable 169–172 are not carried. Inherited Sim/scenario/rules/design contents are unchanged.

Vendor cards bind SessionView.VendorCards exactly: known and unknown terms, supply, reach, the rep, KnownFrom/StillToLearn, office arrangements, known BPAs/IDIQs and unfinished setup. A card offers the existing meet action with the engine's preview and pending/completed guards. The hidden internal vehicle page preserves navigation indices, but only Vendors is visible to players.

The scrollable setup form stays with the selected supplier. The existing BPA control path and engine limit validation remain. IDIQ setup uses EstablishIdiqAction and PreviewEstablish for capability, offered scope, ceiling, optional guarantee/ordering days and funding. Cancel, obsolete controls, duplicate submission, finished games, Menu and playback are guarded; independent Sim comparisons cover the real action and subsequent days. No rules are implemented in Game.

The award editor blanks cash/card and capacity where PaymentKnown/CapacityKnown are false, and fills those fields after learning. Visits no longer displays directory confidence. Updated Game documentation, native CI, affected screen checks and only DESK-24's backlog Status cell.

## Why

D6 replaces an overwhelming directory and separate vehicle list with one understandable record per vendor. Facts stay visible as blanks until learned, and repeat-buy setup belongs to the vendor the player is dealing with.

## Validated how

CLI clone main was attempted and failed for missing HTTPS credentials. The connected GitHub API read live main and all relevant PR heads. The isolated baselines exactly matched the complete published trees of PRs 173 and 174 before edits; the corrected dependency tree is 28f1dec8bd8a53b779bf103b9f3253e3618350f9. No successful CLI pull is claimed.

Solution and Game builds pass. All 684 .NET tests (563 Sim, 121 Harness), 72 Python unittests, scenario/provenance validators, harness validation, conflict and whitespace checks pass. The complete native Godot 4.6.1 CI block passes all 45 expected markers (44 pass lines plus the save-restart write marker), exit 0 and no ERROR/FAIL lines. After the final presentation/knowledge changes, Vendors, GoingToPeople, Action, AwardChoices, Market, Look and PlainNames also pass with exit 0; PlainNames inspected 2,754 panels and 198,673 strings. The final graphical Vendors suite passes and the three committed screenshots were inspected. Dummy audio; no fresh Windows export or human listening verdict.

The new native suite checks exact unknown fields on cards and in the award summary; learning and duplicate meeting guards; one card per engine vendor and held vehicles; exact typed IDIQ terms, cents and preview; read-only edits; canceled/obsolete/duplicate controls; save/resume; and setup completion against direct Sim play. Existing BPA threshold and completion checks remain. Chrome and Ledger checks now follow the engine's exclusion of partner/host-nation support from buyable vendor cards.

## Unresolved

Existing nonfatal ObjectDB and compiler/analyzer warnings remain. Fable/Claude review: card selection/scrolling and the three screenshots, knowledge masking, and the newly reachable IDIQ setup and its funding/guarantee wording. No new asset, spend or FOR-NICK addition.

Automatic approval review initially denied a PR 173 source blob as lacking trusted publication authorization. User-message reads verified Nick's exact standing job and his later permission that Codex can push what is needed; the denied operation was retried once and succeeded. An optional follow-up comment on PR 173 was separately denied as lacking explicit permission for public communication. No comment was posted or retried; the response is in the committed handoff. Authorized code publication continued. The comment denial does not block the implementation or review evidence.

## Next

Fable reviews and merges on its own schedule. Next Codex row: DESK-26, on the reviewed pick-two contract under Q18. This session stops after the fix pass and DESK-24 because its implementation/verification budget is spent. No PR merge, main push, force-push or shared-branch deletion.
