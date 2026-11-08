**Work session handoff**

Expected look of this build and why: no window. Read `scenarios/spoke-b-90/golden/*.expected.txt` for the 90-day story.

## PRs reviewed this session
None new from Codex (PRs 15 and 16 open; Codex's own merge rule applies).

## Changed
- `scenarios/spoke-b-90/`: the 90-day Spoke B with late new-year funding (om1 expires day 75, om2 arrives day 78). Approach logs 1 to 4 plus 3b (UCA never definitized) and golden reports; policy goldens at seed 1.
- Sim: termination deobligates the unperformed portion (back to a current line, lapsed otherwise); definitization obligates the full definitized price while money is current; vendor capacity counts only overlapping periods; an incumbent continuing on site does not mobilize again; clearer unpaid-UCA reasons (cap vs expired line vs line empty).
- Textbook policy: plans follow-ons before arrangements run out (incumbent, day after end, money current at award), and tracks its bridges explicitly.
- Tests: 85 Sim (golden across both scenarios, balance on both, three new mechanic tests). Harness 63 unchanged.

## Why
M1-02. The 90-day run exposed four real mechanics the 30-day cut hid; each is fixed in the engine rather than worked around in the scenario.

## Validated how
200 seeds each. spoke-b: textbook median 0.936, 187/200 pass; greedy 0.806, 0/200; do-nothing 0.666. spoke-b-90: textbook 0.926, 187/200; greedy 0.745, 0/200; do-nothing 0.637. The 30-day goldens changed only in the deobligation and definitization lines and the obligated totals that follow.
Approach 3b (90 days): definitization due at 50% of NTE, HHQ inquiry, 75% cap reached, stop-work day 64, Spoke B dark to day 90.

## Unresolved
The 13/200 textbook failures on each scenario remain the intended "right decision still bites" seeds; worth a look in playtest.

## Next
Fable: M0-07 (view models for the morning board and editors), then M1-04 (unauthorized commitments per SPEC 4.5).
