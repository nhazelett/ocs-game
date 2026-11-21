**Work session handoff** (Codex, 21 Nov 2026)

Expected look of this build and why: the contract page shows a modification refusal in full inside its single scroll, together with the ending controls. Signed contracts retain the added-part sentence and omit the irrelevant modification button. Card water buys omit it too.

## PRs reviewed this session

PRs 405 and 406: CI was green; accepted Fable's two layout/visibility findings on 405 and status/readback request on 406. Main merged into 405 without rewriting history. The explicit five-open-PR ceiling applies.

## Changed

PR 405 binds ModifyView.Offered after retaining Added. Every offered modification, including a disabled one, keeps ending controls in the body scroll; its wrapped row updates its minimum size. Regression checks cover early and in-work refusals, a card forklift buy missing learned drivers, and no modification button after signing. Refusal rectangles are fully enclosed by the single scroll at 1280 by 800; ending controls share that scroll. Six refreshed native PNGs are plain files below 140 KB in images/2026-11-21-codex-desk98-*.png.

## Why

Fable's PR 405 review: D13, D15 and D37. The disabled branch incorrectly sent ending controls to a separate footer and ignored Sim's Offered field.

## Validated how

Game build, headless Modification and native Godot 4.6.1 OpenGL Modification pass (9.9 seconds). All refusal and after-signing captures were inspected. Solution build passes with one MSBuild worker; default parallel build returned an empty failure in this environment. All 1,007 solution tests pass (886 Sim, 121 harness), together with Contract, Performance and CommitPreview headless scenes; scenario/provenance/conflict validators and whitespace checks pass. Existing nullable/analyzer and graphics warnings remain; Windows, audio and frame-rate performance were not tested.

## Unresolved

PR 406 needs this fix merged into its branch, StatusToday bindings and fresh card captures. Hosted CI and Fable review remain. No new Nick-only issue; no FOR-NICK addition. No PR merge, main push, force-push or shared-branch deletion.

## Next

Complete PR 406's fix, then the ordered unblocked Codex backlog within the five-open-PR ceiling.
