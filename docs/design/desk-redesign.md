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
- Staff meeting (D4): a per-day draw decides whether the meeting carries anything for contracting; scenario events can be tagged `heardAtStaffMeeting` with a later, worse fallback day. Built (DESK-08): the meeting is daily (1 hour, from whoever got the shop in), `contractingPermille` 400 with `opsTalk` lines on an empty day; a requirement card's `heardAtStaffMeeting.fromDay` is when it comes up in the room and its `submittedDay` is the later fallback. `RequirementEditorView.HeardAt` says whether the shop was in the room. Left out: meeting minutes, a choice of which meeting to attend, anything the commander asks you to report on.
- Own initiative (D5): a `shopVehicle` purchase lowers visit hours. One rule number. Built (DESK-10): `shopVehicle` is a `forShop` capability in the rules catalog, bought with an ordinary award at the shop's site; `shopVehicle.visitHoursPermille` 500 halves vendor meetings and SF 44 trips on the shop's island while it runs. `SessionView.OwnInitiative` lists it with its reason.
- Vendor knowledge (D6): `VendorOption` fields become nullable until learned; `meetVendor` and survey fill them. More reps in the scenarios.
  Built (DESK-05): `SessionView.VendorCards()` gives one card per vendor (host nation and ACSA suppliers excluded) with what they sell, cash and card, islands reached, what the office holds with them, the rep, and two sentences: where the card's facts came from and what is still blank and how to fill it. Unknown facts are null on the card. `VendorOption` keeps its types so the game build does not break; it gains `PaymentKnown` and `CapacityKnown` instead. Meeting fills in everything; doing business does too; a survey of their island, returned during play, fills in capacity and reach. Knowledge never gates a buy, so no golden moves. Every buyable vendor in the tutorial and both Spoke B cuts now has a rep.
- Pick two (D7): `AwardAction.Priorities` (two of fast, cheap, good). Rules block `pickTwo` with three numbers: extra shop days for competing, the premium, the added failure odds. The debrief names the pick when it explains a gap or a cost.
  Built (DESK-06): 2 shop days, 15%, 15 in a thousand a day, on purchase orders, BPA calls, IDIQ orders and UCAs. The Sim takes no pick as "none" (old logs and policies unchanged); the game always sends one (DESK-26). `ActionPreview.Trade` is the trade in one sentence with this award's numbers. The action record ends "Picked fast and good.", which is where the debrief's decision list shows the cost; a late competed award and a failing first-quote vendor say so in their gap factors. To strip it: Session.PickTwo.cs, the `Priorities` and `Pick` fields, the rules block and the lines marked DESK-06.
- COR fit (D8): `cors[].knows` (capabilities); detection odds drop off fit and with each additional contract.
  Built (DESK-07): minus 300 in a thousand off the COR's work, minus 150 per other running contract, floor 100, only for a COR with `knows` (the tutorial's two). `SessionView.CorChoices(capability)` lists the roster best odds first, each with a `Why` sentence; the preview's COR line uses the same sentence. Report lag is unchanged. Goldens did not move.
- Ask in view (D9): `ActionEditorView` already carries peak demand; add the card's requested start and days.
  Built (DESK-07): `RequestedStartDay`, `RequestedDurationDays` and `RequestedQuantity` (the customer's own words), null without a card.
- End of day (D1): `SessionView.EndOfDayCheck()` returns the open items as sentences. Read only.
- SF 44 (D11): a multi-day SF 44 pays per delivery from cash on the day, costs trip hours per delivery, and for an off-island site needs the link open that day. Built (DESK-09): rules `sf44Deliveries` (2 trip hours on the supplier's island, 6 across the water); the trip goes to whoever made the buy, at the front of their day; a held delivery supplies nothing, costs nothing and names its reason in the shortfall. The no-terms confusion roll on recurring cash buys is unchanged, and stays (Nick, 11 Nov, FOR-NICK #14: a third cost, so the SF 44 is not leaned on for recurring work).

## 5. Order

Codex can start now, with no Sim change: one End the day button (DESK-20), the Ledger without Terminate (DESK-21, using `SessionView.TerminateBlocked`), the own-initiative entry (DESK-25). Fable builds the Sim rows in this order: team, discovery, end-of-day check, vendor knowledge, pick two, COR fit and the ask, staff meetings, SF 44, shop vehicle. Each Codex screen row follows its Sim row. The port-to-port shipper and the higher-quality models come after the DESK gate.

## 6. The DESK gate, first play (11 and 12 Nov 2026)

Nick played the tutorial on the main build with everything above merged (PRs 180 to 184). His verdict: "it's shaping up well", and "Music is a nice touch, I'd like to get into the UI and UX soon." What did not work yet, in his words:

- "There's two scroll boxes here, and you can't really tell. It makes navigation confusing." (The award form scrolls inside the tab, and the tab scrolls too.)
- "Not a big fan of the left/right navigation here, either. It's kind of hidden and it hides your tabs." (Eight tabs do not fit, so the tab bar shows arrows.)
- "When this button is greyed out, it should explain why." (The reason exists, but it is above the button, often scrolled out of view, and a refusal like "Harbor Bottling can supply 5000 gal/day in total and 4500 is already committed" says a fact, not what to do.)
- "I tend to also get lost in the sauce about what's going on. Maybe I anticipate more needs? I'm not too sure." Asked whether the 4500 already committed was his own earlier water buy: "I don't remember. And there inlies the problem lol. I went to buy water, I probably did."

Nick agreed to the decisions below in chat ("Go ahead and implement the findings, and we'll keep working").

D13. **One scroll per pane.** A pane scrolls as one piece; nothing scrolls inside it. The button that acts (Arrange support) stays in view at the bottom of the pane.

D14. **Five tabs that always fit.** Today (the board), Requests (going to market opens inside the request it is for, so there is no separate Actions tab), Vendors, Payments (the ledger), Inbox. No arrows at 1280 by 800. The Debrief tab appears only when the game is over. The empty Reports tab goes.

D15. **A greyed button says why, under the button.** One sentence, always, directly under it: what is missing, or the first thing the Sim refuses. Refusals say what to do next, not only the fact ("Harbor Bottling has 500 gal/day left on those days. Ask for 500 or less, or buy the rest from someone else.").

D16. **Where things stand.** The Today tab gives one line per request: who asked, where it is (nothing yet, someone asking about it, a contract being written by a named teammate, arranged to start on a day, running, running short, ended), and who is on it. And when you go to buy something already arranged or being bought, the quote says so first ("You already have Harbor Bottling water at Hub, 4500 gal/day from day 5 to day 11."), because buying more than asked is sometimes right (D9) but buying it twice by accident never is.

Left out on purpose: a task list or calendar, notifications, a separate status screen, and any rule that stops a second buy. The answer to "maybe I anticipate more needs?" is to make the present readable first; anticipation is already rewarded by the staff meeting and own-initiative buys.

Sim notes (Fable):

- Where things stand (D16): `SessionView.WhereThingsStand()`, one entry per request on the desk, needing attention first. Each has the request, the person who asked, a state (`nothing`, `asking`, `buying`, `arranged`, `running`, `short`, `ended`), the teammate on it, and one sentence. Read only.
- Already in place (D16): the award preview's first soft gap names live or in-work support for the same site and capability on overlapping days. Not a stop.
- Capacity refusal (D15): says what is left on those days and the next step.

Order: Fable builds DESK-31 (where things stand) and DESK-32 (the quote says what is in place; refusals say what to do). Codex builds DESK-33 (one scroll, the button and its reason) and DESK-34 (five tabs) now, and DESK-35 (where things stand on Today) after DESK-31. Then Nick plays again, and the UI and UX pass he asked for starts with him. DESK-11, DESK-12, ART-10 and other Sim depth stay paused until then.

## 7. Nick's next replay: findings for the UI/UX specification (13 Nov 2026, Eastern)

Nick is reviewing the Windows demo linked from successful CI run [43544621253](https://github.com/nhazelett/ocs-game/actions/runs/43544621253), build `1394445`. The supplied screenshot shows **Morning of day 4** at **Hub**, in Night watch. The screenshot and these observations came directly from Nick in chat; no save or input log was supplied. The download link establishes the offered build, rather than independently proving the executable's version.

His overall verdict: "It's not 'bad'" and "It's getting there", but the game still feels "juggly" and needs a UI/UX overhaul to make it easier to follow. This is new playtest evidence for Fable to turn into design decisions, read contracts and ordered backlog rows. It is not a passed DESK gate or an implementation spec already agreed between the agents.

### 7.1 Available teammate hours where work is assigned

Nick cannot see how many hours a teammate has available while assigning work and wants to know whether he is overtasking them. The screenshot's top strip shows **8 of 8 h assigned** for each of the three people; that global strip has not solved the decision at the point of assignment.

Design outcome requested: the assignment UI makes each person's capacity, work already assigned and hours still available today readable before committing another task. Make the effect of the proposed assignment and any work carried beyond today clear. Fable should distinguish today's hours, the person's queued work and their different work speeds; the screenshot does not prove that the Sim actually over-allocates anyone.

### 7.2 Explain water and food shortfalls

Nick does not understand why water is not supplied, and has the same problem with food. In the screenshot the map says **Hub · down**, **Water 0% supplied**, **Food service 0% supplied**, without a nearby explanation or next step.

Design outcome requested: from each shortfall, show what the player knows about the cause, what support is already running or being arranged, who is working on it, and what the player can do or wait for next. Distinguish an absent buy, unfinished shop work, a future start, a held delivery or another actual cause using Sim facts. No cause is diagnosed from this image alone; a supply-rule bug remains unconfirmed.

### 7.3 Make prior decisions easy to find

Nick says, "I don't know what decisions I've made easily." The overall flow feels "juggly": he cannot readily connect what he chose, what the team is doing and what the sites are receiving.

Design outcome requested: an accessible, persistent account of the player's decisions and commitments, linked to the relevant request, person and current outcome. It must be easy to tell an instruction already accepted from an action still being composed, work in progress, and support actually delivered. Fable should define the surface and retention without assuming the existing Today request lines are sufficient.

### 7.4 Review the whole day's planned work before ending it

Nick asks for "something to show everything that you're executing on the day before ending the day" and "An update of everything you're gonna accomplish".

Design outcome requested: before advancing, let the player review the day's planned execution across the whole team, including who will do what, the capacity it consumes, likely completions, carryover and unresolved needs. The existing warning about unused hours or untouched requests is narrower than this request. Label forecasts and uncertainties honestly: a preview cannot promise a delivery or completion the Sim has not produced.

### 7.5 An end-of-day summary of actual results

Nick separately asks for "just an end of day summary".

Design outcome requested: after advancement, summarize what actually finished, progressed, slipped or remained undone; what changed in water, food and other support; and what carries into the next day. Connect the result to the earlier plan and player decisions. Keep this retrospective distinct from the pre-advance forecast, and base it on actual recorded outcomes. Fable decides its timing and placement alongside the morning board.

### 7.6 Rework requests and conversations as a legible flow

Nick wants to "re-attack requests and whatnot" because he does not know what unlocks by talking to specific people.

Design outcome requested: make the relationship between a request, the relevant people, what a conversation can help with, the time it takes, what was learned, and the resulting available action understandable. Show when a visit is queued, completed or has already supplied useful information. Fable should preserve discovery while giving enough context for a purposeful choice; distinguish learning information from actually unlocking a permission or action rather than treating every conversation as a hard gate.

### 7.7 Replace every character picture; prefer a 3D look

Nick's verdict on the current characters is "Ugly." His request is "new pictures for all of them, preferably something 3d."

Design outcome requested: a replacement direction covering the whole cast, including teammates, with a preference for a 3D appearance. A further small revision of the current portrait set does not satisfy the requested replacement. Fable should define the visual bar, consistency across small UI portraits and larger views, and whether this means rendered 3D portraits or real-time character models. The preference does not by itself choose a renderer, asset vendor or paid service. Reconcile this new owner request with D12 and ART-11's earlier "later" status when ordering the work.

### 7.8 Day-four screenshot: repeated and confusing task state

The screenshot also makes several concrete presentation problems available for Fable's review:

- The staff-meeting pane says the shop is already getting into the meeting, shows the same warning both above and below a disabled **Join the staff meeting** button, and also offers **Stop attending**.
- The visible content includes both **This takes 2 shop hours** and **This takes 0 shop hours**, each with an earliest result of day 4 and **154 hours already queued**. This describes what is visible, not proof that the two previews concern the same operation or that the queue is calculated incorrectly.
- All three top bars are full at **8 of 8 h assigned** while the task pane gives that large queue total without showing how it relates to a chosen teammate or today's work.

Design outcome requested: one coherent current status per task, clear context for each cost/queue/preview and its action, and understandable controls for a pending enrollment versus an active recurring meeting. Review duplicate notices and the explanation of that 154-hour figure against the actual state. The image proves confusing presentation, not the root cause or a contradictory Sim state.

### Fable follow-through

[Q22](../../backlog/QUESTIONS.md) routes this evidence to Fable. Update the specification/design decisions and add acceptance-scoped Sim and Codex rows for these findings, then order them. Reconsider section 6's explicit exclusion of a task list or separate status surface in light of Nick's new request for a daily execution review and decision history. Keep earlier decisions as history and state what the new pass supersedes. The UI gate remains unresolved; do not interpret this feedback as approval to resume the paused depth work.

## 8. Fable's answer to section 7: D17 to D24 (14 Nov 2026)

Section 7 is Nick's replay; these are Fable's decisions from it, built unless Nick strikes one. They supersede two lines of section 6: the "left out on purpose" list no longer excludes a separate status surface (D19 and D20 are one, because he asked for them), and ART-11 is no longer Later (D24). Everything else in sections 2 and 6 stands.

What the day-four screenshot most likely shows, from the Sim (no save was supplied, so this is a reading, not a diagnosis). Water at Hub has no supplier until the host nation's water point opens on day 9; the request asks for a bridge from day 2. Food service at Hub depends on water, so it drops to 0% the same day ("Food service at Hub down to 0%: water at 0%" is already in the records; the map card leaves out the "water at 0%"). A teammate's work is done strictly in the order it was given, out of that person's own 8 hours a day, so 154 hours already queued on one person is about 19 days of their work, and every new job given to them waits behind it. The quotes said "the earliest result is day 4, not counting the 154 hours already queued", which is true and useless: the real answer was about day 23. That one number explains most of 7.1, 7.2 and 7.8 together. The Sim fixes it first (DESK-37).

D17. **Hand out work knowing who is free.** Wherever work is assigned (any buy, visit, review or reassignment), the choice of person shows each teammate's day: free hours today, hours already queued, the day their queue clears, and the day this job would be done if it went to them ("MSgt Avery: 8 hours free today, nothing queued; done day 5." / "You: 154 hours queued, clear about day 23; this would be done about day 24."). Every earliest-day number in every quote counts the work ahead of it in that person's queue. Overloading someone stays allowed (sometimes it is right), but it is never hidden. Left out: reordering a person's queue, priorities, overtime, a cap on assignments.

D18. **Every shortfall says why, where it shows.** Wherever a site shows a capability short (the map card, the Today line, the request), one sentence from Sim facts says why and what is in motion: nothing bought; bought, still being written by a named teammate, done about day N; arranged, starts day N; delivery held (the barge did not run, the cash box was empty); a vendor missed or delivered short; running, but less than the site needs; or depends on another capability that is short ("No water, so no food service: the kitchen needs water."). What the office has not been told stays untold ("The site reports it short. Nobody has told you why yet."), so the COR and fog of D8 keep their teeth. Left out: a cause tree, percentages per cause.

D19. **A plan of the day before it ends.** End the day opens one page before it ends: each teammate in turn, the work they will do today in queue order with the hours it takes, what finishes today and what carries over; then deliveries and starts due today; then what DESK-04's check already says is open. It is a forecast and reads like one ("should finish today", "starts if the barge runs"): the Sim's hours are certain, rolls and events are not, and the page never promises a delivery. Two buttons: End the day, and Back. Left out: changing the plan on this page (go back and reassign), a calendar.

D20. **What happened today, after it ends.** After the day plays back on the map, one page says what actually happened: what finished and who did it, what started, what was supplied short or missed and why (D18's sentence), what slipped against the morning's plan, and what carries into tomorrow. It closes into the next morning's Today tab, and Today keeps a "Yesterday" link to it. Built from the day's records, never from the forecast. Left out: a weekly report, charts.

D21. **Your decisions, in one list.** The Today tab has a short list under Where things stand: what you decided, newest first, one line each with the day, who it went to and how it stands now ("Day 2: water from Harbor Bottling at Hub, 7 days, given to you. Still being written: about day 23."), each line opening its request. The request page lists the decisions made for it. Only what you committed is in the list; what you are still composing never is. Left out: undo, notes, filters.

D22. **On a request, who can help and what talking to them does.** The request page names the people worth asking for this request and, for each: what they can tell you or do, the hours it takes, and where it stands (not asked; asked, answer due day N, with whom; answered, and what you learned). Each says whether talking gives you information for the request (CE tells you nobody runs the plant) or opens something you can then do (the J4 can add the site to the ACSA order; a vendor meeting fills in their card). Discovery stays discovery: the page names who to ask, never the answer. Left out: a dialogue tree, relationship scores.

D23. **The staff meeting says one thing.** The meeting pane shows one state (not attending, getting into the meeting, attending) and the one button that changes it, with its cost once: joining takes 2 hours once, each daily meeting 1 hour. No warning repeated above and below a button; no Stop attending when you are not attending, no Join when you already are.

D24. **A new cast, in 3D.** Every face in the game is replaced, teammates included, by rendered 3D busts from one original character kit: one sculpt style, one light, the Tabletop palette, varied by face shape, skin, hair, age, glasses and uniform or civilian clothes, rendered to still portraits at the sizes the desk uses (the strip's small faces and the larger faces on requests and vendor cards). The teammate picker offers the same set. Live 3D character models stay later (they need scenes the game does not have). The bar: at 48 px every face is still a face and no two people in a scenario can be mistaken for each other; nothing photoreal or uncanny; original, with provenance; the art review's squint test and the cast sheet before and after.

Sim notes (Fable):

- Queue-aware timing (D17): every preview's earliest result and start count the assignee's unfinished work given before it, less the hours they work before the new job could begin. `SessionView.AssigneeChoices(action)` gives, per teammate, free hours today, hours queued, the day the queue clears, the day this action would be done with them, and one sentence. `TeamMemberView.QueueClearsDay`. `ActionPreview.QueueAheadHours` keeps its meaning; the game's "not counting the N hours already queued" becomes "after the N hours already queued". Goldens move only where a policy reads the preview.
- Why short (D18): `SessionView.WhyShort(siteId, capabilityId)`, one sentence or null when supplied in full; the map day view and `RequestStanding` carry it. Built from arrangements, work items, held deliveries, failures already known to the office and capability dependencies; never from what the office has not been told.
- Day plan (D19): `SessionView.DayPlan()`: per teammate, the work they will touch today in queue order with hours and whether it finishes; deliveries and starts due today; the DESK-04 sentences. Read only; computed with the same rules as the day loop, so its hours match what the day does.
- Day summary (D20): `SessionView.DaySummary(day)` from the day's records: finished (with who), started, short or missed (with D18's sentence), slipped against that morning's plan, carried over. Read only.
- Decisions (D21): `SessionView.Decisions(cardId?)` from the action records: day, sentence, teammate, request, and how it stands now.
- Who can help (D22): `RequirementEditorView` asks gain what the person can tell or do, the hours, the state with the answer day, the answer once given, and whether it informs or opens an action.

Order: Codex builds DESK-49 (the staff meeting pane) and ART-20 (the new cast) now, with no Sim change. Fable builds DESK-37, then DESK-38, DESK-39, DESK-40, DESK-41 and DESK-42, one PR each; each Codex screen row (DESK-43 to DESK-48) follows its Sim row. Then Nick plays the tutorial again. DESK-11, DESK-12, ART-10 and other Sim depth still wait.
