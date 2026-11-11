# The desk redesign (DESK milestone)

Nick played the tutorial on 10 Nov 2026 on the post-ART build (3D map, greyed award choices, sound cues). This is the M2R gate result and what follows from it. He agreed to everything below in chat ("Agree with anything written. Go ahead and write it up.").

## 1. The gate result

Better, and the right feel, but not there. The opening works: he liked the morning tip that sends you to finance, liked meeting vendors, liked the MHE order on day 3. The desk does not work yet. In his words:

- "I don't understand why I'm selecting Plant Operators or Maintenance. Seems like it's there and I don't know what to do with it."
- "I'm not understanding 'without a request.'"
- "Directory seems overwhelming. I'm not sure what we're looking at here."
- "Beyond talking to people in the morning, I don't know what I was trying to do day 2."
- "'Someone is already on the way to the finance office.' I don't know who that is or what they're doing."
- "For COR selection, I don't get it. Why on earth would I ever select TSgt Pratt?"
- "Not too sure what I'm looking at in the ledger."
- "Everything is 0% on this page and I'm not too sure how to navigate to what's next."
- "I should have been clicking 'continue' vs 'next day'." (He had it backwards, which is the point: two buttons end the day and their labels do not say how they differ.)
- "I don't know if I clicked terminate in the ledger, and therein lies the problem."

The 0% dead end was a defect: the Ledger offered Terminate on the host nation's power and fuel and the Sim accepted it. Fixed in DESK-01.

M2R closes here. The next gate is the DESK gate: the same cold-start play, after the rows below.

## 2. Decisions

D1. **One button ends the day.** "End the day", always one day. Before the day ends it says what is still open (a teammate with free hours, a request due and untouched). Continue goes away, or lives in the Menu as "skip quiet days".

D2. **A named team of three.** You, an experienced CCO and a junior CCO. Each has their own hours for the day. Every task is assigned to a person and comes out of that person's hours, so the work can be divided. Anything not assigned stays with you. The experienced CCO gets through work faster than the junior; that is the only difference at first. The team does not replace hours: the strip shows three short named bars where it showed one anonymous one. Every sentence that said "someone" names the person. The player can rename each teammate and pick a face from the portrait set at the start of a game. Better character models are a later ART item.

D3. **No scope checkboxes.** Plant operators, maintenance and the like are no longer boxes the player ticks without knowing why. The request a customer hands you is incomplete, as it is today; you find the missing part by asking the right person (CE tells you nobody is running the plant), and it is then added to the request. A missing part nobody asked about still bites, as the Spoke B fuel does. "Go to the right people" is the mechanic: the J4, CE, FSS, LRS, finance, the SCO.

D4. **Staff meetings are not always useful.** Some days it is sorties and downed jets and nothing for contracting. Some days a commander raises a problem (out of lumber, fuel berms to build) and a requirement lands on your desk because you were in the room. A requirement you were not there to hear arrives later, and later is worse.

D5. **Buying on your own initiative is its own choice.** "Without a request" leaves the list of what you are arranging. A separate entry, "Buy on your own initiative", covers things nobody asked for. The first use is a vehicle for the contracting office: it gets you downtown and makes vendor visits cost fewer hours.

D6. **Vendors, not a Directory.** One card per vendor: what they sell, cash or card, which islands they can reach, what you hold with them. What you have not learned is blank, not hidden. Meeting vendors fills the cards in, and there are more vendors to meet than today. Later: a vendor who ships port to port, which lets something bought on one island reach another (new Sim logic; after the screens).

D7. **Fast, cheap, good: pick two.** On every written contract. Cheap and good means you competed it and it takes longer. Fast and good means you paid a premium. Fast and cheap means you took the first quote and quality is the risk later. Competition and evaluation appear as consequences of the pick, which is Nick's "points of emphasis" without an administration screen. Built to be stripped if he does not like it.

D8. **The COR choice has a reason.** A COR from the unit that knows the work (CE for MHE) catches more. A COR already watching another contract is stretched. The quality word alone no longer decides it.

D9. **The customer's ask stays in view.** Quantity and duration show what was asked for next to what you are about to buy. Buying more than asked is sometimes right.

D10. **The Ledger is "what we are paying for".** No Terminate buttons in the list. Terminate exists only on the office's own contracts, on that contract's own view, and asks first: "End this contract? Here is what stops." Host nation and ACSA support never offer it (DESK-01 already refuses it in the Sim).

D11. **The SF 44 for recurring buys has two costs, and only two.** Every delivery is a trip (you and the paying agent, shop hours each time, the barge for a spoke; a closed barge stops delivery and payment). Cash is paid per delivery, so the box can run dry before finance tops it up, and a delivery you cannot pay for does not happen. Not built: unauthorized commitment (Nick: the CCO is authorized, so it is not one), price creep, splitting findings, paying agent clearing.

D12. **Art.** Higher-quality island and facility models, moving in steps toward the best the engine can do before release, without breaking what works. Character models later.

## 3. What leaves the screen

The scope checkboxes. The Directory tab. Terminate buttons in the Ledger. The Continue button. "Without a request" in the arranging list. The anonymous hours bar. The Vehicles tab folds into the vendor cards.

## 4. Sim notes (Fable)

- Team (D2): scenario `shop.team` lists three members with hours a day and a speed. Work items carry an assignee; a person's free hours bound what they can take that day. Names and faces are prep inputs so replays read the same. Existing scenarios get a default team whose hours sum to today's shop hours, so goldens move only in text.
  Built (DESK-02): each person has 8 hours a day in the tutorial (the rules' hours per person), the experienced CCO works at 1.25 and the junior at 0.75. A work item is worked only from its own person's hours, so one person writes one contract, as "shop days" in the rules always meant. That moves the tutorial's numbers, not just its text: the approach logs give the forklift call and the waste order to the experienced CCO and keep their outcomes; the textbook policy divides work and no longer falls behind on small buys. Scenarios without `shop.team` are you alone with all the shop's hours and do not move at all; Spoke B gets its team in DESK-12, after the gate.
- Discovery (D3): each non-core line gains `knownBy` (a person or role) and `revealedBy` (the visit that surfaces it). Lines are added by an "add what you learned" action that only exists once revealed. `setLine` stays in the API for the policies and old logs.
- Staff meeting (D4): a per-day draw decides whether the meeting carries anything for contracting; scenario events can be tagged `heardAtStaffMeeting` with a later, worse fallback day.
- Own initiative (D5): a `shopVehicle` purchase lowers visit hours. One rule number.
- Vendor knowledge (D6): `VendorOption` fields become nullable until learned; `meetVendor` and survey fill them. More reps in the scenarios.
- Pick two (D7): `AwardAction.Priorities` (two of fast, cheap, good). Rules block `pickTwo` with three numbers: extra shop days for competing, the premium, the added failure odds. The debrief names the pick when it explains a gap or a cost.
- COR fit (D8): `cors[].knows` (capabilities); detection odds drop off fit and with each additional contract.
- Ask in view (D9): `ActionEditorView` already carries peak demand; add the card's requested start and days.
- End of day (D1): `SessionView.EndOfDayCheck()` returns the open items as sentences. Read only.
- SF 44 (D11): a multi-day SF 44 pays per delivery from cash on the day, costs trip hours per delivery, and for an off-island site needs the link open that day.

## 5. Order

Codex can start now, with no Sim change: one End the day button (DESK-20), the Ledger without Terminate (DESK-21, using `SessionView.TerminateBlocked`), the own-initiative entry (DESK-25). Fable builds the Sim rows in this order: team, discovery, end-of-day check, vendor knowledge, pick two, COR fit and the ask, staff meetings, SF 44, shop vehicle. Each Codex screen row follows its Sim row. The port-to-port shipper and the higher-quality models come after the DESK gate.
