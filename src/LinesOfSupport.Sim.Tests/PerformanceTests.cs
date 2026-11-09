using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Policies;

namespace LinesOfSupport.Sim.Tests;

/// <summary>M2-18: vendor under-delivery and the cure notice (FAR 49.402-3).</summary>
public class PerformanceTests
{
    private static Session Golden(string name)
        => Replay.Run(Repo.Load("spoke-b-90"), InputLog.Load(Path.Combine(Repo.Scenario("spoke-b-90"), "golden", name + ".json")));

    [Fact]
    public void Two_failure_days_in_three_raise_an_inbox_decision_on_a_written_contract()
    {
        var s = Golden("approach1-as-written");
        var i = Assert.Single(s.PerformanceIssues);
        Assert.Equal("gens", i.ArrangementId);
        Assert.True(i.FailureDays.Count >= 2 && i.FailureDays.Max() - i.FailureDays.Min() < 3);
        var rec = s.Records.Single(r => r.Subject == i.Id && r.Kind == RecordKind.Inbox);
        Assert.Contains(rec.CausedBy, id => s.Records.First(r => r.Id == id).Kind == RecordKind.VendorFailure);
        Assert.Equal("no decision", s.View.PerformanceDecision(i));
    }

    [Fact]
    public void Cash_card_handshake_and_inherited_arrangements_never_raise_issues()
    {
        for (long seed = 1; seed <= 60; seed++)
        {
            var s = Replay.Run(Repo.Load("spoke-b"), seed, PolicyCatalog.Create("greedy"));
            foreach (var i in s.PerformanceIssues)
            {
                var a = s.Arrangements.First(x => x.Id == i.ArrangementId);
                Assert.True(a.CreatedDay > 0 && !a.Unauthorized && !a.IsCoordination);
                Assert.True(s.Rules.Instruments[a.Instrument].WrittenTerms, $"{a.Instrument} has no written terms");
            }
        }
    }

    [Fact]
    public void Cure_notice_runs_the_cure_period_and_ends_cured_or_in_default()
    {
        var s = Golden("approach1-cure");
        var i = s.PerformanceIssues.First(x => x.Id == "pi1");
        Assert.NotNull(i.CureNoticeDay);
        Assert.Equal(i.CureNoticeDay + s.Rules.Performance.CureDays, i.CureEndsDay);
        Assert.NotEqual(CureOutcome.None, i.Outcome);
        Assert.StartsWith("cure notice day", s.View.PerformanceDecision(i));
        Assert.Equal(1, s.Metrics["performance.cureNotices"]);

        var outcomes = new HashSet<CureOutcome>();
        for (long seed = 1; seed <= 200 && outcomes.Count < 2; seed++)
            foreach (var x in Replay.Run(Repo.Load("spoke-b"), seed, PolicyCatalog.Create("textbook")).PerformanceIssues)
                if (x.Outcome != CureOutcome.None) outcomes.Add(x.Outcome);
        Assert.Contains(CureOutcome.Cured, outcomes);
        Assert.Contains(CureOutcome.Default, outcomes);
    }

    [Fact]
    public void Re_source_and_terminate_are_read_back_for_the_debrief()
    {
        Assert.StartsWith("re-sourced", Golden("approach1-resource").View.PerformanceDecision(Golden("approach1-resource").PerformanceIssues.First(i => i.Id == "pi1")));
        var t = Golden("approach1-terminate");
        Assert.Equal("terminated", t.View.PerformanceDecision(t.PerformanceIssues.First(i => i.Id == "pi1")));
        Assert.Contains(Golden("approach1-cure").View.Debrief().Gaps.SelectMany(g => g.Factors),
                        f => f.Kind == Views.GapFactorKind.VendorFailure && f.Text.Contains("The performance issue with Harbor Equipment Co on power at Spoke B opened"));
    }

    [Fact]
    public void Bad_cure_notices_are_hard_stops()
    {
        var b = Repo.Load("spoke-b-90");
        var log = InputLog.Load(Path.Combine(Repo.Scenario("spoke-b-90"), "golden", "approach1-as-written.json"));
        var s = Replay.Run(b, log, stopAfterDay: 42);
        Assert.False(s.Apply(new CureNoticeAction { Issue = "nope" }).Accepted);
        Assert.True(s.Apply(new CureNoticeAction { Issue = "pi1" }).Accepted);
        Assert.False(s.Apply(new CureNoticeAction { Issue = "pi1" }).Accepted);
    }

    [Fact]
    public void Games_without_a_cure_notice_draw_nothing_from_the_performance_stream()
    {
        // Same seed, greedy never issues cure notices; its outcome must not depend on the performance stream.
        var b = Repo.Load("spoke-b");
        var a = Replay.Run(b, 5, PolicyCatalog.Create("greedy"));
        Assert.Equal(0, a.Metrics["performance.cureNotices"]);
        Assert.Equal(a.StateHash(), Replay.Run(b, 5, PolicyCatalog.Create("greedy")).StateHash());
    }
}
