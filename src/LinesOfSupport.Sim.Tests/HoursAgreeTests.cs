using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

/// <summary>
/// DESK-120 (Claude play 20 Nov 2026, note 14). On day 4 the hand-over picker said Avery's job "takes them 7 hours", her
/// queue then said 6.5, and the request page said "then about 7 hours of work" above "then about 6.5 hours of work" for
/// the same call. A teammate's hours left on a job read to the half hour everywhere (DESK-14).
/// </summary>
public class HoursAgreeTests
{
    private static InputLog Play()
        => InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-20", "input-log.json"));

    /// <summary>Day 4 after Dunn was asked and the Harbor call placed, before it is handed to Avery.</summary>
    private static (Session S, string Work) BeforeHandOver()
    {
        var log = Play();
        var s = Replay.Run(Repo.Load("tutorial"), log, stopAfterDay: 3);
        foreach (var a in log.Actions.Where(a => a.Day == 4 && a is not AssignAction)) Assert.True(s.Apply(a).Accepted);
        var work = Assert.Single(s.Work, w => w.SubjectId == "player-award-2").Id;
        return (s, work);
    }

    [Fact]
    public void The_hand_over_picker_says_the_hours_the_team_view_will_say()
    {
        var (s, work) = BeforeHandOver();
        var avery = Assert.Single(s.View.AssigneeChoices(new AssignAction { Day = 4, Work = work, To = "avery" }), c => c.MemberId == "avery");
        Assert.Contains("This takes them 6.5 hours;", avery.Sentence);

        Assert.True(s.Apply(new AssignAction { Day = 4, Work = work, To = "avery" }).Accepted);
        var queued = Assert.Single(Assert.Single(s.View.Team, m => m.Id == "avery").Work, w => w.WorkId == work);
        Assert.Equal(6.5, queued.HoursLeft);
    }

    [Fact]
    public void The_request_and_its_decision_say_the_same_hours()
    {
        var (s, work) = BeforeHandOver();
        Assert.True(s.Apply(new AssignAction { Day = 4, Work = work, To = "avery" }).Accepted);
        var standing = Assert.Single(s.View.WhereThingsStand(), e => e.CardId == "tutForklift");
        Assert.EndsWith("then about 6.5 hours of work.", standing.Sentence);
        Assert.DoesNotContain("7 hours", standing.Sentence);
    }
}
