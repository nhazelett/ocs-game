**Work session handoff** (Fable, scheduled run, 11 Nov 2026, DESK-09, stacked on DESK-08)

Expected look of this build and why: no new screen. An SF 44 for more than one day now reads at award as "Paid in cash at each delivery, $144 a day, about $1,152 in all. Each delivery is a trip for you and the paying agent: 2 shop hours." Each delivery day puts "SF 44 delivery and payment at Hub" at the front of that person's work. A held delivery says why in the day's records: "No delivery on ... today: it is paid in cash at the door, $X, and the cash box holds $Y. Finance tops it up on day N." or "... the boat to Third Island is not running, so the goods and the cash both stayed on Main Island."

## PRs reviewed this session

See docs/handoffs/2026-11-11-fable-desk-08-staff-meetings.md (same run).

## Changed

- Rules `sf44Deliveries` (new, schema too): `tripShopHours` 2, `offIslandTripShopHours` 6. `instruments.sf44.notes` updated.
- Sim (Session.Sf44.cs, new): an SF 44 with more than one day is `PaidPerDelivery`. Nothing is paid or obligated at award; only the first delivery has to fit the cash. Before the shop works each day, each delivery due is held if the surface link to an off-island site is closed (no trip, no payment) or the cash on hand cannot cover it (no payment, finance asked to top up); otherwise cash goes out, the delivery is obligated, and its trip goes to the front of the buyer's queue. A held delivery supplies nothing, incurs nothing and names its reason in the shortfall. A one-day SF 44 is unchanged.
- docs/assumptions.md, design doc Sim notes, BACKLOG DESK-09 row and status line, FOR-NICK #14.

## Why

D11: every delivery is a trip, and cash is paid per delivery, so the box can run dry and a delivery you cannot pay for does not happen. Nothing else.

## Validated how

568 Sim tests (5 new in Sf44DeliveryTests: cash at delivery, the trip and its assignee, the dry cash box, the closed barge on Spoke B days 16 to 18, the award stop and the one-day buy) and 121 Harness tests pass. validate.py and its unit tests, provenance.py and the game build pass.

Goldens moved, all from the policies or approaches that run a recurring service on an SF 44:
- tutorial approach2-cash-for-waste and policy-greedy: the award text, and shop hours worked 27 to 43 and 21 to 37 (eight 2-hour trips). Outcomes unchanged.
- spoke-b policy-textbook: the two award texts; shop hours 257 to 302 (the Spoke C food trips are 6 hours); the trips push the Spoke C food UCA award a day later (day 13); the ferry closure holds two food deliveries, so they are not paid or incurred (obligated $946,416 to $922,368, incurred $551,349 to $540,789, incurred but not obligated $5,808 to $141, HHQ inquiries 1 to 0). Mission output unchanged.
- sustainment policy-textbook: a 37-day SF 44 forklift rental at Spoke C now costs 6 trip hours a day (shop hours 510 to 714); the shop runs out of hours twice, CE complains to the commander, a fuel purchase order lands a day later and Spoke C fuel and MHE are down one more day; mission output 92% to 91%.
Do-nothing goldens and spoke-b-90 are unchanged.

## Unresolved

FOR-NICK #14: D11 says "two costs, and only two"; the existing no-terms confusion roll on recurring cash buys is kept (it backs Capt Mills's day 3 note). A daily trip for an equipment rental (the sustainment forklift) is what D11 says literally; it may want a per-rental payment instead after the gate. The textbook policies still pick long SF 44s; they could learn not to, but policies are a harness concern and were left alone.

## Next

DESK-10 (a vehicle for the contracting office), stacked on this PR.
