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
| Unauthorized commitments | FAR 1.602-3: ratification by an official with authority, with approval levels, legal review and a finding that the price is fair; the government may decline to ratify. | Situation 1 (handshake) and situation 2 (out-of-scope work under a contract) arrive as inbox decisions (SPEC 4.5). Ratify costs `ratificationShopDays` and obligates what was delivered plus a 7-day bridge; no approval levels, legal review or price finding. | Teaches the decision and its consequences without the paperwork chain. | M3 (approval levels by amount) |
| Handshake vendor patience | A vendor delivering unpaid on a unit's word eventually stops; how long varies. | Seeded 5 to 15 days from the day it is opened while it stays open or parked; ratifying stops the clock. On walk-off the vendor declines this office's work (50%) or quotes it 25% higher (50%) for the rest of the game. | One number the debrief can explain. | Playtest |
| After ratification | A ratified commitment covers past performance; continuing need goes on a contract. | The vendor keeps delivering on the unit's word, with no terms; deliveries past the bridge are owed and show as incurred but not obligated until the player contracts. | Shows the liability without ending supply the player never replaced. | None planned |
| Parked or ignored commitments | Long-unratified commitments and vendor claims draw scrutiny from higher headquarters. | After 10 days unresolved, an HHQ inquiry costing 24 shop hours: for a handshake, why it is unratified; for out-of-scope work, the vendor's claim. | Makes parking a cost later instead of a free pass. | Playtest |
| Refusing a handshake | Stopping the vendor is lawful; the need remains and the commander notices. | Supply stops the same day; a commander-confidence hit is counted. The priority-order penalty in SPEC 4.5 lands with commander priority allocation, which the sim does not have yet. | Priority allocation is not modeled in M1. | M2 |
| Precedent | Units that get away with going around contracting do it again. | Each site's unit has a habit level (0 to 60%): parked or refused handshakes and handshakes open 5+ days raise it; ratifying within 3 days lowers it; ratifying out-of-scope work raises it a little. With a habit above zero, a unit rolls once a day to make its own handshake when a capability was down yesterday and nothing on contract supplies it, never with a vendor that walked off. | A visible, explainable cause for the second and third handshake. | Playtest |
| Terminations | FAR Part 49; cure and show-cause notices for default; settlement for convenience; legal review; deobligation of the unperformed portion. | A termination is a work item. The unperformed portion of a fully obligated award is deobligated: back to its line while the line is current, lapsed if it has expired. SCO guidance and dispute probabilities arrive with the SCO channel. No settlement cost yet. | The money lesson without settlement accounting. | M3 for settlement cost and SCO guidance |
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

## Added with M1-02 (8 Nov 2026)

| Topic | Real | Modeled | Why | Revisit |
| --- | --- | --- | --- | --- |
| Definitization funding | The definitization modification obligates the definitized price with funds current at that time. | Definitizing obligates the remaining quoted total from the named line that day, as far as the line allows; any remainder waits on money. It never creates money. | Matches practice; makes fiscal-year timing matter. | None planned |
| Vendor capacity | Capacity is a daily limit. | Counted only across arrangements whose periods overlap the new one. | A follow-on after an expiring contract is not double-booked. | None planned |
| Incumbent follow-on | An incumbent already on site does not mobilize again. | No mobilization charge when the same vendor already has an active arrangement for the same capability at the same site. | Rewards planning the follow-on with the incumbent. | None planned |
| Fiscal-year money | Appropriations expire; next year's funds arrive on their own schedule. | Lines have available and expiry days; spoke-b-90 has om1 expiring day 75 and om2 arriving day 78, with nothing obligable in between. | The trap in SPEC 5. | None planned |

## Debrief tags (M2-08)

SPEC 4: avoidable means the information existed; discoverable means it was available for a cost; unforeseeable means no path to know. A gap takes the most controllable tag among its factors.

| Factor | Tag | Rule |
| --- | --- | --- |
| Missing supply line | Avoidable | An arrangement for a capability that brings this one (power's fuel supply) was bought without that line; the requirement's dependency checklist showed it from the day the card was submitted. |
| Uncovered requirement | Avoidable | A submitted requirement had nothing supplying it on a day it was needed; the debrief names the last arrangement that ended and the next one that started late. |
| Coordination withdrawn | Avoidable | A coordination was the only source and was withdrawn; a coordination is not a contract. Counted only if no contract supplied the need after the withdrawal. |
| Handshake lost | Avoidable | A handshake was parked or left open until the vendor walked off, or refused with nothing in its place. |
| Off-island consumable | Discoverable, or Avoidable once surveyed | Deliveries from another island wait on the surface link; before a survey of the site's island returned, the dependency was discoverable; after, it was known. Weather closures in the three days before are named as context. |
| Demand above supply | Avoidable | Supply sized below demand: continuous capacity below the load, or a consumable rate (a day) below daily use; population steps are in the plan from day 1. |
| Vendor failure | Avoidable without operator or maintenance lines, otherwise Unforeseeable | Missing lines raise daily failure odds. |
| UCA stop work | Avoidable | Incurred costs passed the obligation cap or the money on the line; the overdue date is named when there is one. |
| Execution confusion | Avoidable | A recurring service on a buy with no written terms. |
| BPA call declined | Unforeseeable | A BPA is not binding; any call can be declined. |
| Priority deferred | Avoidable | An award that would have supplied the capability (directly or through a supply line) was held back behind other sites' shop work because that site's commander lost confidence (a refused handshake), and it had not started on the gap day. |
| Dependency | Inherits | The gap in the capability it depends on. |

What the tags do not do yet: judge whether the information was worth its cost (a survey's shop hours against the gap it would have prevented). That is the counterfactual view (M3).

## Establishing a BPA (M2-12)

| Topic | Real | Modeled | Why | Revisit |
| --- | --- | --- | --- | --- |
| BPA set-up | FAR 13.303: terms, pricing, authorized callers, call limits, review; often competed among several vendors. | 2 to 3 shop days (manual factor applies before the system is online); scope is a set of the vendor's lines; call limit at or under the SAT; no pricing negotiation (offer price applies); no authorized-caller list. | The decision is which vendor and what scope, at a shop-time cost. | M3 (multiple BPAs, caller lists) |
| BPA walk-away | A vendor may stop accepting calls with notice. | Not modeled yet; only declined calls (decay curve) and refusal after an unpaid handshake. | Kept to the decline risk for the first playable. | M3 |

## Commander priority when capacity is short (M2-14)

| Real rule or practice | Game simplification |
| --- | --- |
| A commander who sees contracting walk away from a vendor his people relied on trusts the shop less, and his requirements get worked with less goodwill; the planner brings a priority list to the commander when capacity is short (SPEC 3). | Refusing a handshake (SPEC 4.5) costs that site's commander's confidence for the rest of the game. On a day the shop cannot finish all ready work, that site's work (awards, definitizations, terminations, payment rework, ratifications, denials and HHQ responses tied to it) is served after every other ready item. Among such sites, `actors.commander.priorityOrder` decides; unlisted sites go last. |
| Shop work is sequenced by the contracting officer, inside the commander's priorities. | Otherwise the queue is the player's: first queued, first served. Work tied to no site (surveys, BPA setup) is never held back. There is no player action yet to reorder the queue or to make the priority call (M3). |
| Confidence is rebuilt over time by delivering. | Not modeled: confidence does not recover within a game. |
| | The first day an item gets fewer hours than the plain queue would have given it, a compliance record says so and links the refusal; the debrief tags a gap "Priority deferred" when such an award had not started on the gap day. |

## Life support at Spoke B scenario sites (M2-17)

| Topic | Real | Modeled | Why | Revisit |
| --- | --- | --- | --- | --- |
| Host-nation support cost | Host-nation support is provided under an agreement and reimbursed outside the deployed shop's own funding lines. | Inherited `hostNation` arrangements bill nothing to the shop's lines; Main Island Utilities provides lodging and waste at all three sites and food at the hub. It is reachable only through those agreements, not by a CCO purchase. | Keeps free host-nation support from showing as an unpaid liability. | M3 (ACSA/HNS reimbursement) |
| Spoke B rations | Units arrive with MREs; hot meals need power and water. | Spoke B has no food capability in this scenario (MREs are not modeled). Spoke B lodging depends on power. | The food dependency on power would make every Spoke B power gap a food gap, which MREs do not suffer. | When Spoke B gets a field-feeding card |
| Spoke C field feeding | A camp moves from MREs to contracted hot meals. | Spoke C starts with 10 days of food on hand and card req0014 (field feeding from day 12); food depends on water there, so a water failure takes meals down too. | A real dependency on the island's other open requirement. | Playtest |

