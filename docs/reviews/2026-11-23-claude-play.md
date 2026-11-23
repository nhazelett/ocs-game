# Cold tutorial play, 23 Nov 2026

Verification of DESK-235–238 after PRs 534–538. Two fresh Codex players received only the exact PLAYTEST-CLAUDE first-time-player brief and screen driver; the second also received the change-your-mind sentence. Neither read code, design, backlog or the other player's game. The filename retains the established convention after the single-agent handover. The two games stayed frozen while fixes were prepared elsewhere.

- Build: merged main `d11139ca612644710c28a722f7cfe28a33698793`. Godot 4.6.1 .NET, .NET 8, native 1280×800, separate authenticated TCP-loopback Xvfb displays, Dummy audio.
- Seed: 20261123 for both. Both finished day sixteen and met all four goals. First: water out zero days, forklifts one, waste zero, exact output 98.4%. Second: water one, forklifts one, waste zero, exact output 97.1%. There are 114 and 150 logged commands respectively, each including the final quit. Neither reached the ten-step stuck limit.
- Logs: [first commands](plays/2026-11-23/commands-1.txt), [first input](plays/2026-11-23/input-log-1.json), [first notes](plays/2026-11-23/notes-1.md), [first replay](plays/2026-11-23/replay-1.txt); [second commands](plays/2026-11-23/commands-2.txt), [second input](plays/2026-11-23/input-log-2.json), [second notes](plays/2026-11-23/notes-2.md), [second replay](plays/2026-11-23/replay-2.txt).
- Pictures: [None with unheaded COR facts](plays/2026-11-23/day5-cor-none.png), [zero supply called short](plays/2026-11-23/day8-zero-short-summary.png), [Payments before opening Coastal](plays/2026-11-23/day10-payments-before-contract.png), [Payments after returning](plays/2026-11-23/day10-payments-after-return.png). Exactly four native plain PNGs, each below 400 KB, opened during review.

## The first two minutes

Maj Cole opens with the already-open 300-person hub, water and forthcoming forklift needs. Two preparation choices are preselected; both players take all five offered choices. The first player questions the unused five days and the lack of a role or winning bar on that first screen. Turnover then names the contracting-officer role and all four commander goals before the first purchase. The second questions what supplies water while the water point is down; the request names the stock covering day two. These are the actual opening screens, not a timed human test. D28 and D32 already set this sequence and deliberately allow all tutorial preparations to fit; no artificial scarcity or new preparation rule is added.

## What the merged rows did in play

- DESK-235: both saw every writer read “date: pick two of Fast/Cheap/Good”; choosing the pair restored actual dates. The first used the pick-two link successfully. The remaining below-fold observation is preserved, not treated as another missing-date defect.
- DESK-236: both saw dated completed-day supply. The second player's morning four explicitly says “Supply on day 3 · last completed” while Harbor starts on day four. The earlier undated-snapshot contradiction is not repeated.
- DESK-237: both saw “One reported miss is information. Repeated reported misses can bring a decision…” before the eventual dining decision. Both issued cure notices. Neither searched for a promised decision after just one report.
- DESK-238: both joined personally. Neither handed meeting setup to Novak or opened its changed historical handover record; that specific changed path remains unexercised. Passing implementation/native tests are not recast as a cold play.
- DESK-195/197/199 and DESK-217(d,e) remain unexercised. The second opened a contract but did not end it or change its COR. Neither bought on their own initiative or established a BPA.

## Notes and review

The original gameplay observations are preserved in the two notes files. IDs below count their observations in file order. Grouping does not omit an observation; neutral successful actions and correct consequences are Known, while the player's shell-quoting mistake is Model.

| Note | Observation | Sort | Written reason / row |
| --- | --- | --- | --- |
| P1-1 | All offered preparations fit, leaving five days. | Known | D32 explicitly accepts this for the tutorial, which teaches the effects; no artificial budget tradeoff. |
| P1-2 | Preparation did not yet state the role or win conditions; turnover later did. | Known | D28 and the opening sequence put the role and commander's bar in turnover before play. The opening is not treated as a timed human result. |
| P1-3 | One failed click came from the player's shell quoting. | Model | Driver syntax, not a game defect; retain the observation in the notes. |
| P1-4, P2-8 | A recurring meeting hour appeared after the joining job finished. | Known | D59/DESK-229/230 already state the personal recurring cost and expose the standing commitment on Team; the first player's day plan explained it. No duplicate scheduler or changed hours. |
| P1-5 | Asking Dunn did not automatically add operators to the requirement. | Known | The request is not the requirement: the explicit Add what you learned action is the intended decision. The player found and used it. |
| P1-6, P1-8 | None or Lane was selected while an unheaded paragraph described Pratt as if he were assigned. | Design | D66 / DESK-240: identify every existing full paragraph as a possible COR. Keep candidate facts, explicit selection and the None default. |
| P1-7, P2-4 (writer choices) | Priority controls were below the current view; the pick-two link brought them into view. | Known | D62/DESK-235 now names Fast/Cheap/Good and withholds a date until chosen. D36/D45 retain one scrollable form with pinned consequences; the link worked. No default pair, invented date or new screen. |
| P1-9 | Zero forklift supply was called short after turnover distinguished out from short. | Defect | D67 / DESK-241: label the umbrella count not supplied in full; preserve every threshold, count and outcome. |
| P2-1 | Water was down despite nothing being on fire. | Known | D28/D32 and the request distinguish the broken water point from stock covering day two. No invented early shortage or hidden forecast. |
| P2-2 | The player cancelled the first card buy to seek written terms. | Known | A deliberate available choice; the replay checks its actual execution and consequences. |
| P2-3 | The contract still showed its deliveries until the cancellation task ran. | Known | D37 deliberately queues the call; its pending one-hour task is shown and the player identified it. No instant cancellation or capacity release. |
| P2-4 (capacity) | A queued cancellation still committed Island Mart's capacity; a direct replacement would be easier. | Known | D37 explicitly leaves atomic replacement out. The actual refusal is accurate; no capacity reuse before the cancellation runs. |
| P2-5 | Written terms were too late; the cancellation could not be stopped; handover to Avery worked. | Known | D7/D9 retain writing and delivery time. FOR-NICK #23's approved stop action is bounded to unsigned buys, not every task. Explicit handover is reached; no new universal undo. |
| P2-6 | Harbor was called cash-only but a written purchase order was offered. | Defect | D65 / DESK-239: distinguish refusing GPC from refusing a written order in both turnover passages. Existing cash/card/written previews corroborate the difference; no purchasing-rule change. |
| P2-7 | Back to the desk returned to the open request. | Known | The preserved originating desk/request context also appeared in the fourth 20 Nov report. The draft and navigation state are retained; no new back stack or duplicate exit. |
| P1-10, P2-12 | Both stopped at the completed tutorial with all goals met. | Known | Both complete input logs replay to their actual verdicts and exact figures below. |
| P2-9 | Four scrolls to the new contract in Payments; wanted easier access and invoice follow-up. | Known | The request already links to its own contract (D36/DESK-174). Payments retains inherited and ended financial history. No new invoice-administration surface is added; the separate broken return is DESK-242. |
| P2-10 | Contract page offered ending it; the assignee picker appeared before the action name. | Known | The contract already includes its requested parts; applicable change actions only are shown. COR designation remains on Payments under D8. The visible End this contract action and its cost explain the picker; no action was taken and no new administration screen is added. |
| P2-11 | After returning to Payments, Coastal seemed to disappear. | Defect | D68 / DESK-242: opening selects and moves it above the old scroll offset; return must focus that exact row. The arrangement did not vanish. Preserve the player's original observation and correct its diagnosis from source and replay. |
| P2-13 | Water debrief traced the player's cancellation through Harbor's later start and the food outage. | Known | The replay confirms this actual cause chain, including the one uncovered day. D18/DESK-208's dependency explanation is reached. |
| P2-14 | Earlier ask/wait advice felt at odds with the later avoidable forklift gap; trace omitted the advice/refusal. | Design | D69 / DESK-243 makes the Harbor BPA forecast conditional on acceptance. The advice was for Harbor; Coastal's different written quote said day eight or nine and actually began on nine. Keep those facts and the current tag; a perfect earlier counterfactual was not verified. |
| P2 driver observations | Apostrophe escaping and a missing space before #2 each caused a driver-only error. | Model | Retained as syntax observations; not game defects. |

## The players' four end answers

First player: **Who:** the contracting officer at the 300-person hub with Avery and Novak. **Goal:** sixteen days with water/forklifts out at most once, waste never out and output at least 95%. **Managed:** yes, knowingly accepting the quoted one-day forklift gap and issuing a dining cure notice. **One change:** make the COR explanation agree with the selection; None and Lane both appeared beside unheaded Pratt facts. The full answers remain in the first notes file.

Second player: **Who:** the same hub's contracting officer and two-person team. **Goal:** water, forklifts with drivers, waste and at least 95% output. **Managed:** yes; the cancellation caused one water-out day and the written start used the forklift allowance. **One change:** a replacement flow showing old/new supply dates before cancellation. D37 already deliberately leaves atomic replacement out; retain that scope decision rather than add a new route or undo. The full answers and both reversals remain in the second notes file.

## Replay and balance

The Harness replay accepts both complete action logs and confirms every commander criterion. First: Island Mart card water on day one; personal meeting join on day two; Dunn asked on four, operators added and the Coastal purchase order assigned to Avery on five; waste order with Lane assigned to Avery on eight; cure notice on thirteen. Coastal begins on nine, so day eight is the single forklift outage. Second: Island Mart card water, a cancellation handed to Avery, and a Harbor cash replacement on day one; personal meeting join on two; the same day-four/five forklift learning and written order, waste on eight and cure notice through Avery on thirteen. Cancellation runs before the Harbor start on day four, leaving water out on day three and food affected by water. No signed written contract was ended and no COR was re-designated.

The second log has no terminating action against Coastal; it remains the running forklift source through the end. The Payments disappearance is the selected-row/scroll navigation defect described in D68, not a changed financial or Sim result.

The prescribed `batch tutorial --seeds 200 --json` produced [all aggregate fields](plays/2026-11-23/balance.json), exactly equal to the ninth 22 Nov baseline. A separate pass through the same Sim policy API compared seeds pairwise and checked every commander criterion; its [summary](plays/2026-11-23/paired-balance.json) records textbook 200/200 passes, greedy 189/200 and do-nothing 0/200. Greedy ties textbook output on eleven seeds and exceeds it on none; do-nothing exceeds it on none.

| Policy | Output minimum | Median | Maximum | All four goals met |
| --- | ---: | ---: | ---: | ---: |
| Do nothing | 77.8% | 79.6% | 79.6% | 0/200 |
| Greedy | 94.3% | 97.45% | 100% | 189/200 |
| Textbook | 97.3% | 99.7% | 100% | 200/200 |

Resilience and sortie generation stay at 1 for every policy in this tutorial; those fixed measures are unchanged, not evidence that these policies teach or test them. Water, forklift and waste outage measures and overall output do distinguish the policies. No balance change is proposed.

## Review limits and next

New rows DESK-239–243 and ordinary decisions D65–D69 address the actual words seen here. No question needs Nick's scope, account or spending decision; no FOR-NICK addition or Suno use. DESK remains open and unrelated M3 stays paused. Verification of any resulting player-visible fixes belongs to a later cold play on their merged build, not these frozen games or the informed implementation session.

DESK-195/197/199, DESK-217(d,e) and the changed DESK-238 meeting-handover history still lack cold exercise. Windows execution, human timing, mouse feel, motion, sound quality and enjoyment are not claimed. Play scratch, builds and unselected images are temporary; only the four selected images and small text evidence are retained in the repository.
