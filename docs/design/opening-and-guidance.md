# Opening and guidance: the M2 gate redesign

Status: Accepted. Nick reviewed the first-five-minutes script and answered the open questions in chat on 8 Nov 2026 (section 12).

## 0. Why this exists

Nick played the M2 build (Actions run 42514805058) on 8 Nov 2026. Verdict: unplayable. Not "data entry": he could not tell what the game was. He landed on a scenario dropdown showing a file name, a replay control playing one of our test games to day 20, a requirement called `req0007`, a vendor preselected that the Sim then refused with a red hard stop, and a map of two dots and two lines. No one told him who he was, where he was, or what was due.

The Sim is in good shape (326 Sim tests, 1,000-game crash run clean, the SPEC 5 stories reproduce). The experience on top of it was a debugging harness. That is the M2 gate's "redesign before anything else" outcome, wider than the editors: the opening, the guidance and the language all have to be built before any more Sim depth is added.

Process failure, recorded so it does not repeat: Fable declared M2 "ready for playtest" from merged items and green tests without seeing a screenshot (Codex's screenshots were in Git LFS, which Fable's sessions cannot download). See section 9.

## 1. What Nick said (source for everything below)

1. **The first 72 hours deployed.** You get the turnover from the outgoing CCO: cash on hand, the contract-writing system (or not), the existing contracts, maybe a vendor base. No OPORD, nothing from the SCO.
2. **Customers.** CE is the neediest and never stops: lumber, piping, nails, tools. You could slow that stream by standing up a decentralized GPC program, but it takes a while. As the deployment goes on, units discover what they do not have and the requirements widen. As things calm down: FSS wants morale items, SFS wants guard shacks with AC, LRS ramps with a direct action (heavy equipment, fuel berms).
3. **Who comes to the CCO.** Customers. You may never hear from the SCO; you reach out to their staff when you need them. You find finance. The site commander sometimes comes to you; usually you get yourself into the staff meeting. Vendors seek you out to get on your radar or to clean up old contracts.
4. **Guidance.** The SCO is a good fit for the guiding voice, or a mentor. In the tutorial the mentor is heavily involved. In the real game, not so much: the player decides alone.
5. **Pace.** One heavy decision per day is attractive if the scenario is built for it; it keeps the big picture in view.
6. **Seeing it.** When you decide, you see it happen. A scrollable map, Command and Conquer style camera, no fog of war, and not bad graphics.
7. **Who plays.** A CCO alone at their desk. Any contracting professional, really.

## 2. Design rules that follow

R1. **The game opens as a turnover, not a menu.** The first thing the player sees is a person handing over a shop. Scenario files, replays, save slots and seeds are developer tools and live behind a developer switch.

R2. **Work arrives from people.** Every requirement is brought by a named customer from a named unit, in their words. The card is what the requirement becomes when the player opens it, not how it arrives.

R3. **The player goes and gets things.** Finance, the commander, the SCO and the market are not in the inbox. Going to them is an action that costs time and returns something.

R4. **Facts in the game, directions only in the tutorial.** The tutorial's mentor may say "look at fuel." In a scenario, the SCO's staff answer what they are asked with facts and limits, and the game never tells the player what to do. This is Nick's rule for his kits, applied to play.

R5. **One day, one decision in front.** The morning leads with the one thing that most needs a decision today, with the rest behind it. Quiet days are skipped. Scenarios are built so most days have one such thing.

R6. **No identifiers on screen.** Requirements, sites, vendors, contracts, scenarios and people have names. `req0007` appears nowhere a player can see.

R7. **Decisions are visible on the map.** The map is the main view. A delivery is a truck; a barge is a barge on its schedule; a generator that fails goes dark. The map scrolls; nothing is hidden by fog. First as an illustrated 2D map, then the diorama if M3's gate chooses it.

R8. **It teaches itself.** No instructor is assumed. The tutorial plus in-world guidance must carry a first-time player to the debrief.

R9. **Game text reads like a person wrote it.** No bullet lists, no headers, no bold labels in anything a player reads in the game. The turnover is four short paragraphs in the outgoing CCO's voice, not four cards of bullets. Nick: "we don't want the game looking like AI format, and bullet points highlight it."

R10. **Fuel is not the CCO's buy.** Bulk fuel comes through the ACSA manager (J4) or DLA Energy, so the CCO's move on a fuel dependency is to get the J4 engaged, through the OCS seat. The local fuel vendor stays in the market as the expensive stopgap (a purchase order or SF 44 delivery while the ACSA line is set up), and a turnkey power vendor may bring its own fuel. A CCO who sees fuel uncovered and does nothing still owns the gap.

## 3. The opening

Three screens, each a person talking, each short. Everything they say is drawn from scenario data the Sim already has.

### 3.1 Before you leave (the pre-deployment window, SPEC 4.2)

The home-station deployment manager or the SCO's staff, by message: "You have fourteen days before wheels up. Here is what you can get done." The existing prep choices appear as things to spend days on (GPC accounts, system access, a paying agent arrangement, market research), each with what it buys you and what it costs in days. This is `PrepScreen.View` in a person's voice. Picking is optional; the default picks are sensible and labeled.

### 3.2 The turnover

The outgoing CCO, by name, on your first morning. Four cards, in this order, each one sentence of theirs plus the facts:

1. **The shop.** People, hours a day, the contract-writing system (online, or "down until about day 12; everything's manual until then"), cash on hand with the paying agent, purchase cards or not.
2. **The contracts you are inheriting.** Each inherited arrangement as a line: what, who, where, through when, and one remark ("the fuel guy's been fine," "I never got around to definitizing that one"). Hidden problems stay hidden here (M3-17); the remark is what the outgoing CCO believes.
3. **The vendor list.** "Here's who I've used. Half of these I've never called." The directory with its confidence levels.
4. **The money.** Funding lines as plain lines: what it is for, how much is left, when it ends.

Then: "Capt Ruiz from CE is already outside." The turnover ends by opening the first customer.

### 3.3 Day 1

The map, with the hub lit and Spoke B grey with "opens day 7" on it. The inbox has the first customer. The one-decision frame (R5) shows it. The tutorial's mentor is present; a scenario's is not.

## 4. The first five minutes (script for Nick to react to)

Spoke B scenario, no tutorial.

1. Title. "New game" (Spoke B, or the tutorial), "Continue". Nothing else.
2. Before you leave: three choices preselected within the budget, one sentence each. Continue.
3. Turnover, four cards, about ninety seconds to read. The outgoing CCO's last line: "Spoke B opens in a week and nobody's bought them power yet. CE's outside."
4. The map. Capt Ruiz, 3 CES: "Advance team at Spoke B needs power by day 7. I wrote it up as six sixty-kilowatt generators." The requirement opens as a card with the generator line on and the fuel, operator and maintenance lines off, and the dependency checklist shows fuel uncovered, with "J4 (ACSA)" as the usual owner of that line.
5. The player's first decision is whether to take the requirement as written. Either way they then go to the market and see three ways to get generators, with cost, shop time and earliest start. Awarding something is the end of the first five minutes. Getting the J4 to put Spoke B on the ACSA fuel order is a separate action the player may or may not take; the day 10 story turns on it.
6. Day 2 or 3, the first visible consequence: the survey comes back (if ordered), or the fuel question lands when a tutorial mentor or the dependency checklist makes it unavoidable. By day 10 the player has seen the game answer a decision they made.

What the player should feel at minute five: "I know what I have, I know what's due, and I just did something about it."

## 5. People

Scenario data gains `people[]`: id, name, rank, unit, role (customer, outgoingCco, scoStaff, finance, commander, vendorRep), portrait key, and short lines for the moments the game uses them. Requirement cards reference a person instead of a free-text `submittedBy`. The existing actors (sco, fm, commander) become people.

Customers and their rhythm, from Nick:

| Phase | Who | What | Mechanism |
| --- | --- | --- | --- |
| From day 1, constant | CE | Lumber, piping, nails, tools; many small buys | Small-buy stream (section 6) |
| First two weeks | Units discovering what they lack | Power, water, MHE, comms, lodging | Requirement cards as now, brought by people |
| As things calm | FSS | Morale items | Cards, lower priority, visible in the staff meeting first |
| As things calm | SFS | Guard shacks with AC | Cards with dependency lines (power) |
| With a direct action | LRS | Heavy equipment, fuel berms, surge fuel | Event chain with a population or tempo jump, large cards |

## 6. New mechanics the answers require

### 6.1 The CE small-buy stream and the GPC program

A steady stream of small requests (SF 44 or purchase card sized) consumes shop hours every day. It is the background load, not a decision each time: the morning board shows "CE: 4 small buys today, 3 hours" and the shop works them unless the player defers them (which CE notices). Standing up a decentralized GPC program is an action: shop days over several weeks (cardholder nominations, training, approving official, billing official), after which the stream moves off the shop. The tradeoff is hours now for hours later, and it is visible on the shop gauge. Rules block `smallBuys` and `gpcProgram`; Sim side M2R-03.

### 6.2 Going to people

Actions with a shop-hour cost that return facts:
- **Find finance.** Establishes or speeds the paying agent and cash replenishment; returns the real cash and card timing.
- **Get into the staff meeting.** A recurring weekly slot once joined: the commander's priorities, and requirements before they are submitted (the "units discover what they lack" preview). Joining costs hours; missing weeks costs the preview.
- **Ask the SCO.** Reachback: a question from a short list about a rule, a vehicle or a route; returns a fact a few days later. In the tutorial the answer may include a direction; in scenarios it never does.
- **Meet a vendor.** When a vendor comes by: add them to the directory with a confidence level, or hear about an old contract (an inherited claim or unpaid invoice).

### 6.2a Fuel through the J4

Spoke B's fuel moves from a thing the CCO buys to a thing the CCO gets someone to own. The hub's fuel is an ACSA line (partner stock, barge delivery); `engageJ4` (OCS seat, or the CCO through the OCS seat) asks the ACSA manager to add a site and a daily quantity, with a lag and the partner's stock as the ceiling. The dependency checklist names the usual owner of each line (fuel: J4 ACSA or DLA Energy; operators: the unit or the vendor). The local fuel vendors stay as the stopgap at a stopgap price. The SPEC 5 approach stories are rewritten on this basis: approach 1's day 10 gap is "nobody owned the fuel" (avoidable: the checklist named the owner on day 1); the day 16 barge gap is unchanged because the ACSA fuel still crosses by barge; approach 2's fix is the J4 call plus the bridge, or a turnkey vendor that brings its own fuel.

### 6.3 One decision in front

`MorningBoardView` gains `Lead`: the single item the day leads with, chosen by a fixed rule (an unresolved inbox decision, then a requirement past its start with nothing covering it, then an alert, then the nearest clock). The game shows it large; the rest of the board is behind it. No new Sim state.

## 7. What changes on screen (Codex)

In priority order. Each one is a backlog row; each ships with a screenshot Fable can open (section 9).

1. **Front door.** Title, New game (tutorial or Spoke B), Continue. Developer tools (scenario file, approach replay, seed, load) behind a developer toggle, off by default.
2. **Before you leave, and the turnover.** Sections 3.1 and 3.2 as screens.
3. **Plain names everywhere.** Requirements by what and who; sites, vendors, contracts and scenarios by title; people by rank and name.
4. **Today's decision.** The board leads with `Lead`; the rest collapses under it.
5. **People in the inbox.** Requests as messages from people, with the card behind "open."
6. **The map as the main view.** Sites drawn with their capability state as things (generators, tanks, tents), the barge and aircraft moving on their schedules, deliveries as trucks, a failure as a thing going dark. Scrollable. Illustrated 2D now; diorama later.
7. **Market, not editor.** The action editor becomes "go to the market for X": pick who, then how, with the live preview as now, and no preselected vendor the Sim will refuse.
8. **Going to people.** Finance, staff meeting, SCO, vendor visits as actions from the map or the board.

## 8. The tutorial

Skeleton 2 (hub, 20 days, three requirements) with the outgoing CCO staying three days as the mentor (Nick's call, 8 Nov 2026). The mentor directs (R4 allows it here): "Open that. See the fuel line? That's the one people forget." Each of the three requirements teaches one instrument as the skeleton already says. The mentor leaves on day 4 and the player finishes the twenty days alone. Nick's one-sentence lesson test applies at the end.

## 9. Process changes

- **Screenshots Fable can open.** Game PRs commit screenshots as plain PNG under 400 KB in `docs/handoffs/images/`, not through LFS (`.gitattributes` gets an exception for that folder). Fable's review of a game PR includes looking at them. A game PR without a screenshot of what changed is not ready to merge.
- **Milestone readiness.** No milestone with a player-facing surface is declared ready for Nick until Fable has seen the opening and each panel as a player would, and has written down what a first-time player sees in the first two minutes.
- **Sim depth pauses.** No new Sim routes or scenarios start until the opening and guidance rows are merged and Nick has replayed the opening. In-flight Sim work stays merged; nothing is reverted.
- **Gate result.** FOR-NICK #4 records this playtest. The milestone review for M2 is amended with it.

## 10. Milestone

**M2R, Playable opening.** Done when: Nick starts the game cold, and within two minutes can say who he is, where he is and what is due; he plays Spoke B start to finish from the turnover to the debrief without touching a file; the first visible consequence of one of his decisions appears by day 10; he states the one-sentence lesson and it matches an intended outcome. Decision gate (the original M2 question, now askable): does building a contract action feel like deciding?

M3-07 and the remaining Sim routes resume after M2R.

## 11. Open questions for Nick

1. The first-five-minutes script in section 4: right shape? What would the outgoing CCO actually say in their last line?
2. The mentor in the tutorial: the outgoing CCO staying three days, or the SCO's staff by message? (Section 8 assumes the outgoing CCO.)
3. CE's small buys: is "four a day, three hours" the right order of magnitude for a 300-person hub in week one?
4. The staff meeting as a weekly recurring action you have to get into: does that match how it felt?
5. Which real unit names should appear (3 CES, FSS, SFS, LRS as written), and any you want kept out?

## 12. Nick's answers to section 11 (8 Nov 2026)

1. The script is fine. Game text must not look like AI format; no bullet points in the game (R9).
2. The outgoing CCO is the tutorial mentor.
3. Four CE small buys a day, about three hours, is fine for a 300-person hub in week one.
4. The staff meeting as a weekly thing you get yourself into, with requirements visible before submission: yes.
5. Unit names (3 CES, FSS, SFS, LRS) stay for now; in a joint environment, especially Army-led, they would change. Evolve later.
6. Added by Nick: fuel is usually ACSA, handled by OCS and the J4, not the CCO; a CCO who ignores fuel can still be punished; the goal is to get the ACSA manager involved (R10, 6.2a). Fable's addition: DLA Energy is the other bulk source, and the local fuel vendor stays as the stopgap.

