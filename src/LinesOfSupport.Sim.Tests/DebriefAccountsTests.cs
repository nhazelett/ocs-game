using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Policies;

namespace LinesOfSupport.Sim.Tests;

/// <summary>
/// DESK-91 (Claude play 17 Nov, notes 37 and 38): the debrief accounts for what the player did not start. The obligated
/// total says how much of it sits on the calls the last rotation left, and a cure period still running at the end says
/// its last day and what decides it.
/// </summary>
public class DebriefAccountsTests
{
    [Fact]
    public void The_tutorial_debrief_says_the_inherited_calls_are_most_of_the_obligated_total()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, []);
        while (!s.Finished) s.AdvanceDay();
        var d = s.View.Debrief();
        Assert.Equal(62_400_000, d.Scorecard.InheritedObligatedCents);
        Assert.Equal(d.Scorecard.ObligatedCents, d.Scorecard.InheritedObligatedCents);   // nothing done, so all of it
        Assert.Equal("All $624,000 obligated is on the BPA calls the last rotation left: lodging and food service at Hub.", d.Inherited);
    }

    [Fact]
    public void A_cure_period_still_running_at_the_end_says_its_last_day_and_what_decides_it()
    {
        var s = Replay.Run(Repo.Load("tutorial"), 1, PolicyCatalog.Create("textbook"));
        var d = s.View.Debrief();
        Assert.True(d.Scorecard.ObligatedCents > d.Scorecard.InheritedObligatedCents);
        Assert.StartsWith("$624,000 of the ", d.Inherited);
        var cure = Assert.Single(d.Cures);
        Assert.Equal("hubDining", cure.ArrangementId);
        Assert.True(cure.LastDay > s.LastDay);
        Assert.Equal($"Island Camp Services has until day {cure.LastDay} to cure its food service at Hub; if it does not, you can terminate for default.", cure.Text);
    }

    [Fact]
    public void Read_only_and_quiet_where_there_is_nothing_to_say()
    {
        // Spoke B inherits nothing obligated and nobody issues a cure notice when nothing is done.
        var s = Session.Create(Repo.Load("spoke-b"), 1, []);
        while (!s.Finished) s.AdvanceDay();
        var hash = s.StateHash();
        var d = s.View.Debrief();
        Assert.Null(d.Inherited);
        Assert.Empty(d.Cures);
        Assert.Equal(0, d.Scorecard.InheritedObligatedCents);
        Assert.Equal(hash, s.StateHash());
    }
}
