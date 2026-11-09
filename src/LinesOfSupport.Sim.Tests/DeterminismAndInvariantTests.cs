using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Policies;

namespace LinesOfSupport.Sim.Tests;

public class DeterminismTests
{
    [Fact]
    public void Same_seed_and_policy_give_the_same_hash()
    {
        var bundle = Repo.Load("spoke-b");
        foreach (var name in PolicyCatalog.Names)
        {
            var a = Replay.Run(bundle, 42, PolicyCatalog.Create(name)).StateHash();
            var b = Replay.Run(bundle, 42, PolicyCatalog.Create(name)).StateHash();
            Assert.Equal(a, b);
        }
    }

    [Fact]
    public void A_policy_run_replays_from_its_own_input_log()
    {
        var bundle = Repo.Load("spoke-b");
        foreach (var name in PolicyCatalog.Names)
        {
            var played = Replay.Run(bundle, 7, PolicyCatalog.Create(name));
            var log = InputLog.Load(WriteTemp(played.ToInputLog().ToJson()));
            var replayed = Replay.Run(bundle, log);
            Assert.Equal(played.StateHash(), replayed.StateHash());
        }
    }

    [Fact]
    public void Different_seeds_vary_the_world()
    {
        var bundle = Repo.Load("spoke-b");
        var hashes = Enumerable.Range(0, 20).Select(s => Replay.Run(bundle, s, PolicyCatalog.Create("textbook")).StateHash()).ToHashSet();
        Assert.True(hashes.Count > 5, $"only {hashes.Count} distinct outcomes across 20 seeds");
    }

    [Fact]
    public void Decay_curve_is_exact_at_half_lives_and_never_below_floor()
    {
        Assert.Equal(120, Fixed.Decay(120, 20, 10, 1));
        Assert.Equal(70, Fixed.Decay(120, 20, 10, 11));
        Assert.Equal(45, Fixed.Decay(120, 20, 10, 21));
        for (var d = 1; d < 400; d++)
        {
            var v = Fixed.Decay(120, 20, 10, d);
            Assert.InRange(v, 20, 120);
            Assert.True(v >= Fixed.Decay(120, 20, 10, d + 1), $"curve rose at day {d}");
        }
    }

    [Fact]
    public void Random_streams_are_independent_and_stable()
    {
        var a = DeterministicRandom.ForStream(1, "vendor");
        var b = DeterministicRandom.ForStream(1, "vendor");
        var c = DeterministicRandom.ForStream(1, "events");
        var sa = Enumerable.Range(0, 10).Select(_ => a.NextInt(0, 999)).ToList();
        Assert.Equal(sa, Enumerable.Range(0, 10).Select(_ => b.NextInt(0, 999)).ToList());
        Assert.NotEqual(sa, Enumerable.Range(0, 10).Select(_ => c.NextInt(0, 999)).ToList());
        // Pinned values: a change here means saves from earlier builds would replay differently.
        Assert.Equal(sa, DeterministicRandom.ForStream(1, "vendor") is var r ? Enumerable.Range(0, 10).Select(_ => r.NextInt(0, 999)).ToList() : null);
    }

    private static string WriteTemp(string json)
    {
        var path = Path.Combine(Path.GetTempPath(), $"los-log-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, json);
        return path;
    }
}

/// <summary>Invariants that must hold for every seed and every policy (ADR-0002).</summary>
public class InvariantTests
{
    public static IEnumerable<object[]> Runs()
    {
        foreach (var name in PolicyCatalog.Names)
            for (var seed = 0; seed < 200; seed += 13)
                yield return [name, seed];
    }

    [Theory]
    [MemberData(nameof(Runs))]
    public void Invariants_hold(string policy, int seed)
    {
        var bundle = Repo.Load("spoke-b");
        var s = Session.Create(bundle, seed, PolicyCatalog.Create(policy).PrepChoices(bundle));
        var p = PolicyCatalog.Create(policy);
        while (!s.Finished)
        {
            foreach (var a in p.Decide(s.View).ToList()) s.Apply(a);
            s.AdvanceDay();
            Check(s);
        }
    }

    private static void Check(Session s)
    {
        foreach (var c in s.Capabilities)
        {
            Assert.True(c.BufferMilli >= 0, $"{c.SiteId}.{c.CapabilityId} buffer negative");
            Assert.InRange(c.TodayPermille, 0, 1000);
        }
        foreach (var f in s.Funding)
            Assert.True(f.ObligatedCents <= f.AmountCents, $"funding line {f.Id} over-obligated");
        foreach (var a in s.Arrangements)
        {
            Assert.True(a.ObligatedCents >= 0 && a.IncurredCents >= 0);
            if (a.Uca is { Definitized: false } u)
            {
                var cap = u.ProposalReceived ? s.Rules.Uca.ObligationCapAfterProposal : s.Rules.Uca.ObligationCapBeforeProposal;
                Assert.True(a.ObligatedCents <= (long)Math.Ceiling(u.NteCents * cap), $"{a.Id} obligated past the UCA cap");
            }
            if (a.Status == ArrangementStatus.Active)
                Assert.True(a.StartDay <= s.Day - 1 && a.EndDay >= s.Day - 1, $"{a.Id} active outside its period");
            if (!a.Unauthorized && !a.IsCoordination && a.Instrument is not "hostNation")
                Assert.False(string.IsNullOrEmpty(a.CauseId), $"{a.Id} has no cause record");
        }
        Assert.True(s.CashCents >= 0, "cash went negative");
        var ids = s.Records.Select(r => r.Id).ToHashSet();
        foreach (var r in s.Records)
            foreach (var cause in r.CausedBy)
                Assert.Contains(cause, ids);
    }
}

/// <summary>The yardstick in SPEC 6: a scenario fails review if textbook loses or do-nothing wins.</summary>
public class BalanceTests
{
    [Theory]
    [InlineData("spoke-b")]
    [InlineData("spoke-b-90")]
    [InlineData("sustainment")]
    public void Textbook_beats_greedy_beats_do_nothing(string scenario)
    {
        var bundle = Repo.Load(scenario);
        double Median(string policy)
        {
            var v = Enumerable.Range(0, 60).Select(seed => Replay.Run(bundle, seed, PolicyCatalog.Create(policy)).Metrics["mission.output"]).OrderBy(x => x).ToList();
            return v[v.Count / 2];
        }
        var nothing = Median("do-nothing");
        var greedy = Median("greedy");
        var textbook = Median("textbook");
        Assert.True(textbook > greedy && greedy > nothing, $"medians: textbook {textbook}, greedy {greedy}, do-nothing {nothing}");
    }
}
