using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>
/// DESK-102 (Claude play 18 Nov 2026, note 24): a quote does not count support its request replaces. The commander tasks
/// the tutorial's waste removal at the day 6 staff meeting because the host nation is taking its honey trucks back; the
/// withdrawal is on the record only on day 8. On day 7 the buy summary said the buy was "on top of Main Island Utilities
/// (host nation). Every day it would run is already covered." in amber, and Where things stand read the request as
/// running. Host nation support already running before a request is wanted counts only through the day before it.
/// </summary>
public class ReplacedSupportTests
{
    private static readonly string[] Prep = ["gpcAccounts", "cashAndPayingAgent", "systemAccess", "vendorLists"];

    private static AwardAction Waste(string id) => new()
    {
        Id = id, Instrument = "purchaseOrder", Vendor = "sanitation", Card = "tutWaste", Quantity = 3600, StartDay = 13,
        DurationDays = 8, FundingLine = "om1", Priorities = ["fast", "good"],
    };

    /// <summary>The shop in the staff meeting from day 1, at the morning of day 7: the waste request is on the desk and
    /// the host nation still has its honey trucks on the record.</summary>
    private static Session Day7()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        Assert.True(s.Apply(new JoinStaffMeetingAction()).Accepted);
        while (s.View.Day < 7) s.AdvanceDay();
        return s;
    }

    [Fact]
    public void The_waste_buy_is_not_on_top_of_the_trucks_it_replaces()
    {
        var s = Day7();
        var buy = Waste("w") with { Day = s.View.Day };
        var preview = s.View.Preview(buy);
        Assert.True(preview.Accepted, string.Join(" ", preview.HardStops));
        Assert.DoesNotContain(preview.SoftGaps, g => g.Contains("Main Island Utilities", StringComparison.Ordinal));
        var summary = s.View.AwardSummary(buy)!;
        Assert.DoesNotContain("on top of", summary.Sentence, StringComparison.Ordinal);
        Assert.False(summary.Attention);
        Assert.Null(summary.Why);
    }

    [Fact]
    public void Where_things_stand_reads_the_waste_request_as_not_yet_bought()
    {
        var s = Day7();
        var waste = Assert.Single(s.View.WhereThingsStand(), e => e.CardId == "tutWaste");
        Assert.Equal(RequestStanding.Nothing, waste.State);
        Assert.DoesNotContain("is delivering", waste.Sentence, StringComparison.Ordinal);
        Assert.NotNull(waste.OrderBy);
    }

    [Fact]
    public void A_second_waste_buy_is_still_on_top_of_the_first()
    {
        var s = Day7();
        Assert.True(s.Apply(Waste("first") with { Day = s.View.Day }).Accepted);
        var again = Waste("second") with { Day = s.View.Day, Vendor = "sanitation" };
        var summary = s.View.AwardSummary(again);
        // The first buy is still being written, so the second is on top of it, whatever the host nation does.
        Assert.Contains(s.View.Preview(again).SoftGaps, g => g.StartsWith("You are already writing the written purchase order with Main Island Sanitation", StringComparison.Ordinal));
        if (summary is not null) Assert.Contains("It is on top of the written purchase order being written with Main Island Sanitation.", summary.Sentence);
    }

    [Fact]
    public void Host_nation_support_still_to_start_counts_in_full()
    {
        // The water point opens on day 9, inside the water request's days if a buy runs past day 8: still on top of it.
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        var water = new AwardAction
        {
            Id = "w", Instrument = "sf44", Vendor = "harborBottling", Card = "tutWater", Quantity = 4500, StartDay = 2,
            DurationDays = 9, FundingLine = "om1", Day = 1,
        };
        Assert.Contains(s.View.Preview(water).SoftGaps, g => g.StartsWith("Main Island Utilities (host nation) is already set to supply water", StringComparison.Ordinal));
    }
}
