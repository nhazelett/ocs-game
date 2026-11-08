# M3-07 plan: the representative scenario ("Sustainment", hub plus three spokes, 60 days)

Fable, 8 Nov 2026. Design plan for backlog M3-07, written before any scenario code so the Sim work it needs can be built and reviewed in small pieces. First-pass numbers; the harness and the playtest tune them.

## 1. Which skeleton

M3-07 builds skeleton 3, **Sustainment: everything is already running** (scenarios/SKELETONS.md, approved by Nick 7 Nov 2026), as the representative mission. Reasons:

- SPEC 11's M3 row asks for hub plus three spokes over 60 days. Skeleton 3 is the only approved skeleton with exactly that shape (surge is hub plus two over 45 days; drawdown is hub plus three over 45).
- It is the scenario where the M3 systems are the game: transitions with overlap (M3-05, just built), funding lines that expire with a late replacement (M3-03), COR rotation and report fidelity (M3-04), an IDIQ near its ceiling and a BPA's ordering period running out (M3-06), a vendor default with the cure-notice decision (M2-18), and UCA definitization clocks.
- "All routes" (SPEC 11) fits as alternatives the player can reach for when an inherited arrangement fails or ends: a partner force with an ACSA on Spoke A, a host nation support agreement covering hub water, a stateside generator vendor by airlift or sealift, and the external support contract for an enduring base operating support package.

Reversible: this is content. If Nick would rather M3-07 be the surge or drawdown skeleton, the Sim pieces in section 5 are still needed (both skeletons inherit arrangements and terminate them), so nothing here is wasted. No question goes to FOR-NICK.

## 2. The world on day 1 (day 90 of the operation)

| Site | Island | Population | Notes |
| --- | --- | --- | --- |
| Hub | Main Island | 650 | Host nation grid and fuel point (inherited HNS), the shop (3 people, system online), the fuels flight |
| Spoke A | North Island | 220 | A partner force shares the island and has an ACSA; road to the port washes out in heavy rain |
| Spoke B | Second Island | 260 | The Spoke B of skeleton 1, 80 days on: barge twice weekly, weather-sensitive |
| Spoke C | Third Island | 150, rising to 210 on day 30 | Aircraft only for people; supplies by a weekly landing craft from the hub |

Capabilities in play at each spoke: power, fuel, water, lodging, food, waste; comms and transport at the hub. The shop has two funding lines: `om1` (O&M, expires day 45, the fiscal-year end) and `om2` (O&M, planned day 46, released late by a seeded chain).

## 3. The eleven inherited arrangements

| Id | Route | Site, capability | Days | The clock on it | Hidden until a file review |
| --- | --- | --- | --- | --- | --- |
| hubGrid | HNS (inherited) | Hub power | 1 to 400 | Withdrawable like any HNS | No |
| hubFuelPoint | HNS (inherited) | Hub fuel | 1 to 400 | Withdrawable | No |
| aPower | UCA, issued day -50 | Spoke A power | to day 70 | Qualifying proposal in hand; 40 days left to definitize (due day 40) | No |
| cLodging | UCA, issued day -120 | Spoke C lodging | to day 60 | Past due for definitization since before day 1; HHQ asks around day 15 | Yes: shows as an ordinary contract |
| bGens | BPA call on `bosBpa` | Spoke B power | to day 30 | Ordering period of `bosBpa` ends day 20: no new calls after it | No |
| bFood | BPA call on `bosBpa` | Spoke B food | to day 35 | Same vehicle | No |
| aFuel | IDIQ order on `fuelIdiq` | Spoke A fuel | to day 40 | `fuelIdiq` at 85% of ceiling: one more 30-day order fits, two do not | Yes: ceiling and ordered value |
| cFuel | IDIQ order on `fuelIdiq` | Spoke C fuel | to day 40 | Same ceiling | Yes |
| bFuel | Purchase order, Second Island Fuel | Spoke B fuel | to day 60 | Reported reliability 0.97, true 0.88; defaults day 25 unless cured or re-sourced; 15-day termination notice | Yes: true reliability |
| hubWaste | Purchase order | Hub waste | to day 75 | COR on it rotates home day 10 | No |
| aWater | ACSA order from the partner | Spoke A water | to day 50 | The partner withdraws water support in a dry spell (chain 6) | No |

Every inherited contract names a COR from a roster of four; two are fair, one good, one (the hubWaste and bFuel COR) rotates home day 10.

## 4. The eight event chains

Each chain has preconditions and a window, so the player's choices decide whether it fires and how hard it bites (SPEC 4 "never bare penalties").

1. **COR rotates (day 10).** `cor.rotates` on the bFuel and hubWaste COR: report quality drops to poor until the player designates another. Makes chain 3 harder to see.
2. **Ordering period runs out (day 20).** `bosBpa` reaches its ordering-period end; an inbox note on day 14 says higher HQ declined to extend it. Calls already placed run to their end (bGens day 30, bFood day 35); anything after needs a new vehicle, an award, or the external support package. Transition planner test: bGens to whatever replaces it.
3. **Fuel vendor slips, then defaults (days 12 to 25).** bFuel's true reliability is 0.88 from day 1 and drops to 0.6 on day 20 unless a cure notice was issued or a replacement started (precondition). Failures surface through the COR at the COR's quality (poor after day 10). The cure-notice decision (M2-18) is the decision point; terminating it without its 15 days' notice draws a claim (chain 3b, a vendor claim event: shop hours and money).
4. **Past-due UCA found by higher HQ (window days 12 to 18).** HHQ inquiry on cLodging unless it was already definitized or in definitization; a response is shop work. A file review in week one shows the UCA as past due before HHQ does.
5. **Fiscal-year end and a late replacement (days 45 to 52).** `om1` expires day 45; `funding.late` holds `om2` 5 to 9 days. Definitizing aPower and cLodging, and any new award, must land on `om1` before day 45 or wait.
6. **Partner dry spell (window days 28 to 36, precondition: aWater active).** `acsa.withdraw` for water with 3 days' notice. A contract or HNS request behind it holds; otherwise Spoke A water drains its buffer.
7. **Spoke C grows (notice day 22, arrival day 30).** `site.populationJump` +40% at Spoke C: lodging (on the past-due UCA, which cannot be modified while undefinitized past its cap) and food demand rise.
8. **Weather closes the Spoke B barge (window days 33 to 40, forecast to a surveyed island).** `link.closed` 3 days: fuel and food at Spoke B ride buffers; a replacement fuel source on Second Island, or the external support package, avoids it.

The trap (skeleton): new unit requests arrive days 2, 5, 9 and 13 (comms at Spoke A, transport at the hub, MHE at Spoke C, extra lodging at Spoke B), each a reasonable card that eats shop hours while the inherited clocks run. Good play reviews the files in week one, puts the clocks first, cures or re-sources bFuel before day 20, definitizes both UCAs on `om1` before day 45, plans every transition with overlap, and answers the new requests after.

## 5. What the Sim needs first (new backlog rows, Fable unless noted)

The schema already carries most of this (`inheritedArrangements[].ucaState`, `noticeDays`, `visibleProblem`), but the Sim ignores it today: `AddInherited` gives every inherited arrangement the vendor's full capacity, no price basis, no obligation, no UCA state and no notice.

- **M3-16 Inherited arrangement fidelity.** Inherited `quantity`, `dailyCostCents` (or the vendor's price), `obligatedCents` and `fundingLine`; inherited `ucaState` becomes a running `UcaState` with its clocks counted from `issuedDay` (which may be negative), so the 180-day and 50% definitization triggers and the HHQ inquiry fire on the right day; inherited BPA calls and IDIQ orders count against their vehicle; `noticeDays` on a contract makes a termination that ends it sooner than the notice a vendor claim (shop hours plus a settlement charged to the line). Schema additions for `quantity`, `dailyCostCents`, `obligatedCents`, `fundingLine`; ADR-0005 addendum. Every existing golden must stay unchanged (their inherited HNS rows bill nothing today and must keep doing so).
- **M3-17 Hidden inherited problems and the file review.** `visibleProblem: false` keeps an inherited UCA's overdue state, an IDIQ's ceiling and ordered value, and a vendor's true reliability (new optional `reportedReliability` on the inherited row) out of the views until the shop reviews the files: new `ReviewFilesAction` (shop hours per inherited arrangement, rules `fileReview`), a Knowledge record listing what it found, and debrief tags that treat an unreviewed hidden problem as discoverable and a reviewed one as avoidable. ADR-0006 addendum for the gated fields.
- **M3-18 Vendor reliability drop as an event effect.** `vendor.reliabilityDrop` (target arrangement, to a reliability, from the event day), with preconditions that read the existing performance-issue state ("no cure notice issued", "no other arrangement for the same site and capability started"). Lets chain 3 be authored rather than coded.
- **M3-07 The scenario itself** (after M3-16 to M3-18): `scenarios/sustainment/` with the world above, the eight chains and the four trap cards; five seeds visibly different; 200-seed ordering textbook > greedy > do-nothing; goldens for "clocks first" (textbook-like), "new requests first" (the trap), "cure bFuel", "re-source bFuel with a transition", and do-nothing. The textbook policy learns to review files on day 1, definitize inherited UCAs, and plan transitions for arrangements ending within the scenario; greedy keeps answering cards only.
- **Codex, after each Fable piece merges:** `tools/validate/validate.py` mirrors the new bundle checks (M3-16, M3-17, M3-18 rows will name them); game rows for the file review button and the inherited UCA's clocks in the ledger reuse existing screens; anything needing a new screen waits for the M2 gate.

Order: M3-16, M3-17, M3-18, then M3-07. Each is gate-safe under FOR-NICK #3 (Sim and scenario content; no new screens).

## 6. Pass criteria (first pass)

- No capability at any site down more than two consecutive days.
- No UCA past due when higher HQ asks (`compliance.hhqInquiries = 0`).
- Every transition plan clean (`transitions.gap = 0`), when the player made any.
- Nothing obligated on an expired line; `cost.unobligatedIncurredCents = 0` at the end.
- Mission output at least 88% (tuned after the first harness spread).

## 7. Risks

- Balance cost is in the chains, not the code (SPEC 9 risk 4): the 200-seed batch and the five-seed spread are the acceptance test, and the first numbers above will move.
- Inherited state touches the replay of every scenario. M3-16's acceptance keeps every existing golden byte-identical.
- The file review overlaps with the survey as an information investment; the debrief has to keep the two distinct (island facts versus contract facts).
