using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Policies;

namespace LinesOfSupport.Sim.Tests;

/// <summary>Q11: the per-arrangement ledger field agrees with the engine's total, every day.</summary>
public class LedgerTests
{
    private static IEnumerable<Session> EveryDay(Session s, Func<Session, IEnumerable<PlayerAction>> decide)
    {
        while (!s.Finished)
        {
            foreach (var a in decide(s).ToList()) s.Apply(a);
            s.AdvanceDay();
            yield return s;
        }
    }

    private static void AssertLedger(Session s, ref bool sawPositive)
    {
        foreach (var a in s.View.Arrangements)
        {
            Assert.True(a.UnobligatedIncurredCents >= 0, $"{a.Id} negative on day {s.Day}");
            if (a.IsCoordination) Assert.Equal(0, a.UnobligatedIncurredCents);
            else Assert.Equal(Math.Max(0, a.IncurredCents - a.ObligatedCents), a.UnobligatedIncurredCents);
            if (a.UnobligatedIncurredCents > 0) sawPositive = true;
        }
        Assert.Equal((long)s.Metrics["cost.unobligatedIncurredCents"], s.View.Arrangements.Sum(a => a.UnobligatedIncurredCents));
    }

    [Theory]
    [InlineData("spoke-b")]
    [InlineData("spoke-b-90")]
    public void Per_arrangement_unobligated_incurred_sums_to_the_metric_on_every_golden_day(string scenario)
    {
        var bundle = Repo.Load(scenario);
        var sawPositive = false;
        foreach (var path in Directory.GetFiles(Path.Combine(Repo.Scenario(scenario), "golden"), "*.json"))
        {
            var log = InputLog.Load(path);
            var byDay = log.Actions.GroupBy(a => a.Day).ToDictionary(g => g.Key, g => g.ToList());
            var s = Session.Create(bundle, log.Seed, log.PrepChoices);
            foreach (var day in EveryDay(s, x => byDay.TryGetValue(x.Day, out var t) ? t : []))
                AssertLedger(day, ref sawPositive);
        }
        foreach (var policy in new[] { "do-nothing", "greedy", "textbook" })
            foreach (var seed in new long[] { 1, 2, 3 })
            {
                var p = PolicyCatalog.Create(policy);
                var s = Session.Create(bundle, seed, p.PrepChoices(bundle));
                foreach (var day in EveryDay(s, x => p.Decide(x.View)))
                    AssertLedger(day, ref sawPositive);
            }
        Assert.True(sawPositive, "no arrangement ever carried unobligated incurred cost; the test would not catch a wrong sign");
    }
}
