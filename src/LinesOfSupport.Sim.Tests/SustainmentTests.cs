using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Policies;

namespace LinesOfSupport.Sim.Tests;

/// <summary>
/// M3-07: the representative scenario (skeleton 3, Sustainment; plan in docs/design/M3-07-representative-scenario.md).
/// Hub plus three spokes, 60 days, eight event chains, inherited clocks. Goldens are in GoldenTests; the 60-seed
/// policy ordering is in BalanceTests; these check the rest of the acceptance.
/// </summary>
public class SustainmentTests
{
    private static readonly string[] Chains =
        ["corRotates", "bpaNotExtended", "bFuelSlips", "fyFundingLate", "partnerDrySpell", "spokeCGrows", "secondBargeWeather"];

    [Fact]
    public void Five_seeds_give_visibly_different_games()
    {
        var bundle = Repo.Load("sustainment");
        var runs = Enumerable.Range(1, 5).Select(seed => Replay.Run(bundle, seed, PolicyCatalog.Create("textbook"))).ToList();
        Assert.Equal(5, runs.Select(s => s.StateHash()).Distinct().Count());
        // The seeded chains land on different days, so the player meets them in a different order.
        int FiredOn(Session s, string id) => s.Records.First(r => r.Kind == RecordKind.Event && r.Subject == id).Day;
        Assert.True(runs.Select(s => (FiredOn(s, "partnerDrySpell"), FiredOn(s, "secondBargeWeather"), FiredOn(s, "fyFundingLate"))).Distinct().Count() >= 4);
        // And the outcome moves with them.
        Assert.True(runs.Select(s => s.Metrics["mission.output"]).Distinct().Count() >= 3);
    }

    [Fact]
    public void Every_chain_fires_for_a_player_who_ignores_them()
    {
        var s = Replay.Run(Repo.Load("sustainment"), 1, PolicyCatalog.Create("do-nothing"));
        foreach (var id in Chains)
            Assert.Contains(s.Records, r => r.Kind == RecordKind.Event && r.Subject == id);
        // Chain 4 is the inherited UCA clock, not an event: the hidden past-due UCA draws the inquiry.
        Assert.Contains(s.Records, r => r.Kind == RecordKind.Compliance && r.Subject == "cLodging" && r.Text.Contains("Higher HQ asked"));
    }

    [Fact]
    public void Clocks_first_meets_the_clock_criteria_the_trap_misses()
    {
        var bundle = Repo.Load("sustainment");
        Session Approach(string name) => Replay.Run(bundle, InputLog.Load(Path.Combine(Repo.Scenario("sustainment"), "golden", $"{name}.json")));
        var clocks = Approach("approach1-clocks-first");
        var trap = Approach("approach2-requests-first");
        Assert.Equal(0, clocks.Metrics["compliance.hhqInquiries"]);
        Assert.Equal(0, clocks.Metrics["cost.unobligatedIncurredCents"]);
        Assert.True(trap.Metrics["compliance.hhqInquiries"] > 0);
        Assert.True(trap.Metrics["cost.unobligatedIncurredCents"] > 0);
        Assert.True(clocks.Metrics["mission.output"] > trap.Metrics["mission.output"] + 0.1);
        // Dealing with bFuel (a cure notice, or a second source with an overlap) keeps Spoke B fuel up.
        foreach (var name in new[] { "approach3-cure-bfuel", "approach4-resource-bfuel" })
            Assert.True(Approach(name).Metrics["site.spokeBSite.fuel.downDays"] < clocks.Metrics["site.spokeBSite.fuel.downDays"], name);
    }

    [Fact]
    public void Textbook_meets_the_pass_criteria_on_most_seeds()
    {
        var bundle = Repo.Load("sustainment");
        var passed = Enumerable.Range(0, 20).Count(seed =>
        {
            var m = Replay.Run(bundle, seed, PolicyCatalog.Create("textbook")).Metrics;
            return bundle.Scenario.PassCriteria.All(c => Holds(m[c.Metric], c.Comparator, c.Value));
        });
        Assert.True(passed >= 10, $"textbook met every pass criterion on {passed} of 20 seeds");
    }

    [Theory]
    [InlineData("do-nothing", 3)]
    [InlineData("greedy", 3)]
    [InlineData("textbook", 3)]
    [InlineData("textbook", 77)]
    public void Invariants_hold_with_inherited_clocks(string policy, int seed)
    {
        var bundle = Repo.Load("sustainment");
        var p = PolicyCatalog.Create(policy);
        var s = Session.Create(bundle, seed, p.PrepChoices(bundle));
        while (!s.Finished)
        {
            foreach (var a in p.Decide(s.View).ToList()) s.Apply(a);
            s.AdvanceDay();
            foreach (var f in s.Funding) Assert.True(f.ObligatedCents <= f.AmountCents, $"funding line {f.Id} over-obligated");
            foreach (var c in s.Capabilities) Assert.True(c.BufferMilli >= 0);
            foreach (var a in s.Arrangements.Where(a => a.Uca is { Definitized: false }))
            {
                var u = a.Uca!;
                var cap = u.ProposalReceived ? s.Rules.Uca.ObligationCapAfterProposal : s.Rules.Uca.ObligationCapBeforeProposal;
                Assert.True(a.ObligatedCents + u.PriorObligatedCents <= (long)Math.Ceiling(u.NteCents * cap), $"{a.Id} obligated past the UCA cap");
            }
        }
    }

    private static bool Holds(double actual, string comparator, double value) => comparator switch
    {
        "<=" => actual <= value, ">=" => actual >= value, "==" => actual == value, "<" => actual < value, ">" => actual > value,
        _ => throw new ArgumentException(comparator),
    };
}
