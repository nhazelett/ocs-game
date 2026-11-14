using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Policies;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-39 (docs/design/desk-redesign.md D19): before the day ends, the plan of it. Nick on his 13 Nov replay
/// wanted "an update of everything you're gonna accomplish" before ending the day.</summary>
public class DayPlanTests
{
    private static readonly string[] Prep = ["gpcAccounts", "cashAndPayingAgent", "systemAccess"];

    [Fact]
    public void Each_teammate_shows_their_work_in_order_and_what_should_finish()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        while (s.Day < 3) s.AdvanceDay();
        Assert.True(s.Apply(new MeetVendorAction { Vendor = "sanitation", Assignee = "avery" }).Accepted);
        var plan = s.View.DayPlan();
        Assert.Equal(s.View.Team.Select(m => m.Id), plan.People.Select(p => p.MemberId));
        var avery = plan.People.Single(p => p.MemberId == "avery");
        var visit = Assert.Single(avery.Work);
        Assert.True(visit.FinishesToday);
        Assert.Equal(0, visit.HoursLeftAfter);
        Assert.EndsWith("and it should be done today.", visit.Sentence);
        Assert.StartsWith("MSgt Joan Avery: ", avery.Sentence);
        Assert.Contains("One job should finish today", avery.Sentence);
        var novak = plan.People.Single(p => p.MemberId == "novak");
        Assert.Empty(novak.Work);
        Assert.Contains("nothing to work on today", novak.Sentence);
        Assert.Equal(s.View.EndOfDayCheck(), plan.Open);
    }

    [Fact]
    public void Work_too_big_for_today_carries_over_with_the_hours_left()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        while (s.Day < 9) s.AdvanceDay();
        Assert.True(s.Apply(new AwardAction
        {
            Id = "waste", Instrument = "purchaseOrder", Vendor = "sanitation", Card = "tutWaste", Quantity = 3600,
            StartDay = 13, DurationDays = 8, FundingLine = "om1",
        }).Accepted);
        s.AdvanceDay();
        var you = s.View.DayPlan().People.Single(p => p.MemberId == "you");
        var order = you.Work.Single(w => w.Label.Contains("purchase order", StringComparison.OrdinalIgnoreCase));
        Assert.False(order.FinishesToday);
        Assert.True(order.HoursLeftAfter > 0);
        Assert.Contains("left after today.", order.Sentence);
        Assert.Contains("carries over", you.Sentence);
    }

    [Fact]
    public void Support_due_to_start_today_is_said_as_a_forecast()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        Assert.True(s.Apply(new AwardAction
        {
            Id = "water", Instrument = "gpc", Vendor = "islandMart", Card = "tutWater", Quantity = 4500, StartDay = 2,
            DurationDays = 7, FundingLine = "om1",
        }).Accepted);
        s.AdvanceDay();
        Assert.Contains("Island Mart should start water at Hub today.", s.View.DayPlan().Due);
    }

    public static IEnumerable<object[]> Scenarios() => [["spoke-b"], ["spoke-b-90"], ["sustainment"], ["tutorial"]];

    [Theory]
    [MemberData(nameof(Scenarios))]
    public void Every_job_planned_to_finish_today_does_and_the_plan_changes_nothing(string scenario)
    {
        var bundle = Repo.Load(scenario);
        var runs = Directory.GetFiles(Path.Combine(Repo.Scenario(scenario), "golden"), "*.json").Order(StringComparer.Ordinal)
            .Select(p => (Path.GetFileName(p), (Func<Session>)(() => Session.Create(bundle, InputLog.Load(p).Seed, InputLog.Load(p).PrepChoices, InputLog.Load(p).Team)), InputLog.Load(p) as InputLog, (IPolicy?)null))
            .Concat(PolicyCatalog.Names.Select(n => (n, (Func<Session>)(() => Session.Create(bundle, 1)), (InputLog?)null, (IPolicy?)PolicyCatalog.Create(n))));
        foreach (var (name, create, log, policy) in runs)
        {
            var s = create();
            while (!s.Finished)
            {
                var actions = log is not null ? log.Actions.Where(a => a.Day == s.Day).ToList() : policy!.Decide(s.View).ToList();
                foreach (var a in actions) s.Apply(a);
                var hash = s.StateHash();
                var plan = s.View.DayPlan();
                Assert.Equal(hash, s.StateHash());
                var day = s.Day;
                foreach (var p in plan.People)
                {
                    Assert.True(p.HoursPlanned <= p.HoursToday, $"{scenario}/{name} day {day}: {p.Sentence}");
                    Assert.All(p.Work.Concat([]), w => Assert.EndsWith(".", w.Sentence, StringComparison.Ordinal));
                }
                Assert.All(plan.Due.Concat(plan.People.Select(p => p.Sentence)), x =>
                {
                    Assert.True(char.IsUpper(x[0]), x);
                    Assert.DoesNotContain("  ", x, StringComparison.Ordinal);
                    Assert.DoesNotContain("..", x, StringComparison.Ordinal);
                });
                var finishing = plan.People.SelectMany(p => p.Work).Where(w => w.FinishesToday && w.WorkId is not null).Select(w => w.WorkId!).ToList();
                var carried = plan.People.SelectMany(p => p.Work).Where(w => !w.FinishesToday && w.WorkId is not null).ToList();
                s.AdvanceDay();
                // The plan's hours are the day's hours: what it says is left after today is left.
                foreach (var c in carried)
                {
                    var w = s.Work.Single(x => x.Id == c.WorkId);
                    Assert.True(w.CompletedDay is null && (w.HoursRemainingMilli + 499) / 500 / 2.0 == c.HoursLeftAfter,
                        $"{scenario}/{name} day {day}: {w.Label} was planned to have {c.HoursLeftAfter} hours left and has {w.HoursRemainingMilli} milli-hours");
                }
                foreach (var id in finishing)
                {
                    var w = s.Work.Single(x => x.Id == id);
                    Assert.True(w.CompletedDay == day, $"{scenario}/{name} day {day}: {w.Label} was planned to finish and is {(w.CompletedDay is { } c ? $"done day {c}" : $"still {w.HoursRemainingMilli} milli-hours short")}");
                }
            }
        }
    }
}
