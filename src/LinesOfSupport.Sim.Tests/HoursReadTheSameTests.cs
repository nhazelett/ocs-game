using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-131 (Claude play 20 Nov, third play, note 3). Novak's day 1 ask read 2 hours in the picker, 1.5 on the strip and
/// "about 1 shop hour" on the request; Avery's day 7 purchase order was quoted 7 to 13 hours and then planned 6.5. A named
/// person's hours now read to the half hour in the picker for new work, and the request's in-work line says that person's.</summary>
public class HoursReadTheSameTests
{
    private static InputLog ThirdPlay()
        => InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-20-3", "input-log.json"));

    [Fact]
    public void Novaks_day_1_ask_reads_one_and_a_half_hours_in_the_picker_the_strip_and_the_request()
    {
        var log = ThirdPlay();
        var s = Replay.Run(Repo.Load("tutorial"), log, stopAfterDay: 0);
        Assert.Equal(1, s.Day);
        var ask = (AskAboutAction)log.Actions.First();
        var novak = s.View.AssigneeChoices(ask with { Assignee = null }).Single(c => c.MemberId == "novak");
        Assert.Contains("This takes them 1.5 hours;", novak.Sentence);

        Assert.True(s.Apply(ask).Accepted);
        Assert.Equal(1.5, s.View.Team.Single(m => m.Id == "novak").HoursSpokenForToday);
        var helper = s.View.RequirementEditor("tutWater")!.WhoCanHelp!.Single(h => h.State == HelperView.Asking);
        Assert.Equal(1.5, helper.Hours);
    }

    [Fact]
    public void Averys_day_7_purchase_order_is_quoted_in_her_hours_to_the_half_hour()
    {
        var log = ThirdPlay();
        var s = Replay.Run(Repo.Load("tutorial"), log, stopAfterDay: 6);
        Assert.Equal(7, s.Day);
        var po = log.Actions.OfType<AwardAction>().Single(a => a.Day == 7);
        foreach (var earlier in log.Actions.Where(a => a.Day == 7 && a is not AwardAction)) s.Apply(earlier);
        var avery = s.View.AssigneeChoices(po with { Assignee = null }).Single(c => c.MemberId == "avery");
        var p = s.View.PreviewAny(po);
        var lo = HalfHoursOf(p.HoursMinMilli);
        var hi = HalfHoursOf(p.HoursMaxMilli);
        Assert.Equal((6.5, 13.0), (lo, hi));   // the play read "7 to 13", then a plan of 6.5
        Assert.Contains("This takes them 6.5 to 13 hours;", avery.Sentence);

        Assert.True(s.Apply(po).Accepted);
        var work = s.View.Team.Single(m => m.Id == "avery").Work.Single(w => w.Label.Contains("waste", StringComparison.OrdinalIgnoreCase));
        Assert.InRange(work.HoursLeft, lo, hi);
    }

    private static double HalfHoursOf(long milli) => milli <= 0 ? 0 : (milli + 499) / 500 / 2.0;
}
