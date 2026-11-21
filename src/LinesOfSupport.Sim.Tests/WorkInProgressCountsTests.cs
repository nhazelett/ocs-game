using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-132 (Claude play 20 Nov, third play, notes 4 and 5). The day's plan called Novak's standing staff meeting a job
/// that "should finish today", and the results then said nothing finished; the COR line said Pratt "watches no other contract"
/// while he was named on a purchase order being written.</summary>
public class WorkInProgressCountsTests
{
    private static InputLog ThirdPlay()
        => InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-20-3", "input-log.json"));

    [Fact]
    public void The_standing_staff_meeting_takes_hours_but_is_not_a_job_that_finishes()
    {
        var s = Replay.Run(Repo.Load("tutorial"), ThirdPlay(), stopAfterDay: 4);
        Assert.Equal(5, s.Day);
        var plan = s.View.DayPlan();
        var novak = plan.People.Single(p => p.MemberId == "novak");
        var meeting = novak.Work.Single(i => i.Label == "Staff meeting");
        Assert.Null(meeting.WorkId);
        Assert.False(meeting.FinishesToday);
        Assert.Equal("Staff meeting: 1.5 hours today.", meeting.Sentence);
        var jobs = novak.Work.Count(i => i.WorkId is not null && i.FinishesToday);
        if (jobs == 0) Assert.DoesNotContain("should finish today", novak.Sentence);
        else Assert.Contains(jobs == 1 ? "One job should finish today" : $"{jobs} jobs should finish today", novak.Sentence);
    }

    [Fact]
    public void A_cor_named_on_a_buy_being_written_says_so()
    {
        var log = ThirdPlay();
        var s = Replay.Run(Repo.Load("tutorial"), log, stopAfterDay: 4);
        foreach (var a in log.Actions.Where(a => a.Day == 5)) Assert.True(s.Apply(a).Accepted);
        Assert.Contains(s.Work, w => w.CompletedDay is null && w.Label.Contains("Coastal Rentals"));
        var pratt = s.View.CorChoices("waste").Single(c => c.Id == "corPratt");
        Assert.Contains("watches no other contract and is named on one more being written", pratt.Why);
    }
}
