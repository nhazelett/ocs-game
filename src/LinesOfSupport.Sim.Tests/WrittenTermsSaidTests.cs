using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-194 (D52; Claude play 21 Nov, eleventh play, notes P2-13, P2-20). The second player bought the forklifts
/// on the card for thirteen days on day 5; the pinned summary said nothing about written terms, the warning sat in the
/// quote's notes below the fold, and the debrief graded day 16's dispute avoidable. The summary now says it when nothing
/// ahead of it takes its place, and says it once.</summary>
public class WrittenTermsSaidTests
{
    private static InputLog SecondLog()
        => InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-21-11", "input-log-2.json"));

    private static AwardAction Coastal(string instrument) => new()
    {
        Id = "f", Instrument = instrument, Vendor = "coastalRentals", Card = "tutForklift", Site = "hubSite",
        Capability = "mhe", Quantity = 3, StartDay = 8, DurationDays = 13, Day = 5,
    };

    [Fact]
    public void The_day_5_card_buy_says_nothing_is_written_down()
    {
        var log = SecondLog();
        var s = Replay.Run(Repo.Load("tutorial"), log, stopAfterDay: 4);
        foreach (var act in log.Actions.Where(x => x.Day == 5 && x is AddLineAction)) Assert.True(s.Apply(act).Accepted);
        var buy = Coastal("gpc");
        var summary = s.View.AwardSummary(buy)!;
        Assert.EndsWith("It starts on day 8; it is wanted from day 8. No written terms on a 13-day service: on a day the vendor and the unit disagree on how much or when, nothing on paper settles it and only half of that day's supply arrives.", summary.Sentence);
        Assert.False(summary.Attention);
        // Said once: not again in the quote's notes.
        Assert.DoesNotContain(s.View.Preview(buy).SoftGaps, g => g.StartsWith("No written terms", StringComparison.Ordinal));
    }

    [Fact]
    public void A_written_order_says_nothing_of_it()
    {
        var log = SecondLog();
        var s = Replay.Run(Repo.Load("tutorial"), log, stopAfterDay: 4);
        var summary = s.View.AwardSummary(Coastal("purchaseOrder") with { FundingLine = "om1" })!;
        Assert.DoesNotContain("No written terms", summary.Sentence);
    }

    [Fact]
    public void Behind_the_answer_due_it_stays_in_the_notes()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1);
        AwardAction water = new() { Id = "w", Instrument = "gpc", Vendor = "islandMart", Card = "tutWater", Quantity = 4500, StartDay = 2, DurationDays = 7 };
        Assert.True(s.Apply(new AskAboutAction { Card = "tutWater", Person = "ortiz" }).Accepted);
        var summary = s.View.AwardSummary(water)!;
        Assert.DoesNotContain("No written terms", summary.Sentence);
        Assert.Single(s.View.Preview(water).SoftGaps, g => g.StartsWith("No written terms on a 7-day service", StringComparison.Ordinal));
    }
}
