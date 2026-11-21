using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-190 (D51; Claude play 21 Nov, tenth play, notes P1-4, P2-3). Three times a game (water on day 1, forklifts
/// on day 4, waste on day 7) the player chose between ordering blind and waiting a night for the requester's answer, and the
/// buy summary said only that the answer "may change what this buy needs". It now says what the same buy placed after the
/// answer would get.</summary>
public class NightsWaitTests
{
    private static InputLog FirstLog()
        => InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-21-10", "input-log-1.json"));

    private static Session At(int day)
    {
        var log = FirstLog();
        var s = Replay.Run(Repo.Load("tutorial"), log, stopAfterDay: day - 1);
        // The day's own asks, as the player gave them before buying.
        foreach (var act in log.Actions.Where(x => x.Day == day && x is AskAboutAction)) Assert.True(s.Apply(act).Accepted);
        return s;
    }

    [Fact]
    public void Day_1_water_waited_for_costs_a_day_out()
    {
        var s = At(1);
        var buy = new AwardAction { Id = "w", Instrument = "gpc", Vendor = "islandMart", Card = "tutWater", Site = "hubSite",
                                    Capability = "water", Quantity = 4500, StartDay = 2, DurationDays = 7, Day = 1 };
        var summary = s.View.AwardSummary(buy)!;
        Assert.Null(summary.PassLineCost);   // ordered today, nothing goes out
        Assert.EndsWith("may change what this buy needs. Ordered tomorrow, after the answer, it starts on day 3 or 4: water at Hub is out 1 day in all: all the commander allows.", summary.Sentence);
    }

    [Fact]
    public void Day_4_forklifts_by_purchase_order_start_a_day_late()
    {
        var s = At(4);
        var buy = new AwardAction { Id = "f", Instrument = "purchaseOrder", Vendor = "coastalRentals", Card = "tutForklift", Site = "hubSite",
                                    Capability = "mhe", Quantity = 3, DurationDays = 13, FundingLine = "om1", Assignee = "avery", Day = 4 };
        var summary = s.View.AwardSummary(buy)!;
        Assert.EndsWith(" Ordered tomorrow, after the answer, it starts between day 7 and day 9: material handling equipment at Hub is out 1 day in all: all the commander allows.", summary.AnswerDue);
        // A card buy from tomorrow still starts before the day wanted: the start, and no count.
        var card = buy with { Instrument = "gpc", FundingLine = null, Assignee = null };
        Assert.EndsWith(" Ordered tomorrow, after the answer, it starts on day 6 or 7.", s.View.AwardSummary(card)!.AnswerDue);
    }

    [Fact]
    public void Day_7_waste_wanted_on_day_13_costs_nothing()
    {
        var s = At(7);
        var buy = new AwardAction { Id = "x", Instrument = "purchaseOrder", Vendor = "sanitation", Card = "tutWaste", Site = "hubSite",
                                    Capability = "waste", Quantity = 3600, StartDay = 13, DurationDays = 8, FundingLine = "om1", Assignee = "avery", Day = 7 };
        var summary = s.View.AwardSummary(buy)!;
        // Starting on day 13 either way: waiting costs nothing, so nothing is said.
        Assert.DoesNotContain("Ordered tomorrow", summary.AnswerDue);
        var asap = buy with { StartDay = null };
        Assert.EndsWith(" Ordered tomorrow, after the answer, it starts between day 11 and day 13.", s.View.AwardSummary(asap)!.AnswerDue);
    }
}
