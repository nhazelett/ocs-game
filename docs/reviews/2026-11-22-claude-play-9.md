# Cold tutorial play, 22 Nov 2026 (ninth)

Verification of DESK-229–233 after PRs 527–531, with the graphical fixture repaired in PR 532. Two fresh Codex agents received only the prescribed first-time-player brief and driver; the second also received the change-your-mind sentence. Neither read repository files or design context. The filename retains the PLAYTEST-CLAUDE convention after the single-agent handover; this is a new play, not a replay of the eighth.

- Build: main at `88d2d5d15c8301737688edc3e5ac3636574bcc85`. Godot 4.6.1 .NET, .NET 8, native 1280×800, separate authenticated TCP-loopback Xvfb displays, Dummy audio.
- Seed: 20261122 for both. Each game remained on this exact build while implementation continued separately.
- Results: both finish day sixteen with all four lines met, without getting stuck. First: no days out, exact mission output 99.4%; second: water out one day, forklifts/waste out zero, exact output 98.4%. There are 125 and 134 logged commands respectively, each including one runner quit after the final verdict was preserved. Each player supplied twelve numbered observations and all four end answers.
- Logs: [first commands](plays/2026-11-22-9/commands-1.txt), [first input](plays/2026-11-22-9/input-log-1.json), [first notes](plays/2026-11-22-9/notes-1.md); [second commands](plays/2026-11-22-9/commands-2.txt), [second input](plays/2026-11-22-9/input-log-2.json), [second notes](plays/2026-11-22-9/notes-2.md). Original observations are preserved; classification below belongs to the review.
- Pictures: [the day-one writer placeholder](plays/2026-11-22-9/day1-writer-date.png), [morning four with an undated supply snapshot](plays/2026-11-22-9/day4-undated-supply.png), [the first dining report without a decision](plays/2026-11-22-9/day12-report-without-decision.png). Three native plain PNGs under 400 KB each, all opened during review.

## The first two minutes

Preparation opens with Maj Cole describing an already-open 300-person hub, water and forthcoming forklifts. Two preparations are preselected; all five fit in nine of fourteen days, so both players take them all. Mills's turnover then explains the contracting shop and the four goals before the first water decision. Both question the apparent preparation tradeoff and what supplies water before the purchase. D32 already permits unused tutorial prep days; the water request names stock covering day two. No artificial scarcity or new preparation rule is added.

## What the merged rows did in play

- DESK-229/230: the first player joined personally for two hours, then found the one-hour standing commitment and next meeting on Team on day three. The second handed the joining work to Novak; it became his three-hour job, and following mornings reserve his 1.5 hours. The first player still initially treated joining as a one-off; the Team line resolved it. Stop/rejoin and Avery's meeting quote were not exercised.
- DESK-231: the first player opened Island Mart's contract after the half-delivery. It says “cost incurred $1,390.50” and explicitly says day three was disputed, half arrived, and the card already paid for the whole day. The changed caption and explanation are reached; the player does not assert a numerical refund or disputed-quantity entitlement.
- DESK-232: “No crossings today” is visible on the local-delivery mornings. Neither player now reads it as a denial that Island Mart is supplying water. No inter-island trip was tested by this tutorial.
- DESK-233: the first notice says the dining hall is currently short-staffed, without claiming tomorrow's miss. Both players still search for a decision after the first report; that is the separate D64 finding below.
- DESK-217(a): “You have this task” is reached on the first player's cure task and the second player's cancellation task. Correct words are visible. DESK-195/197/199 and DESK-217(d,e) remain unexercised; visible entry points alone do not count as those actions being played.

## Notes and review

Every numbered observation is retained in the raw notes. Known includes expected consequences and neutral completion observations; it does not turn them into new defects.

| Note | Observation | Sort | Decision / row |
| --- | --- | --- | --- |
| P1-1, P2-1 | All five preparations fit inside fourteen days. | Known | D32: useful tutorial preparation, no artificial scarcity or new point budget. |
| P1-2, P2-2 | The water point is down despite “nothing on fire”; what supplies people now? | Known | The request names stock covering day two; D4/D28/D32 already distinguish inherited support and buffers. |
| P1-3 | The card risk names a half-delivery but no odds. | Known | D9/D54–56 preserve the written-versus-card trade and explain the resulting risk; no hidden vendor/seed odds added. |
| P1-4, P2-5 | “done day after you pick two” sounds like tomorrow or does not name which choices. | Design | D62, DESK-235: name Fast/Cheap/Good while withholding the date until Sim can quote it. |
| P1-5 | End the day first opens a forecast; idle people appear under Still open although walk-ins use the hours. | Known | D1/D19/DESK-149(c): the plan is advisory, names the fallback work and permits ending the day; no mandatory make-work or new penalty. |
| P1-6 | The meeting action is below the news and must be scrolled to. | Known | Existing Today discovery and single-scroll layout, D13/D36; no duplicate meeting action introduced. |
| P1-7 | Joining looked one-off until Team showed the recurring hour. | Known | D59/DESK-229/230's personal cost and standing commitment are reached and resolve it; no new scheduler or atomic handover. |
| P1-8, P2-8 | Short or absent water also cuts food service. | Known | DESK-208 and D18: the dependency and replay are correct; no separate unexplained food failure or altered scoring. |
| P1-9 | Wanted to settle the card dispute; left the buy running and supply recovered next day. | Known | D37/D60: cancellation is the available action; no retroactive terms or automatic refund. The contract names the disputed day and charge. |
| P1-10 | Priorities and COR still require scrolling behind the pinned writer/action area. | Known | D36/D45/D46 and DESK-96/148/156/187 deliberately retain one scrolling form and pinned consequences; keep the existing half-pane and one-scroll acceptance rather than add another screen or smaller type. |
| P1-11, P2-10 | A COR report exists, but the promised vendor decision is absent; Inbox/history do not answer what to do. | Design | D64, DESK-237: distinguish one report from repeated reported misses. The actual issue correctly opens after days eleven and twelve. |
| P1-12 | Finished with all four lines held and no outage days. | Known | Harness confirms all four met and exact output 99.4%; the player's 99% quotation remains unchanged. |
| P2-3 | Successfully cancelled the first water buy before delivery, intending to replace it. | Known | Existing card cancellation; verify its execution and charge release in the replay, not a new defect. |
| P2-4 | Capacity remains committed until the queued cancellation happens. | Known | D37: no instant capacity release or atomic replacement; the player advances the day and then buys. |
| P2-6 | A written replacement is too late, so the player returns to the card. | Known | D7/D9/D33: the authoritative quote exposes the timing trade; no refusal bypass or changed lead time. |
| P2-7 | Hands the meeting setup to Novak, deliberately pressing Give them this task. | Known | Existing handover, personal pace and explicit commit are reached. Selecting a face alone correctly applies no action. |
| P2-9 | Morning four shows zero supply beside water starting on day four. | Design | D63, DESK-236: date the completed-day supply snapshot; keep current morning and planned start distinct, without inventing today's deliveries. |

| P2-11 | The cure period runs past the tutorial; what protects the remaining days? | Known | D31/DESK-91/165 deliberately leave a running cure period in the debrief. A notice gives the vendor time; it is not instant repaired performance or a guaranteed outcome. |
| P2-12 | Finished with all four lines held, one water-out day and rounded output 98%. | Known | Harness confirms exact 98.4%, one water day out and no forklift/waste day out. |

Runner finding R1 (Defect, DESK-238), from the second Harness replay: the handover record reads “SrA Eli Novak took over getting the shop into the commander's staff meeting: about 2 shop hours, then 1 at each meeting, every day. From you: 3 hours left.” The runtime, Team and future plan correctly use his three joining hours and 1.5 recurring hours, but that history description retains the base two/one. This is a record-wording follow-up under D59, not a finding the cold player made or a changed-hours simulation bug. Fix the description, retain actual work and remaining hours, and check direct joins and handovers. No owner decision is needed.

## The four end answers

First brief: a new contracting officer at a 300-person operating hub with Avery and Novak; keep water, forklifts, waste and mission output inside the commander's limits for sixteen days; yes, all four held with no days out. Its requested change is a compact purchasing view with arrangement, supplier, COR, priorities, writer and consequence together, because finding scattered controls was unnecessary work. The complete answer is in notes-1; the report does not replace its words.

Second brief: the contracting officer responsible for the same hub and 300 people; hold the four lines for sixteen days while changing two decisions; yes, the cancellation/replacement and meeting handover both completed and all four goals held. Its requested change is a dated next-action status atop Today, including whether it is waiting for another report or starts today. D63/D64 address the demonstrated ambiguity; no generic new issue-management screen is added.

## Replay and Sim pass

Both saved logs replay through the freshly built Harness to the displayed pass lines. [First replay](plays/2026-11-22-9/replay-1.txt): mission.output 0.994, water/forklift/waste days out 0/0/0, hash `3cba0a8c3d580dbd360e096cb319c24d50846536081c7625755e04d7b8a31e14`. [Second replay](plays/2026-11-22-9/replay-2.txt): 0.984, days out 1/0/0, hash `11d4564fcae7bbcff420e67725a5079d45d2f86bee3decf78773d0dab8538647`. The players' rounded 99% and 98% remain in their original notes.

The first water buy is disputed on day three, reducing both water and dependent food to half. The second player cancels before delivery and receives the whole first charge back, then buys again on day two: day three has no water and food, day four a disputed half-delivery. The missing-day count is one, not two. The meeting handover completes with Novak and subsequent attendance is his 1.5 hours. Both dining issues correctly open after missed days eleven and twelve; both players issue a cure notice on day thirteen. R1 is the stale history description noted above, not an execution mismatch.

`batch tutorial --seeds 200 --policies do-nothing,greedy,textbook --json`, seeds 0–199, [complete aggregates](plays/2026-11-22-9/balance.json):

| Policy | Mission output min / median / max | Water days out | Forklift days out | Waste days out |
| --- | --- | --- | --- | --- |
| do-nothing | 77.8% / 79.6% / 79.6% | 6 / 6 / 7 | 9 / 9 / 9 | 4 / 4 / 4 |
| greedy | 94.3% / 97.45% / 100% | 0 / 0 / 1 | 0 / 1 / 2 | 0 / 0 / 1 |
| textbook | 97.3% / 99.7% / 100% | 0 / 0 / 1 | 0 / 0 / 0 | 0 / 0 / 0 |

Textbook retains the best floor and median; neither other policy has a better pass-line aggregate. Power/fuel/lodging days out remain zero, and sortie generation and resilience stay one across all policies and seeds: these are the same inherited tutorial fixtures, not evidence that a new action improves them. Water, forklifts, waste and mission output respond. No balance or golden change is warranted by this run.

## Validation and next

Solution build and all 1,148 tests pass (1,027 Sim, 121 Harness), as do scenario/provenance/conflict checks and the two Python driver tests. The initial scratch-only test run lacked historical play-log fixtures; those repository JSON fixtures were added to the test copy before the complete passing run. The default parallel MSBuild invocation exited without diagnostics; the passing build uses a single MSBuild node and disables build servers. No production test was weakened.

A separate report review accounts for all 24 numbered notes exactly once, checks both replays and the original end answers, opens all three report images plus the meeting, contract and final-verdict captures, and confirms that no production source, scenario or golden changes in this report. The stale DESK-234 status is reconciled with merged PR 532, and the objection index now matches OBJ-0004's already-decided record.

D62–D64 are ordinary design decisions with written reasons; new rows DESK-235–238 are the queue. DESK-235 is prepared separately with native BuyForm/RequestBuyFit/AwardContext evidence. Continue with DESK-236 after it; retain the remaining unexercised paths listed above, and verify future merged UI with the prescribed cold briefs. DESK remains open and M3 outside it paused. No new FOR-NICK addition or Suno spending. Windows execution, human timing, motion, audio and enjoyment are outside this play's evidence.
