**Work session handoff**: Fable, scheduled run, 9 Nov 2026 (third item in the run, after M2R-04 and M2R-07). M2R-08, the tutorial scenario.

Expected look of this build and why: a new scenario, `scenarios/tutorial`, "Week one at the hub". Nothing on screen changes until Codex turns on the tutorial button in the front door (M2R-10) and the mentor messages (M2R-17). What the lead says each morning on the taught path:

    day 1: MSgt Carla Ortiz, 3 FSS, is here about water for Hub, wanted from day 2.
    day 4: SMSgt Ray Dunn, LRS, is here about material handling equipment for Hub, wanted from day 8.
    day 6: Material handling equipment for Hub is wanted from day 8 and nothing covers it yet.   (Harbor turned the BPA call down)
    day 9: Capt Ana Ruiz, 3 CES, is here about waste removal for Hub, wanted from day 13.
    other days: quiet

## The scenario
Skeleton 2 as written, with the numbers made concrete: 300 people at the hub, host nation power, fuel, water point, honey trucks and forklift; billeting and dining on calls against the camp BPA. Day 1: the water point is down until day 9 in a heat advisory, so bottled water for 7 days (card or cash; Island Mart takes only the card, Harbor Bottling only cash; $15,000 in the cash box). Day 3 the host nation says its forklift goes back after day 7; day 4 LRS asks for three forklifts from day 8; Harbor Equipment Co declines every call days 4 to 8 (new `vendor.declinesCalls` effect), so the player learns to have a second source (Coastal Rentals). Day 8 the host nation says its honey trucks go back after day 12; day 9 CE asks for waste removal from day 13, which wants a written purchase order and a COR (Main Island Sanitation also takes cash: the trap). The outgoing CCO, Capt Jordan Mills, is the mentor: the turnover's last line points at the water request, and three notes on days 1 to 3 cover the cash box, BPAs and services.

## PRs reviewed this session
See docs/handoffs/2026-11-09-fable-m2r-04-people.md; plus 112 (M2R-13, ready once CI is green).

## Validated how
`dotnet test src/LinesOfSupport.sln`: 429 Sim (7 new TutorialTests; the tutorial added to the golden and plain-names runs) and 120 Harness pass; no existing golden changed; `validate.py`, `provenance.py` pass. Seed 1: taught path 100% mission and every pass criterion met; do-nothing 79%, greedy 99%, textbook 96%.

## Unresolved
- The trap (waste on SF 44s) only bites through the execution-confusion roll; at seed 1 it happens to go clean. If Nick wants the trap to cost something every time, it needs a surer consequence (for example, no COR reports and a higher confusion rate for a recurring service on SF 44). Fable's call to tune after Nick plays it.
- Textbook scores below greedy here (it uses UCAs for small buys, and a UCA in a 20-day game runs into its definitization clock). Not an invariant for the tutorial; the 200-seed ordering is for the representative scenarios.
- Portrait keys mills, ortiz, dunn, ward and reyna are new; the game's keyed neutral portrait covers them.
- Hidden vendor capacity from the skeleton is not modelled.

## Next
M2R-06 (Spoke B fuel through the J4) is claimed by another session; after that, playtest polish from Nick's next check-in.
