using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-07 (docs/design/desk-redesign.md D8, D9): a COR from the unit that knows the work catches more, a COR
/// already watching other contracts catches less, and the COR list says why. The award editor carries what was asked.</summary>
public class CorFitTests
{
    private static readonly string[] Prep = ["gpcAccounts", "cashAndPayingAgent", "systemAccess"];

    [Fact]
    public void For_the_forklift_the_engineer_beats_the_better_rated_cor_and_the_list_says_why()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        var choices = s.View.CorChoices("mhe");
        Assert.Equal(["corPratt", "corLane"], choices.Select(c => c.Id));
        var pratt = choices[0];
        Assert.True(pratt.Fits);
        Assert.Equal(800, pratt.ReportPermille);
        Assert.Equal("TSgt Pratt (3 CES) works with material handling equipment every day and watches no other contract: catches about 80% of problems, reported 2 days late.", pratt.Why);
        var lane = choices[1];
        Assert.False(lane.Fits);
        Assert.Equal(2, lane.Watching);                     // billeting and dining, inherited
        Assert.Equal(1000 - 300 - 2 * 150, lane.ReportPermille);
        Assert.Equal("SSgt Lane (3 FSS) does not work with material handling equipment and already watches 2 other contracts: catches about 40% of problems, reported the same day.", lane.Why);
    }

    [Fact]
    public void Each_contract_a_cor_takes_on_stretches_them_further()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        Assert.Equal(800, s.View.CorChoices("power").Single(c => c.Id == "corPratt").ReportPermille);
        foreach (var id in new[] { "hubBilleting", "hubDining" })
            Assert.True(s.Apply(new DesignateCorAction { Arrangement = id, Cor = "corPratt" }).Accepted);
        var pratt = s.View.CorChoices("power").Single(c => c.Id == "corPratt");
        Assert.Equal(2, pratt.Watching);
        Assert.Equal(800 - 2 * 150, pratt.ReportPermille);
        // Lane gave both up, so Lane is no longer stretched.
        Assert.Equal(1000, s.View.CorChoices("food").Single(c => c.Id == "corLane").ReportPermille);
    }

    [Fact]
    public void Off_the_work_and_stretched_both_count()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        foreach (var id in new[] { "hubBilleting", "hubDining" })
            Assert.True(s.Apply(new DesignateCorAction { Arrangement = id, Cor = "corPratt" }).Accepted);
        // Fair (800), not their work (-300), two other contracts (-300); the floor (100) is not reached.
        Assert.Equal(200, s.View.CorChoices("food").Single(c => c.Id == "corPratt").ReportPermille);
    }

    [Fact]
    public void Cors_without_knows_keep_their_quality_odds()
    {
        var s = Session.Create(Repo.Load("spoke-b-90"), 1);
        var choices = s.View.CorChoices("power");
        Assert.All(choices, c => Assert.Null(c.Fits));
        Assert.Equal(1000, choices.Single(c => c.Id == "corReyes").ReportPermille);
        Assert.Equal(800, choices.Single(c => c.Id == "corOkafor").ReportPermille);
    }

    [Fact]
    public void The_award_editor_carries_what_the_customer_asked_for()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        var e = s.View.ActionEditor("hubSite", "water", "tutWater")!;
        Assert.Equal(2, e.RequestedStartDay);
        Assert.Equal(7, e.RequestedDurationDays);
        Assert.Equal("Bottled water, 4,500 gal a day", e.RequestedQuantity);
        var none = s.View.ActionEditor("hubSite", "water")!;
        Assert.Null(none.RequestedStartDay);
        Assert.Null(none.RequestedDurationDays);
    }

    [Fact]
    public void Reading_the_choices_changes_nothing()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        var hash = s.StateHash();
        _ = s.View.CorChoices("mhe");
        Assert.Equal(hash, s.StateHash());
    }
}
