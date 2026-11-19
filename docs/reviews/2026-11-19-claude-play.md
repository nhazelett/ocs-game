# Claude plays the tutorial, 19 Nov 2026

The day's play (PLAY-03, docs/PLAYTEST-CLAUDE.md). A fresh agent got only the brief in section 3 and the driver, plus a notes file of its own. It never read the repository.

- Build: `main` at 8fb3f7f (after DESK-102 to DESK-110: a buy in work that leaves out a part is flagged, Stop this work, the debrief says what carried each line), Godot 4.6.1 .NET, 1280 by 800, Xvfb, Dummy audio.
- Seed: 20261119.
- How far: the end of day 16 and the debrief, in 273 commands. It was never stuck for ten steps in a row; it spent about 28 steps on day 5 trying arrangements and suppliers before it found how to put the drivers on the request (note 15).
- Logs: [commands](plays/2026-11-19/commands.txt) (the number is the screenshot it produced; step 165 timed out in the driver, and the click took effect) and [input log](plays/2026-11-19/input-log.json). The harness replays the log to the same game (`run tutorial --log`, state hash 8d96b798).
- Pictures: four of the 274 the play took, all from the same game.

## The result, from the replay

Every pass line met, mission output 99%, on 68 shop hours. Prep was all five choices (9 of 14 days). Water went on the card to Island Mart on day 1 and ran from day 2 until the water point came back on day 9. Novak got the shop into the staff meeting on day 3, and the player met Coastal Rentals the same day. On day 4 it asked SMSgt Dunn, filled in a BPA call with Harbor Equipment and then ended the day without placing it; Dunn's answer that night said the rental needs drivers, and Harbor's shortage was in the news. On day 5 it added the operators and wrote a fast-and-good purchase order with Coastal Rentals, written by Avery, which started on day 8. The staff meeting gave it the waste request on day 6; it asked Capt Ruiz on day 7, ordered Main Island Sanitation on day 8, and that started on day 13. The camp vendor's lodging failed on days 4 and 6 (50%, both unforeseeable); it issued a cure notice on day 7. The dining hall's slips on days 11 and 12 were covered by stock; it issued a second cure notice on day 13. Days 14 to 16 were End the day.

It never made a buy it later learned was wrong, so Stop this work (DESK-105) was not exercised. The brief is fixed and does not steer the player; the stop waits for a play that needs it, or Nick's.

## The first two minutes

The preparation page read as a list of good things to tick: all five fit in 9 of 14 days, as on 17 and 18 Nov (D32 accepts it). Each choice's effect line sits as far from its own box as from the next one, so the player read some of them as belonging to the box below ([picture](plays/2026-11-19/prep-effect-lines.png)). The turnover's four pages were read in full; the pass lines were found on the last page and again as the commander's bar on Today, which the player called the screen that works. Mills' first page promises "a note each morning"; the first one arrives in the end-of-day report on day 1.

On day 1 the water request's order-by line ("Order today: a card buy from Island Mart starts on day 2 or 3, with no written terms; it is wanted from day 2. The water stock on hand should cover day 2.") told the player what to do, and it did it. Its words: "the tutorial largely plays itself" on day 1.

## Its notes, sorted

The sort follows section 5: defect, design, known, model. A row named here is new in this PR unless it says otherwise.

| # | Day, screen | What the player said (shortened) | Sort | Row |
| --- | --- | --- | --- | --- |
| 1 | Prep | All five fit in 9 of 14 days; "pick what will help most" is not a choice | Known: D32 | none |
| 2 | Prep | Three horizons: day 3 not 25, 16 days here, O&M good through day 90 | Model: each is true; the 16 days are on the turnover's last page and on the bar | none |
| 3 | Prep | Each effect line reads as if it belongs to the next box ([picture](plays/2026-11-19/prep-effect-lines.png)) | Defect, game: the effect line sits midway between its box and the next | DESK-113 |
| 4 | Prep | "Your team · choose names and faces" looks as important as the real choices | Model, mostly: a full-width button among checkboxes; noted for Nick's eye | none |
| 5 | Turnover | Pass lines appear only on the last page, inside the money paragraph | Known: D28, the bar on Today | none |
| 6 | Turnover | Page 2 names only the camp BPA; Harbor's comes a page later; whose card, what limit | Model: the next page says it; the card limit is in the refusal when a buy exceeds it | none |
| 7 | Day 1 | Mills promised a note each morning; there was none on Today; it was "The day's news" that night | Defect, scenario text: the greeting says "each morning" and the note comes with the day's results | DESK-112 |
| 8 | Day 1, request | The order-by line tells me what to do, so the tutorial plays itself | Known: D35 made it advice on purpose; the second and third requests are where the player has to think | none |
| 9 | Day 1, request | Ask Ortiz answered at day's end, after the buy | Known: D25 | none |
| 10 | Day 1, request | Left the request page, came back, and the buy form had lost my picks | Defect, game: the form resets on leaving the page the same day | DESK-113 |
| 11 | Day 1, Vendors | The vendor picker is labelled "Whose card?", read as the purchase card | Defect, label | DESK-113 |
| 12 | Day 1 | Never learned what "walk-in small buys" do | Model: a background line; no decision rides on it in the tutorial | none |
| 13 | Day 3, end of day | "Back to Today" moved to the next day; I thought I had skipped one | Defect, label: the button after the day's results goes to the next morning and says "Back" | DESK-113 |
| 14 | Day 3 | Meet Coastal Rentals said their terms were already known; the meeting still cost 2 hours | Known: the line said so (DESK-108); the player met them anyway | none |
| 15 | Day 5, request | The quote warns "Without operators, it fails more often" and the fix, "Add what you learned", is in the other column below the fold; about 28 steps to find it | Design, the most important, and the player's one change. The request column has the part, Dunn's words and the button (D36); the buy column, where the player's eyes are while choosing, has only the warning | D39; DESK-113 |
| 16 | Day 5, request | The order-by line still said "a BPA call with Harbor Equipment Co" after Dunn said drivers and after the news that Harbor is short of forklifts ([picture](plays/2026-11-19/day5-order-today-harbor.png)) | Defect in the Sim view, the second most important. `OrderBy` reads only the parts on the request, not those someone has said it needs, and does not read the news; Harbor's call brings no drivers and Harbor turns down every call that week | D39; DESK-111 |
| 17 | Day 5, Vendors | Harbor's card says "Uncommitted: 5" after the shortage news | Design: the card shows what the shop has committed, not what the shop has heard | D39; DESK-111, DESK-114 |
| 18 | Day 5, request | After the operators went in, the line named a card buy, which the quote warned has no written terms on a 13-day service | Known: D35 as built; no written route made day 8 on the slow roll at the player's own pace, and the line says "with no written terms". The player chose the purchase order, and Avery's pace made day 8 | none |
| 19 | Day 5, editor | Pick-two clicks flaky; one step said the driver did not answer though the click took effect | Driver, once; no row unless it recurs | none |
| 20 | Day 7, Today | The lodging decision was at the foot of a long Inbox with no button to it; on day 13 the dining decision had "Open vendor decision" ([picture](plays/2026-11-19/day7-alert-no-decision.png)) | Defect, game: the morning alert says "its performance issue is in your inbox for a decision" and its only button opens the site | DESK-113 |
| 21 | Day 7 | The issue lists "terminate"; terminate is on the contract page | Known: DESK-108 | none |
| 22 | Day 8, editor | Naming Pratt on the waste order quietly lowered his watch on the forklift order from about 80% to 65% | Defect in the Sim view: the COR line gives his odds on this contract and does not say they fall on the one he already watches (D8 lowers both) | DESK-112 |
| 23 | Day 9 | Lane 40% on forklifts, 85% on lodging | Model: D8, the COR's own unit | none |
| 24 | Day 9 | Payments and COR details at the bottom of a long scroll | Model: nothing to decide there in the tutorial | none |
| 25 | Day 11 to 13 | The lodging cure period ends on day 17, after the tutorial | Known: DESK-91 says so on the debrief | none |
| 26 | Days 14 to 16 | Nothing to decide | Known: D31 | none |
| 27 | Debrief | "Links each outcome to my decision"; the best screen in the game ([picture](plays/2026-11-19/debrief.png)) | Praise, earned: D38 working | none |
| 28 | Debrief | "Lodging failed... with full operator and maintenance coverage" reads like forklift wording | Defect in the Sim view: the sentence names parts lodging does not have | DESK-112 |
| 29 | Debrief | Both lodging gaps unforeseeable, so the cure notice felt pointless; the dining misses and the second notice go unmentioned | Model, mostly: a cure notice is about the next failure, and the dining misses were covered by stock and are not on the bar (D38 leaves out lines not on the bar) | none |
| 30 | Debrief | No grade, no "tutorial complete" | Known: D28 and D38 leave grades out; every line reads "Met" | none |

## Its four answers, in its words

- **Who were you?** "The contracting officer for a 300-person hub on the main island. I had two people: MSgt Avery (experienced and fast) and SrA Novak (new). I had $15k cash, a purchase card and $576k of O&M funds."
- **What were you trying to do?** "Keep the commander's four goals met for 16 days while the host nation pulled its support."
- **Did you manage it?** "Yes. The only shortfalls were two lodging dips from the camp vendor, which the debrief calls unforeseeable, plus food-service misses on days 11 and 12 that the debrief doesn't mention."
- **One change?** "When a quote warns about a gap ('Without operators, it fails more often...'), put the fix right there in the warning. The fix is the 'Add what you learned' button, which sits below the fold in the other column of the request page. Asking the requester and folding the answer into the request is the tutorial's core lesson. I spent about 20 steps trying every arrangement and vendor before I found that button. Second place: the 'Order today' advice line kept recommending options the game had already told me were bad."

## Fable's reading

The request page and the D38 debrief both did their jobs: the player was not lost, never fought the layout, and called the debrief the best screen in the game. What remains is that the game's advice did not take in what the player had just been told.

1. On day 5 the order-by line, the first line a player reads about a request and the one D35 made trustworthy, named a BPA call with Harbor Equipment after Dunn said the rental needs drivers (Harbor's call brings none) and after the news that Harbor is short of forklifts that week (it turns down every call). A player who followed it would have bought the wrong thing from a vendor who would refuse. The 18 Nov player did exactly that on day 4, before the answer was in; this player read the news and held back. D39, DESK-111.
2. The tutorial's lesson is to ask and fold the answer into the request. The answer is on the left with its button; the warning about it is on the right, under the buy the player is building, and does not say what to do. D33 already says a warning carries its action. D39 puts the same button under the quote's warning when someone has said the part is needed. DESK-113.
3. Small text: the debrief's coverage sentence on a capability with no operator or maintenance parts, the COR line that does not say naming him lowers his other watch, and Mills' "each morning". DESK-112.

## Sim pass

`batch tutorial --seeds 200`, medians (min to max). Unchanged from 18 Nov in every column: nothing since then moved a draw (DESK-102 to DESK-110 are read only, and no policy stops work).

| Policy | Mission output | Forklifts down days | Waste down days | Water down days | Food down days | Confusion days |
| --- | --- | --- | --- | --- | --- | --- |
| do-nothing | 0.796 (0.778 to 0.796) | 9 | 4 | 6 (6 to 7) | 6 (6 to 8) | 0 |
| greedy | 0.975 (0.943 to 1) | 1 (0 to 2) | 0 (0 to 1) | 0 (0 to 1) | 0 (0 to 3) | 1 (0 to 6) |
| textbook | 0.997 (0.973 to 1) | 0 | 0 | 0 (0 to 1) | 0 (0 to 2) | 1 (0 to 4) |

- Textbook meets every pass line in every seed and beats greedy on mission output.
- No policy reads the order-by line, so note 16 moves no number; a player who obeys it on day 5 calls Harbor without drivers, in the week Harbor turns down every call, and has to buy again.
- The player's 68 shop hours against textbook's 103 to 118, as on 18 Nov: not a finding.
