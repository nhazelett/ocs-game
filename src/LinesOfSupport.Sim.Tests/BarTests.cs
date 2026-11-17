using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>
/// DESK-74 (docs/design/desk-redesign.md D28): the commander's bar. Checked on the 17 Nov review plays: the careful
/// tutorial player met every line, the hasty one missed three (docs/reviews/2026-11-17-gameplay-review.md).
/// </summary>
public class BarTests
{
    private static InputLog Log(string name)
        => InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-17-review", name + "-input-log.json")) with { SavedDay = null };

    private static Session Play(string name, string scenario, int? throughDay = null)
        => Replay.Run(Repo.Load(scenario), Log(name), throughDay);

    [Fact]
    public void The_turnover_says_the_bar_in_the_commanders_words_with_the_length()
    {
        var log = Log("tutorial-careful");
        var bar = Session.Create(Repo.Load("tutorial"), log.Seed, log.PrepChoices, log.Team).View.Turnover().Bar!;
        Assert.Equal("ward", bar.From!.Id);
        Assert.Equal(20, bar.Days);
        Assert.False(bar.Final);
        Assert.StartsWith("Water is never out more than a day. Forklifts the same: never out more than a day.", bar.Spoken);
        Assert.EndsWith("That's for the 20 days you're here.", bar.Spoken);
        Assert.All(bar.Lines, l => Assert.Equal(BarState.Met, l.State));
    }

    [Fact]
    public void The_careful_play_meets_every_line_and_the_debrief_opens_with_it()
    {
        var s = Play("tutorial-careful", "tutorial");
        var bar = s.View.Debrief().Bar!;
        Assert.True(bar.Final);
        Assert.Equal(["waterUp", "mheUp", "wasteUp", "mission"], bar.Lines.Select(l => l.Id));
        Assert.All(bar.Lines, l => Assert.Equal(BarState.Met, l.State));
        Assert.Equal("Water at Hub: out 0 days.", bar.Lines[0].Text);
    }

    [Fact]
    public void The_hasty_play_misses_three_lines()
    {
        var bar = Play("tutorial-hasty", "tutorial").View.Debrief().Bar!;
        Assert.Equal(["mheUp", "wasteUp", "mission"], bar.Lines.Where(l => l.State == BarState.Missed).Select(l => l.Id));
        Assert.Equal("Material handling equipment at Hub: out 4 days.", bar.Lines.Single(l => l.Id == "mheUp").Text);
        Assert.Equal("Mission output: 94%, against 95%.", bar.Lines.Single(l => l.Id == "mission").Text);
    }

    [Fact]
    public void A_running_measure_under_the_bar_is_amber_until_the_end()
    {
        var bar = Play("tutorial-hasty", "tutorial", 9).View.Bar();
        var mission = bar.Lines.Single(l => l.Id == "mission");
        Assert.Equal(BarState.AtRisk, mission.State);
        Assert.Equal("Mission output: 93% so far, against 95%.", mission.Text);
        Assert.Equal(BarState.Missed, bar.Lines.Single(l => l.Id == "mheUp").State);
    }

    [Fact]
    public void A_line_with_its_allowance_used_up_is_amber_and_met_if_it_holds()
    {
        var mid = Play("spoke-b", "spoke-b", 19).View.Bar().Lines.Single(l => l.Id == "powerUp");
        Assert.Equal(BarState.AtRisk, mid.State);
        Assert.Equal("Power at Spoke B: out 1 day so far.", mid.Text);
        Assert.Equal(BarState.Met, Play("spoke-b", "spoke-b").View.Debrief().Bar!.Lines.Single(l => l.Id == "powerUp").State);
    }

    [Fact]
    public void A_line_that_allows_no_bad_day_is_met_or_missed_never_amber()
    {
        for (var d = 1; d <= 20; d++)
        {
            var waste = Play("tutorial-hasty", "tutorial", d).View.Bar().Lines.Single(l => l.Id == "wasteUp");
            Assert.NotEqual(BarState.AtRisk, waste.State);
        }
    }

    [Theory]
    [InlineData("tutorial")]
    [InlineData("spoke-b")]
    [InlineData("spoke-b-90")]
    public void Every_played_scenarios_lines_are_in_the_commanders_words(string scenario)
        => Assert.All(Repo.Load(scenario).Scenario.PassCriteria, p => Assert.False(string.IsNullOrWhiteSpace(p.Says)));

    [Fact]
    public void The_tutorial_has_no_line_nothing_in_it_can_fail_and_does_not_call_its_length_a_week()
    {
        var t = Repo.Load("tutorial").Scenario;
        Assert.DoesNotContain(t.PassCriteria, p => p.Metric == "compliance.unauthorizedCommitments");
        foreach (var text in new[] { t.Title, t.Briefing!.Title, t.Briefing.Due, t.People.Single(p => p.Role == "commander").Lines["greeting"] })
        {
            Assert.DoesNotContain("this week", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("week one", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("first week ", text + " ", StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Reading_the_bar_changes_nothing()
    {
        var a = Play("spoke-b", "spoke-b", 10);
        var b = Play("spoke-b", "spoke-b", 10);
        _ = a.View.Bar(); _ = a.View.Turnover();
        Assert.Equal(b.StateHash(), a.StateHash());
    }
}
