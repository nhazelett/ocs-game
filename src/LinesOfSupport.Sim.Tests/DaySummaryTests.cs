using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Policies;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-40 (docs/design/desk-redesign.md D20): after the day plays, what actually happened. Nick on his 13 Nov
/// replay asked for "just an end of day summary".</summary>
public class DaySummaryTests
{
    private static readonly string[] Prep = ["gpcAccounts", "cashAndPayingAgent", "systemAccess"];

    [Fact]
    public void There_is_no_summary_before_a_day_is_played()
        => Assert.Null(Session.Create(Repo.Load("tutorial"), 1, Prep).View.DaySummary());

    [Fact]
    public void It_says_what_finished_and_who_did_it_and_what_was_short_and_why()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        while (s.Day < 3) s.AdvanceDay();
        Assert.True(s.Apply(new MeetVendorAction { Vendor = "sanitation", Assignee = "avery" }).Accepted);
        s.AdvanceDay();
        var summary = s.View.DaySummary()!;
        Assert.Equal(3, summary.Day);
        var met = Assert.Single(summary.Finished);
        Assert.Equal("avery", met.TeammateId);
        Assert.StartsWith("Sitting down with ", met.Sentence);
        Assert.EndsWith(": done by MSgt Joan Avery.", met.Sentence);
        Assert.Contains(summary.Short, x => x.StartsWith("Water at Hub was at 0%. Nothing supplies water at Hub until", StringComparison.Ordinal));
        Assert.Equal("Day 3: one job finished and 2 things were short.", summary.Sentence);
        Assert.Empty(summary.Slipped);
    }

    [Fact]
    public void Work_that_goes_on_says_what_is_left_and_when_it_should_be_done()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        while (s.Day < 9) s.AdvanceDay();
        Assert.True(s.Apply(new AwardAction
        {
            Id = "waste", Instrument = "purchaseOrder", Vendor = "sanitation", Card = "tutWaste", Quantity = 3600,
            StartDay = 13, DurationDays = 8, FundingLine = "om1",
        }).Accepted);
        s.AdvanceDay();
        s.AdvanceDay();
        var carried = Assert.Single(s.View.DaySummary()!.CarriedOver);
        Assert.Equal("you", carried.TeammateId);
        Assert.Matches(@"left with you, done about day \d+\.$", carried.Sentence);
    }

    [Fact]
    public void Support_that_started_is_listed()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        Assert.True(s.Apply(new AwardAction
        {
            Id = "water", Instrument = "gpc", Vendor = "islandMart", Card = "tutWater", Quantity = 4500, StartDay = 2,
            DurationDays = 7, FundingLine = "om1",
        }).Accepted);
        s.AdvanceDay();
        s.AdvanceDay();
        Assert.Contains("Island Mart started water at Hub 4500 gal/day.", s.View.DaySummary()!.Started);
    }

    public static IEnumerable<object[]> Scenarios() => [["spoke-b"], ["spoke-b-90"], ["sustainment"], ["tutorial"]];

    [Theory]
    [MemberData(nameof(Scenarios))]
    public void Every_day_of_every_golden_has_a_summary_that_matches_what_happened(string scenario)
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
                s.AdvanceDay();
                var d = s.Day - 1;
                var hash = s.StateHash();
                var summary = s.View.DaySummary();
                Assert.Equal(hash, s.StateHash());
                Assert.NotNull(summary);
                Assert.Equal(d, summary.Day);
                var done = s.Work.Count(w => w.CompletedDay == d && !w.Label.StartsWith("Staff meeting", StringComparison.Ordinal)
                                             && !w.Label.StartsWith("SF 44 delivery", StringComparison.Ordinal));
                Assert.Equal(done, summary.Finished.Count);
                Assert.Equal(s.View.MorningBoard().Tiles.Count(t => t.Status is "down" or "degraded"), summary.Short.Count);
                Assert.Empty(summary.Slipped);   // the plan's hours are the day's hours (DayPlanTests), so nothing slips here
                Assert.All(summary.Finished.Concat(summary.Slipped).Concat(summary.CarriedOver).Select(x => x.Sentence)
                               .Concat(summary.Started).Concat(summary.Ended).Concat(summary.Short).Append(summary.Sentence), x =>
                {
                    Assert.True(char.IsUpper(x[0]), $"{scenario}/{name} day {d}: {x}");
                    Assert.EndsWith(".", x, StringComparison.Ordinal);
                    Assert.DoesNotContain("..", x, StringComparison.Ordinal);
                    Assert.DoesNotContain("  ", x, StringComparison.Ordinal);
                    Assert.DoesNotContain(".:", x, StringComparison.Ordinal);
                });
            }
        }
    }
}
