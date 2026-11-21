using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-149 (Claude play 21 Nov, second play, notes 9 to 12), on the logs of the 21 Nov plays.</summary>
public class FourSentencesTests
{
    private static Session Play(string folder, string file, int? stopAfterDay = null)
    {
        var log = InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", folder, file));
        return Replay.Run(Repo.Load("tutorial"), log, stopAfterDay);
    }

    [Fact]
    public void The_tutorial_debrief_says_it_is_done_and_how_the_lines_came_out()
    {
        var s = Play("2026-11-21-2", "input-log-1.json");
        Assert.True(s.Finished);
        var bar = s.View.Debrief().Bar!;
        Assert.True(bar.Lines.All(l => l.State == BarState.Met));
        Assert.Equal("The tutorial is done: all four of Col Ward's lines held.", bar.Closing);

        // Not before the end, and not outside the tutorial.
        Assert.Null(Play("2026-11-21-2", "input-log-1.json", 5).View.Bar().Closing);
        Assert.Null(Session.Create(Repo.Load("sustainment"), 1).View.Bar().Closing);
    }

    [Fact]
    public void The_tutorial_debrief_counts_the_lines_that_held_when_some_did_not()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1);
        while (!s.Finished) s.AdvanceDay();
        var bar = s.View.Debrief().Bar!;
        var held = bar.Lines.Count(l => l.State == BarState.Met);
        Assert.InRange(held, 0, bar.Lines.Count - 1);
        var expected = held == 0 ? "none of Col Ward's four lines held" : $"{DebriefWords.Number(held)} of Col Ward's four lines held";
        Assert.Equal($"The tutorial is done: {expected}.", bar.Closing);
    }

    [Fact]
    public void A_report_still_on_the_way_names_the_cor_lag_and_the_one_who_reports_sooner()
    {
        var s = Play("2026-11-21", "input-log.json");
        var waste = s.View.Debrief().Gaps.Single(g => g.CapabilityId == "waste" && g.SiteId == "hubSite");
        var factor = Assert.Single(waste.Factors, f => f.Text.Contains("still on the way", StringComparison.Ordinal));
        // DESK-155 (a): when the office would have heard, not why it happened, beside the unforeseeable verdict.
        Assert.Equal(GapTag.Unforeseeable, factor.Tag);
        Assert.Contains("when the game ended. The failure itself could not be foreseen. TSgt Pratt reports problems 2 days late; with SSgt Lane, who reports the same day, its report would have been due on day 15.", factor.Text);
    }

    [Fact]
    public void The_first_plan_that_sends_hours_to_walk_in_buys_says_once_what_they_are()
    {
        const string said = "Walk-in small buys are the lumber, tools and parts CE brings to the counter; spare hours there are well spent, because once about 3 days' worth are waiting, CE goes to the commander.";
        var s = Session.Create(Repo.Load("tutorial"), 1);
        var plan = s.View.DayPlan();
        var first = plan.People.First(p => p.Sentence.Contains("to walk-in small buys", StringComparison.Ordinal));
        Assert.EndsWith(said, first.Sentence);
        Assert.Single(plan.People, p => p.Sentence.Contains("Walk-in small buys are", StringComparison.Ordinal));

        s.AdvanceDay();
        Assert.DoesNotContain(s.View.DayPlan().People, p => p.Sentence.Contains("Walk-in small buys are", StringComparison.Ordinal));
    }

    [Fact]
    public void A_part_added_from_what_was_learned_says_the_price_already_includes_it()
    {
        var s = Play("2026-11-21-2", "input-log-1.json", 4);
        Assert.Equal(5, s.Day);
        var award = new AwardAction
        {
            Id = "t-coastal", Instrument = "gpc", Vendor = "coastalRentals", Card = "tutForklift", Site = "hubSite", Capability = "mhe",
            Quantity = 3, StartDay = 8, DurationDays = 13, Day = 5,
        };
        Assert.DoesNotContain(s.View.Preview(award).SoftGaps, g => g.Contains("already includes", StringComparison.Ordinal));
        Assert.True(s.Apply(new AddLineAction { Card = "tutForklift", Line = "operator", Day = 5 }).Accepted);
        Assert.Contains("Coastal Rentals' price already includes its operators.", s.View.Preview(award).SoftGaps);
        Assert.DoesNotContain("already includes", s.View.AwardSummary(award)!.Sentence);   // the pinned summary stays short
    }
}
