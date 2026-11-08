# Model assumptions and deliberate simplifications

Every place the game departs from how the real mechanism works, with the real rule beside it. The player-facing "model assumptions" panel is generated from the scenario file; this document is the full list for the team. When a simplification is removed, delete its row here and bump the rules patch version.

Legend: **Real** is the mechanism as it exists; **Modeled** is what the sim does; **Why** is the reason for the gap; **Revisit** is the milestone, if any, where it should be reconsidered.

## Thresholds and money

| Topic | Real | Modeled | Why | Revisit |
| --- | --- | --- | --- | --- |
| Thresholds | FAR 2.101 sets several threshold sets; the contingency, outside-the-U.S. values apply when the acquisition supports a contingency operation and is awarded and performed outside the U.S. | One threshold set per rules file: micro-purchase $40,000, SAT $2,000,000 (effective 1 Oct 2025). Home-station values appear only if a scenario sets them. | Every 1.0 scenario is a deployed, outside-the-U.S. contingency. | When a home-station scenario is written |
| Money | Dollars and cents; obligations, commitments, expenditures are distinct accounting events. | Integer cents. Award obligates; invoices expend; commitments are not modeled. | Determinism and clarity. | None planned |
| Pricing | Quotes, negotiation, price analysis, cost analysis above thresholds. | Vendor quote drawn once per game from a seeded band around a true price; market research narrows the band; the player picks a quote. No negotiation, no typing of prices. | Nick: no dollar entry; pricing arithmetic has a better home on KTHQ. | None planned |
| Color of money | O&M, procurement, MILCON and others, each with rules on use and period. | O&M only. Lines have an amount, an available day and an expiry day. | Scope control for 1.0. Labeled as a large simplification in the spec. | Post-1.0 |
| Funds certification | FM certifies availability before obligation. | A certification lag in days (scenario parameter); the action waits. | Enough to teach "funding in hand before award." | None planned |
| Fiscal year end | Appropriations expire; replacement funds arrive on their own schedule. | A funding line's `expiresDay` and optional `replacementArrivesDay`. | The trap is the gap, not the appropriations law. | None planned |

## Instruments

| Topic | Real | Modeled | Why | Revisit |
| --- | --- | --- | --- | --- |
| SF 44 | Purchase order-invoice-voucher for on-the-spot, over-the-counter purchases, with conditions on use and dollar limits by situation. | Same-day cash buy capped at the micro-purchase threshold; draws cash on hand with a replenishment lag; needs a paying agent. Recurring services on repeated SF 44s roll an execution-confusion curve and are tagged in the debrief. | Captures the consequence Nick cares about (no terms, cash runs out) without the form. | None planned |
| GPC | Cardholder and billing-official structure, single-purchase and monthly limits, merchant acceptance, convenience checks, expanded-use procedures. | Available or not (by prep choice or a 10 to 20 day work item); per-vendor acceptance and surcharge; single and monthly limits. No convenience checks; expanded use is not modeled. | Scope. Expanded-use GPC is its own kit on KTHQ. | M3 if a scenario needs it |
| Purchase order | SF 1449 or equivalent; synopsis and posting rules with contingency exceptions; evaluation; award. | 1 to 2 shop-days with a system, times a manual factor without; written terms; invoice path with a manual error curve. No synopsis wait (contingency flexibilities assumed to apply). No evaluation play. | Evaluation and source selection are a different game. | None planned |
| BPA | An agreement, not a contract; calls are the obligations; vendors may decline calls; multiple BPAs are common. | Vehicle with scope tags, call limit and ordering period; calls can be declined (base probability scaled by market tempo and vendor reliability); the vendor can withdraw with notice. No minimum guarantee. | Teaches non-binding by consequence. | None planned |
| IDIQ | Minimum guarantee, maximum ceiling, ordering period, fair opportunity among multiple awardees, scope. | 8 to 15 shop-days to establish; obligates a minimum guarantee fraction at award; binding; scope tags, ceiling, ordering period; fair opportunity is a 2 to 4 day step when multiple awardees exist. Vendor capacity is still finite. | The capacity event is the lesson. | None planned |
| UCA | DFARS 217.74: HCA approval; NTE; obligations capped at 50% of NTE before definitization, 75% after a qualifying proposal; definitize by the earlier of 180 days after issuance or 50% obligated; profit and cost-risk considerations at definitization. | All of the first sentence except the last clause: no profit or cost-risk adjustment. Approval is through the SCO channel with a lag. The qualifying proposal is a seeded vendor event. Definitization is a work item that needs funding attached to the definitized amount; it never releases funds by itself. | The caps and the clock are the lesson; profit math is not. | None planned |
| ACSA | Reimbursable logistic support, supplies and services with partner nations under agreement; ordering and reimbursement procedures. | Available only when a partner is present; limited by partner stock; reimbursement is a ledger line later. | Scope. | M3 |
| Host nation support | Agreements vary widely. | Available only when an agreement exists; slow to establish, cheap, reliability per scenario, withdrawable. | Scope. | M3 |
| External support contract | Program-specific request and approval chain, task order competition, pricing, mobilization. | Requested from the OCS seat through the SCO; approval lag 10 to 20 days; mobilization 14 to 30 days; no competition modeled; price is a scenario parameter; minimum useful duration. No program is named in content. | Keeps content from going stale and keeps the lesson (start it early so the bridge can end). | None planned |
| Coordination | Informal arrangements between units; no contract. | A zero-cost arrangement that can be withdrawn when scenario events say the other party's priorities changed. | Teaches the difference between coordination and contractual execution. | None planned |
| Unauthorized commitments | FAR 1.602-3 ratification procedures and approval levels. | A unit directing a vendor creates a ratification work item that costs shop-days; no approval levels modeled. | Time cost is the lesson. | None planned |
| Terminations | FAR Part 49; cure and show-cause notices for default; settlement for convenience; legal review. | A termination is a work item; with SCO guidance it costs half the shop-days and carries a low dispute probability; without, full cost and a higher dispute probability that resolves weeks later. No settlement cost modeled yet. | The SCO lesson. | M3 for settlement cost |
| Warrants | Warrant limits and authority. | A scenario parameter; null means sufficient. A prep choice can raise it. | Not a mechanic until someone asks. | On request |

## Simulation model

| Topic | Real | Modeled | Why | Revisit |
| --- | --- | --- | --- | --- |
| Demand | Varies by climate, mission, equipment, season. | Linear per person per day per capability, times a site tempo multiplier. | Understandable; the scenario can vary the constants. | M3 climate factor |
| Dependencies | Complex. | Each capability lists the capabilities that gate it (power depends on fuel; water, lodging, food and comms depend on power; transport and MHE depend on fuel). Effective supply is rated supply times the worst gating coverage. | Explicit and teachable. | None planned |
| Buffers | Storage, reserves, slack vary by commodity and site. | One buffer per capability per site in days of demand; default 3; drained by coverage under 1.0, refilled over 1.0. | Forgiving on day 1, unforgiving on day 10. | None planned |
| Mission effect | Many factors. | Four outputs (sortie generation, throughput, endurance, resilience) from capability coverage with scenario weights. | Credible abstraction, stated to the player. | None planned |
| Reliability | Performance varies for many reasons. | A daily failure probability per vendor; the directory shows an estimate by information tier. | Seedable and explainable. | None planned |
| Transport | Routes, schedules, weather, priority, customs. | Links between islands with mode, weekly schedule, capacity per trip, weather sensitivity and priority-bumpable flag. A delivery needs a link trip. | Island setting makes transport the dependency it really is. | None planned |
| Information | Partial and uneven. | Four tiers per fact: assumed, estimated, surveyed, observed. Surveys, research and reports move facts up tiers. | Lets the debrief say avoidable, discoverable, unforeseeable. | None planned |
| Aggregate error rates | Unknown; they cluster at the start of an operation. | Front-loaded decay curves with placeholder values; can produce zero events. | Nick, 7 Nov 2026: tune in playtesting. | M2 and after |
| Shop capacity | People, skills, warrants, fatigue. | People times hours per day; actions consume hours; manual contract writing multiplies written-action time. | Pacing resource. | None planned |
| Contractor management | Accountability, base access, arming, medical, SPOT. | Out of scope except as access events. | Scope. | Post-1.0 |
| Joint structures | JRRB, JCSB, lead service designation and more, per JP 4-10. | Stood up through the SCO in 3 to 7 days; add 1 to 2 days per new requirement; merge duplicates, stop price-war events, expose the other service's vehicles, stop cross-service unauthorized commitments. | The joint turn in SPEC 4.4. | M3 |

## Added with the M0-05 simulation core (8 Nov 2026)

| Topic | Real | Modeled | Why | Revisit |
| --- | --- | --- | --- | --- |
| Fuel by air | Small quantities can fly; hazmat rules and aircraft capacity limit it. | Consumables (fuel, water, food) from another island move only on surface links (barge, road, sealift). | Keeps the barge dependency honest; small air lifts would be noise at this scale. | M3 |
| Link capacity | Barges and aircraft have cargo limits. | Schedule and open/closed only; `capacityPerTrip` is ignored in v0. | One constraint at a time. | M3 |
| Missing operator or maintenance | Untrained operators and no maintenance cause breakdowns and shorter equipment life. | They do not cut capacity; they add to the daily failure chance (maintenance wear grows after a grace period). Failure halves a continuous capability for the day and loses a consumable delivery. | Shows up as the thing a CCO hears about: equipment down. | None planned |
| Days in service | Wear, invoicing and goodwill run while equipment serves people. | Count only while the site is open. | A coordination arranged before the site opens is not yet being leaned on. | None planned |
| Execution confusion | Cash and card buys for services lack terms; disputes follow. | Rolls only for SF 44, GPC and unauthorized handshakes lasting four days or more; host nation agreements and coordinations are exempt. | Targets the lesson Nick named. | None planned |
| Supplies lines | A turnkey vendor sizes the fuel it brings to the site's need. | A "supplies" line is sized to the supplied capability's peak demand over the period times 1.25. | Vendor's sizing, not the player's. | None planned |
| UCA obligations | Obligations are made as funds and limits allow; definitization fixes the price. | Obligated daily to cover incurred cost, up to the cap (50%, 75% after the proposal; the quoted total once definitized) and the money left on the line. Three unpaid days stop work. | Shows the caps and the funding dependency without accounting detail. | None planned |
| Mission output | Commanders weigh sites by mission. | Each open site counts equally. | A small spoke carrying the flying mission matters as much as a large hub. | M3 commander weights |
| Spoke B length | The prose situation runs 90 days. | The first playable cut runs 30; the UCA overdue clock and funding expiry fall after day 30 and appear as pending. | Nick chose 30 days for the first playable. | spoke-b-90 at M1 |
