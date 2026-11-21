using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-179 (Claude play 21 Nov, eighth play, notes P2-15, P2-18). Every line of the day's plan rounded up to the
/// half hour on its own, so Avery's 0.8-hour staff meeting and 7.2 hours of writing read 1 and 7.5 under "8 of 8 hours
/// planned", and the 13-hour waste order became 7.5 today and 6 after. The cure notice handed to Avery read 4 shop hours on
/// the decision and 3.5 on the team page. The lines now add up to the hours planned and the decision says the hours of the
/// person who has the work.</summary>
public class HoursAddUpTests
{
    private static InputLog SecondLog()
        => InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-21-8", "input-log-2.json"));

    [Fact]
    public void The_lines_of_the_day_8_plan_add_up_to_the_hours_planned()
    {
        var s = Replay.Run(Repo.Load("tutorial"), SecondLog(), stopAfterDay: 7);
        Assert.Equal(8, s.Day);
        var avery = s.View.DayPlan().People.Single(p => p.MemberId == "avery");
        Assert.Equal(8, avery.HoursPlanned);
        Assert.Equal(avery.HoursPlanned, avery.Work.Sum(w => w.HoursToday));
        Assert.StartsWith("8 of 8 hours planned.", avery.Sentence, StringComparison.Ordinal);
        var waste = avery.Work.Single(w => w.WorkId is not null);
        Assert.Equal(7, waste.HoursToday);
        Assert.Equal(6, waste.HoursLeftAfter);
        // Today and after today add up to the hours the team page gives the job.
        var team = s.View.Team.Single(m => m.Id == "avery").Work.Single(w => w.WorkId == waste.WorkId);
        Assert.Equal(team.HoursLeft, waste.HoursToday + waste.HoursLeftAfter);
        Assert.Equal("Staff meeting: 1 hour today.", avery.Work.Single(w => w.WorkId is null).Sentence);
    }

    [Fact]
    public void On_day_13_the_handed_over_cure_notice_reads_the_same_on_the_decision_the_team_page_and_the_plan()
    {
        var log = SecondLog();
        var s = Replay.Run(Repo.Load("tutorial"), log, stopAfterDay: 12);
        foreach (var a in log.Actions.Where(a => a.Day == 13)) Assert.True(s.Apply(a).Accepted);
        var team = s.View.Team.Single(m => m.Id == "avery");
        var cure = team.Work.Single();
        Assert.Equal(3.5, cure.HoursLeft);
        var started = s.View.Decisions().Single(d => d.Sentence.Contains("Cure notice", StringComparison.Ordinal) && d.Sentence.Contains("started", StringComparison.Ordinal));
        Assert.Contains("started: 3.5 hours, given to MSgt Joan Avery.", started.Sentence, StringComparison.Ordinal);
        var plan = s.View.DayPlan().People.Single(p => p.MemberId == "avery");
        Assert.Equal(3.5, plan.Work.Single(w => w.WorkId == cure.WorkId).HoursToday);
        Assert.Equal(plan.HoursPlanned, plan.Work.Sum(w => w.HoursToday));
        Assert.Equal(team.HoursSpokenForToday, plan.HoursPlanned);
    }

    [Fact]
    public void Every_plan_on_every_recorded_day_adds_up()
    {
        var log = SecondLog();
        var s = Replay.Run(Repo.Load("tutorial"), log, stopAfterDay: 0);
        while (!s.Finished)
        {
            foreach (var p in s.View.DayPlan().People)
                Assert.Equal(p.HoursPlanned, p.Work.Sum(w => w.HoursToday));
            foreach (var a in log.Actions.Where(a => a.Day == s.Day)) s.Apply(a);
            s.AdvanceDay();
        }
    }
}
