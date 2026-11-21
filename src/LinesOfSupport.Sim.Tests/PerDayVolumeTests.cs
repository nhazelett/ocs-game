using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-126 (Claude play 20 Nov, second play, note 5): a continuous service measured in a volume reads per day,
/// as a consumable does; a count of things does not.</summary>
public class PerDayVolumeTests
{
    private static readonly string[] Prep = ["gpcAccounts", "cashAndPayingAgent", "systemAccess"];

    [Fact]
    public void Waste_removal_reads_gallons_a_day_and_forklifts_stay_a_count()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        while (s.Day < 9) s.AdvanceDay();
        Assert.True(s.Apply(new AwardAction
        {
            Id = "waste", Instrument = "purchaseOrder", Vendor = "sanitation", Card = "tutWaste", Quantity = 3600, StartDay = 13,
            DurationDays = 8, FundingLine = "om1",
        }).Accepted);
        Assert.Equal("Day 9: Waste removal at Hub from Main Island Sanitation, 3,600 gal/day for 8 days on a purchase order.",
            s.View.Decisions().First(d => d.Sentence.Contains("Main Island Sanitation")).Sentence.Replace(", given to you", ""));

        Assert.True(s.Apply(new AwardAction
        {
            Id = "fork", Instrument = "purchaseOrder", Vendor = "coastalRentals", Card = "tutForklift", Quantity = 3, StartDay = 13,
            DurationDays = 4, FundingLine = "om1",
        }).Accepted);
        Assert.Contains("3 forklifts for 4 days", s.View.Decisions().First(d => d.Sentence.Contains("Coastal Rentals")).Sentence);
    }
}
