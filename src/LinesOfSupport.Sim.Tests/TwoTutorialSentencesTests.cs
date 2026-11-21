using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-160 (Claude play 21 Nov, fourth play, notes 3 and 19).</summary>
public class TwoTutorialSentencesTests
{
    [Fact]
    public void A_the_vendor_list_says_a_vehicle_for_the_office()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, ["gpcAccounts", "cashAndPayingAgent", "systemAccess"]);
        var vendors = s.View.Turnover().Sections.Single(x => x.Kind == TurnoverSectionKind.Vendors);
        Assert.Contains("Coastal Rentals for material handling equipment and a vehicle for the contracting office", vendors.Text);
    }

    [Fact]
    public void B_a_buy_that_ran_its_course_names_its_last_day()
    {
        var log = InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-21-4", "input-log.json"));
        var s = Replay.Run(Repo.Load("tutorial"), log, stopAfterDay: 9);
        s.AdvanceDay();
        Assert.Contains("Water at Hub from Island Mart ended: its last day was day 9.", s.View.DaySummary()!.Ended);
    }
}
