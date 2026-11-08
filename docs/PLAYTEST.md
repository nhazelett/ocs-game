# M2 playtest (backlog M2-11)

For Nick, and for anyone Nick hands the build to. SPEC 11 M2 is done when four things are true and one decision is made; this page is the script for checking them.

## Get the build

1. On GitHub, open **Actions → CI → Run workflow**, choose **main**, and run it. After the checks pass, `export-windows` creates the build. Other branches do not export through this button.
   - Tag route: Releases → Draft a new release, choose a new `v...` tag on `main`, and publish. The tag also runs CI; its verified zip is attached to that release. A tag pushed without a release gets a release when the zip is ready.
2. When the run finishes, download its artifact `LinesOfSupport-<short-commit>-windows-x86_64` (button route) or `LinesOfSupport-<tag>-windows-x86_64` (tag route). Tag builds also have a directly downloadable zip under **Releases → Assets**. Unzip until `LinesOfSupport.exe` and its sibling `scenarios` folder are together, then run the exe. No install or setup; development `_stub` is excluded. Export follows all four CI checks and can take several minutes.
3. If Windows SmartScreen warns about an unknown publisher, "More info", "Run anyway". The build is not code-signed yet.

## The four checks

| Check | How | Pass |
| --- | --- | --- |
| Play Spoke B start to finish without touching JSON | Prep screen, then days 1 to 30 from the desk, then the debrief | No file edited, no console used |
| A new player with no briefing finishes in under 45 minutes | Hand the build to someone who has not seen it; say only "you are the contracting officer; get the sites supported"; time it | Under 45 minutes to the debrief |
| The gap view tags the four seeded gaps | Play approach 1 by hand (generators only, no fuel line, no survey): see below | Tags match the table below |
| One-sentence lesson matches an intended outcome | After the debrief, before reading this page's list, write one sentence: what did the game teach you? | It matches one of the intended outcomes below |

### Seeded gaps (approach 1 by hand)

On day 1, award generators for requirement 0007 (BPA call to Harbor Equipment Co, generation line only), do not add the fuel line, do not survey, and leave the rest to the defaults. The debrief should show:

| Gap | Tag | Why the game says so |
| --- | --- | --- |
| Spoke B fuel and power, about day 10 | Avoidable | The generators were bought without the fuel supply line; the requirement's dependency checklist showed fuel uncovered from day 1 |
| Spoke B fuel, about day 15 to 19 | Discoverable | Fuel comes from Main Island and waits on the barge; a survey of Second Island would have shown that |
| Spoke B fuel, after a fuel coordination is withdrawn (approach 2 instead) | Avoidable | A coordination can be withdrawn at any time; it is not a contract |
| Spoke B power, from day 20 | Avoidable | Six generators against the population that arrives on day 20, which was in the plan from day 1 |

Exact days move a little by seed. The tags should not.

## Intended outcomes (read after writing your sentence)

Any of these counts as a match:

1. A requirement is not a scope: what the customer wrote down is rarely everything the capability needs to run (fuel, operators, maintenance).
2. Information has a price and a payoff: a survey costs shop hours now and prevents a gap later.
3. A coordination or a handshake is not a contract: it can disappear, and the gap it leaves is yours.
4. Speed is bought with debt: a UCA gets the vendor moving and leaves definitization and obligation work that comes due.
5. Plans change on dates you can see: population growth and funding expiry were in the plan on day 1.
6. The same decision can be right and still bite: vendors fail, links close, and the debrief separates bad luck from bad calls.

## The gate decision

After playing: does building a contract action feel like deciding, or like data entry?

- Deciding: M2 closes, M3 starts (SPEC 11).
- Data entry: say where it dragged (which panel, which choice felt like typing). The editors get redesigned before anything else.

Tell Fable in chat. Your answer goes in `backlog/FOR-NICK.md` and the milestone review (docs/REVIEW.md).
