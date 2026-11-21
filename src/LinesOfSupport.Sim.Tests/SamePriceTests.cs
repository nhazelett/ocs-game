using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-202 (Claude play 21 Nov, twelfth play, notes P1-14, P2-10). Both players added operators to the forklift
/// request on day 5 and saw Coastal's card quote stay at $8,580 with its warning gone; nothing said Coastal brings operators
/// at that price. The leaves-out sentence now says so, and once added the summary names what the buy brings.</summary>
public class SamePriceTests
{
    private static InputLog SecondLog()
        => InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-21-12", "input-log-2.json"));

    private static AwardAction Buy(InputLog log) => log.Actions.OfType<AwardAction>().Single(a => a.Day == 5);

    [Fact]
    public void Before_adding_the_quote_says_coastal_brings_operators_at_the_same_price()
    {
        var log = SecondLog();
        var s = Replay.Run(Repo.Load("tutorial"), log, stopAfterDay: 4);
        Assert.Equal(5, s.Day);
        var summary = s.View.AwardSummary(Buy(log))!;
        Assert.Equal("Coastal Rentals, material handling equipment for Hub: $8,580 and no shop hours. It starts on day 8; it is wanted from day 8. "
            + "It leaves out operators, which SMSgt Ray Dunn said the request needs: add what you learned first; Coastal Rentals brings them at the same price.",
            summary.Sentence);
    }

    [Fact]
    public void Once_added_the_summary_names_operators_beside_the_same_price()
    {
        var log = SecondLog();
        var s = Replay.Run(Repo.Load("tutorial"), log, stopAfterDay: 4);
        Assert.True(s.Apply(log.Actions.OfType<AddLineAction>().Single(a => a.Day == 5)).Accepted);
        var summary = s.View.AwardSummary(Buy(log))!;
        Assert.StartsWith("Coastal Rentals, material handling equipment for Hub with operators: $8,580 and no shop hours.", summary.Sentence);
    }

    [Fact]
    public void A_vendor_that_does_not_offer_the_part_is_unchanged()
    {
        var log = SecondLog();
        var s = Replay.Run(Repo.Load("tutorial"), log, stopAfterDay: 4);
        var harbor = Buy(log) with { Instrument = "bpaCall", Vendor = "harborEquipment", Vehicle = "equipBpa", FundingLine = "om1" };
        var summary = s.View.AwardSummary(harbor);
        if (summary is not null) Assert.DoesNotContain("same price", summary.Sentence);
    }
}
