using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

/// <summary>D49 (DESK-175; Claude play 21 Nov, seventh play, note P2-14). The seventh play's second player replaced a card
/// buy with a written order from Harbor on day 10 and read only the pinned summary, which said every day was already
/// covered; Harbor brings forklifts without the operators the request includes. The summary's third sentence now says so,
/// and "already covered" moves to the quote's notes.</summary>
public class IncludedLeftOutTests
{
    private static InputLog Second()
        => InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-21-7", "input-log-2.json"));

    private const string Left = "Harbor Equipment Co does not bring operators, which the request includes; without them it fails more often every day it runs.";

    [Fact]
    public void The_second_logs_Harbor_order_on_day_10_names_the_operators_it_leaves_out()
    {
        var log = Second();
        var s = Replay.Run(Repo.Load("tutorial"), log, stopAfterDay: 9);
        Assert.Equal(10, s.Day);
        var harbor = log.Actions.OfType<AwardAction>().Single(a => a.Day == 10 && a.Vendor == "harborEquipment");
        var summary = s.View.AwardSummary(harbor)!;
        Assert.Equal(Left, summary.Why);
        Assert.True(summary.Attention);
        Assert.EndsWith(Left, summary.Sentence);
        Assert.DoesNotContain("already covered", summary.Sentence);
        Assert.Equal(3, summary.Sentence.Split(". ").Length);   // D46
        var notes = s.View.Preview(harbor).SoftGaps;
        Assert.Single(notes, n => n == "Every day it would run is already covered.");
        Assert.DoesNotContain(notes, n => n == Left);
    }

    [Fact]
    public void Coastal_with_its_operators_and_water_goods_say_nothing_of_it()
    {
        var log = Second();
        var s = Replay.Run(Repo.Load("tutorial"), log, stopAfterDay: 9);
        var coastal = log.Actions.OfType<AwardAction>().Single(a => a.Day == 10 && a.Vendor == "harborEquipment") with { Id = "c", Vendor = "coastalRentals", Instrument = "gpc", FundingLine = null, Priorities = null, Assignee = null, Quantity = 1 };
        Assert.DoesNotContain("does not bring", s.View.AwardSummary(coastal)?.Sentence ?? "");

        var t = Session.Create(Repo.Load("tutorial"), 1, ["gpcAccounts", "cashAndPayingAgent", "systemAccess"]);
        var water = new AwardAction { Id = "w", Instrument = "gpc", Vendor = "islandMart", Card = "tutWater", Quantity = 4500, StartDay = 2, DurationDays = 7 };
        Assert.DoesNotContain("does not bring", t.View.AwardSummary(water)!.Sentence);
    }
}
