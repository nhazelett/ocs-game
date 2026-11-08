**Work session handoff**

Expected look of this build and why: new capability tiles (lodging, food, waste) on the board; Spoke C gets a field-feeding requirement card.

## PRs reviewed this session
- None open.

## Changed
- spoke-b: lodging, food, waste added. Host nation (Main Island Utilities) provides lodging and waste at all three sites and food at the hub; Spoke B has lodging and waste only (no food: MREs not modeled); Spoke C starts with 10 days of food and card req0014 (field feeding from day 12; food depends on water). Vendor G renamed Coastal Provisions and offers food (service only); Water Works offers food (service and operator). Still 8 vendors on 3 islands.
- Engine: inherited host-nation arrangements bill nothing.
- Textbook policy: the enduring turnkey vendor must be buyable (not vehicle-only) and have capacity. It had been picking the host nation at $0 with no capacity left and never awarding.
- Approach logs: competent Spoke C food on day 3 (operator line on, Water Works PO), like M2-13 did for water, so Spoke C does not confound the Spoke B stories.
- Goldens regenerated; assumptions (3 rows); backlog M2-17 Done.

## Validated how
- Approach stories: Spoke B day 10 (avoidable), day 15-19 (discoverable), coordination withdrawal (avoidable) and approach 3 with no gaps all hold; Spoke B lodging now follows each power gap. Approach 4 shows the new food-on-water dependency on a one-day Spoke C water failure.
- 200 seeds: textbook median 0.911 (177 pass), greedy 0.721, do-nothing 0.607; endurance medians 0.994, 0.735, 0.507. spoke-b-90 unchanged.
- 136 Sim, 94 Harness tests.

## Next
- Fable: M2-18 (vendor failure and cure notice).
