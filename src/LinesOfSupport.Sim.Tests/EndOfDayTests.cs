using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-04 (docs/design/desk-redesign.md D1): before the day ends, what is still open, as sentences. Read only.</summary>
public class EndOfDayTests
{
    private static readonly string[] Prep = ["gpcAccounts", "cashAndPayingAgent", "systemAccess"];

    [Fact]
    public void Day_one_of_the_tutorial_names_the_water_and_the_idle_teammates()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        Assert.Equal(
        [
            "Water for Hub is wanted from day 2 and nothing is arranged or in work yet.",
            "MSgt Joan Avery has nothing to work on today; their 8 hours go to walk-in small buys.",
            "SrA Eli Novak has nothing to work on today; their 8 hours go to walk-in small buys.",
        ], s.View.EndOfDayCheck());
    }

    [Fact]
    public void Buying_the_water_and_giving_work_out_clears_the_check()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        Assert.True(s.Apply(new AwardAction
        {
            Id = "water", Instrument = "gpc", Vendor = "islandMart", Card = "tutWater", Quantity = 4500, StartDay = 2,
            DurationDays = 7, FundingLine = "om1",
        }).Accepted);
        Assert.True(s.Apply(new FindFinanceAction { Assignee = "novak" }).Accepted);
        var check = s.View.EndOfDayCheck();
        Assert.DoesNotContain(check, x => x.StartsWith("Water for Hub", StringComparison.Ordinal));
        // The finance visit is 3 usual hours, 4 of the junior's; he still has 4 free.
        Assert.Contains("SrA Eli Novak has 4 free hours today.", check);
        Assert.Contains(check, x => x.StartsWith("MSgt Joan Avery has nothing to work on today", StringComparison.Ordinal));
    }

    [Fact]
    public void A_late_request_says_so_and_one_far_off_is_not_listed()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1);
        Assert.Equal([], s.View.EndOfDayCheck()); // power from day 7, water from day 5: more than three days off; one-person office
        while (s.Day < 6) s.AdvanceDay();
        var check = s.View.EndOfDayCheck();
        Assert.Contains("Water for Spoke C was wanted from day 5 and nothing is arranged or in work.", check);
        Assert.Contains("Power for Spoke B is wanted from day 7 and nothing is arranged or in work yet.", check);
        Assert.DoesNotContain(check, x => x.Contains("Food service", StringComparison.Ordinal));
    }

    [Fact]
    public void Asking_for_the_check_changes_nothing()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        var hash = s.StateHash();
        _ = s.View.EndOfDayCheck();
        _ = s.View.Team;
        Assert.Equal(hash, s.StateHash());
    }
}
