using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-211 (D55; Claude play 22 Nov, second play, notes P1-11, P1-14, P1-15, P2-15). Both players rejected
/// "Written terms settle a dispute: a purchase order or a BPA call" on water, where the day-1 purchase order was quoted to
/// start day 4 to 6 and there was no water BPA; one saw the day-5 forklift lead push a card buy while a written order kept
/// the commander's line; and food service read as a second mistake though it went down with water. The quote, the lead
/// and the debrief now agree on what the written route would have cost.</summary>
public class WrittenRouteTests
{
    private static InputLog Log(string dir, int n)
        => InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", dir, $"input-log-{n}.json"));

    [Fact]
    public void A_confusion_gap_says_what_the_written_route_would_have_cost_that_day()
    {
        var d = Replay.Run(Repo.Load("tutorial"), Log("2026-11-22-2", 1)).View.Debrief();
        var water = d.Gaps.Single(g => g.CapabilityId == "water" && g.StartDay == 3);
        // (a) The purchase order the award form quoted on day 1 could not have started by day 3: the card was the way.
        Assert.EndsWith("from day 3 the vendor and the unit disagreed on what was owed. A purchase order with Island Mart placed on day 1 "
                        + "would have started day 4 to 6, with water at Hub out up to 3 days in all, 2 more than the commander allows; "
                        + "the card was the way to have water at Hub on day 3.", Assert.Single(water.Factors).Text);
        Assert.DoesNotContain("Written terms settle a dispute", water.Factors[0].Text, StringComparison.Ordinal);
        Assert.Equal(DebriefWords.NoTermsTagLine, water.TagLine);

        // (c) Food service went down with water: its button says so, not that it was in front of you.
        var food = d.Gaps.Single(g => g.CapabilityId == "food" && g.StartDay == 3);
        Assert.Equal(GapTag.Avoidable, food.Tag);
        Assert.Equal("Avoidable: it went down with water at Hub.", food.TagLine);
        Assert.Contains("Food service at Hub went down with it.", d.Summary!.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void A_written_route_that_would_have_kept_the_line_is_named_as_the_cost_of_writing_it()
    {
        // The first 22 Nov play bought forklifts on the card on day 5; a purchase order with Coastal that day would have
        // started day 7 to 9 and kept the commander's line, so the dispute was the price of the faster buy.
        var d = Replay.Run(Repo.Load("tutorial"), Log("2026-11-22", 1)).View.Debrief();
        foreach (var g in d.Gaps.Where(g => g.CapabilityId == "mhe"))
            Assert.EndsWith("Written terms settle a dispute, at a cost in days: a purchase order with Coastal Rentals placed on day 5 would have "
                            + "started day 7 to 9, with material handling equipment at Hub out up to 1 day in all, all the commander allows.",
                            Assert.Single(g.Factors).Text);
    }

    [Fact]
    public void The_lead_names_the_written_order_when_its_worst_start_keeps_the_line()
    {
        // (b) Morning of day 5 in the second 22 Nov play: forklifts wanted day 8. The card buy from Coastal is faster but has
        // no written terms on a long service; the purchase order with Coastal starts by day 9 at worst, one day out in all,
        // all the commander allows, so the line names it.
        var s = Replay.Run(Repo.Load("tutorial"), Log("2026-11-22-2", 1), stopAfterDay: 4);
        var ob = s.View.WhereThingsStand().Single(r => r.CardId == "tutForklift").OrderBy!;
        Assert.Equal("purchaseOrder", ob.InstrumentId);
        Assert.Equal("Order today: a purchase order with Coastal Rentals starts between day 7 and day 9; it is wanted from day 8. "
                     + "If it starts on day 9, material handling equipment at Hub is out 1 day in all: all the commander allows.", ob.Sentence);

        // Water on day 1: a purchase order would put water out three days against one allowed, so the card stays.
        var first = Replay.Run(Repo.Load("tutorial"), Log("2026-11-22-2", 1) with { Actions = [] }, stopAfterDay: 0);
        Assert.Equal("gpc", first.View.WhereThingsStand().Single(r => r.CardId == "tutWater").OrderBy!.InstrumentId);
    }
}
