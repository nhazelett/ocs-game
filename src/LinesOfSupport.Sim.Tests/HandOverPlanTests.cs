using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-176 (Claude play 21 Nov, seventh play, note P2-13). On the second log's day 8 the team screen said handing
/// Avery's waste order (8 hours at your pace) to you would be done day 8, and the day's plan then said "7 hours today, 1 hour
/// left after today": the staff meeting takes your first hour. The queue forecasts now start today from the hours left after
/// the trips and the staff meeting, as the plan does.</summary>
public class HandOverPlanTests
{
    private static Session SecondLogDay8()
        => Replay.Run(Repo.Load("tutorial"), InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-21-7", "input-log-2.json")), stopAfterDay: 7);

    [Fact]
    public void Handing_the_waste_order_to_you_on_day_8_says_done_day_9_as_the_plan_then_does()
    {
        var s = SecondLogDay8();
        Assert.Equal(8, s.Day);
        var you = s.View.AssigneeChoices(new AssignAction { Work = "w11", To = "you" }).Single(c => c.MemberId == "you");
        Assert.Equal("You: 7 hours free today, nothing queued. This takes you 8 hours; it would be done day 9.", you.Sentence);
        Assert.Equal(9, you.ResultDayMin);

        Assert.True(s.Apply(new AssignAction { Work = "w11", To = "you" }).Accepted);
        var plan = s.View.DayPlan().People.Single(p => p.MemberId == "you");
        Assert.Contains(plan.Work, i => i.WorkId == "w11" && i.Sentence.EndsWith("7 hours today, 1 hour left after today.", StringComparison.Ordinal));
        // The decisions list and the team page give the same day.
        s.AdvanceDay();
        Assert.Equal(9, s.Day);
        Assert.Null(s.Work.Single(w => w.Id == "w11").CompletedDay);
        s.AdvanceDay();
        Assert.Equal(9, s.Work.Single(w => w.Id == "w11").CompletedDay);
    }

    [Fact]
    public void A_person_with_no_meeting_or_trip_today_keeps_their_full_day()
    {
        var s = SecondLogDay8();
        var novak = s.View.AssigneeChoices(new AssignAction { Work = "w11", To = "novak" }).Single(c => c.MemberId == "novak");
        Assert.StartsWith("SrA Eli Novak: 8 hours free today, nothing queued.", novak.Sentence);
        Assert.Equal(9, novak.ResultDayMin);   // 11 hours at his pace: 8 today, 3 tomorrow
    }
}
