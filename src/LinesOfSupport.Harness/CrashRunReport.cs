using System.Globalization;
using System.Text.Json;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Policies;

namespace LinesOfSupport.Harness;

internal sealed record CrashRunGroup(string Scenario, string RulesVersion, string Policy, int Games, int Passed, int Failed,
    long FirstSeed, long LastSeed, int DaysChecked, double? MissionMin, double? MissionMedian, double? MissionMax);
internal sealed record CrashRunFailure(string Scenario, string RulesVersion, string Policy, long Seed, int Day,
    string Phase, string Kind, string Message);

internal sealed record CrashRunReport(int Games, int Passed, int Failed, IReadOnlyList<CrashRunGroup> Results,
    IReadOnlyList<CrashRunFailure> Failures)
{
    internal static CrashRunReport Create(IReadOnlyList<LoadedScenario> scenarios, int count, int firstSeed,
        Func<string, IPolicy> policyFactory)
    {
        // Load and validate all bundles before playing. Never mistake a missing scenario for a completed matrix.
        var groups = scenarios.Select(ScenarioLoader.LoadBundle)
            .SelectMany(bundle => PolicyCatalog.Names.Select(policy => new Group(bundle, policy))).ToArray();
        var failures = new List<CrashRunFailure>();
        for (var index = 0; index < count; index++)
        {
            var group = groups[index % groups.Length];
            var seed = (long)firstSeed + index / groups.Length;
            group.Games++;
            group.FirstSeed ??= seed; group.LastSeed = seed;
            Session? session = null;
            var day = 1; var phase = "initialize";
            try
            {
                var policy = policyFactory(group.Policy);
                session = Session.Create(group.Bundle, seed, policy.PrepChoices(group.Bundle));
                CrashRunChecks.CheckMetrics(session.Metrics);
                while (!session.Finished)
                {
                    day = session.Day; phase = "decisions";
                    foreach (var action in policy.Decide(session.View).ToList()) session.Apply(action);
                    phase = "advance"; session.AdvanceDay();
                    phase = "checks"; CrashRunChecks.Check(session);
                    group.DaysChecked++;
                }
                group.Missions.Add(session.Metrics["mission.output"]);
            }
            catch (Exception error)
            {
                failures.Add(new CrashRunFailure(group.Bundle.Scenario.Id, group.Bundle.Rules.RulesVersion,
                    group.Policy, seed, day, phase, error is CrashCheckException check ? check.Kind : "exception",
                    $"{error.GetType().Name}: {error.Message}"));
            }
        }
        var rows = groups.Select(g => g.Summary()).ToArray();
        return new CrashRunReport(count, count - failures.Count, failures.Count, rows, failures);
    }

    internal string ToJson() => JsonSerializer.Serialize(this, new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true
    });

    internal void WriteText(TextWriter output)
    {
        output.WriteLine($"Crash run: {Games} games; {Passed} passed; {Failed} failed. Checks after every simulated day.");
        output.WriteLine("| Scenario | Rules | Policy | Games | Passed | Failed | First seed | Last seed | Days checked | Mission min | Median | Max |");
        output.WriteLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |");
        static string Number(double? value) => value?.ToString("0.######", CultureInfo.InvariantCulture) ?? "—";
        foreach (var row in Results)
            output.WriteLine($"| {row.Scenario} | {row.RulesVersion} | {row.Policy} | {row.Games} | {row.Passed} | {row.Failed} | {row.FirstSeed} | {row.LastSeed} | {row.DaysChecked} | {Number(row.MissionMin)} | {Number(row.MissionMedian)} | {Number(row.MissionMax)} |");
        foreach (var failure in Failures)
            output.WriteLine($"FAIL {failure.Scenario} / {failure.Policy} / seed {failure.Seed} / day {failure.Day} / {failure.Phase} / {failure.Kind}: {failure.Message}");
    }

    private sealed class Group(ScenarioBundle bundle, string policy)
    {
        internal ScenarioBundle Bundle { get; } = bundle;
        internal string Policy { get; } = policy;
        internal int Games, DaysChecked;
        internal long? FirstSeed;
        internal long LastSeed;
        internal List<double> Missions { get; } = [];
        internal CrashRunGroup Summary()
        {
            Missions.Sort();
            var middle = Missions.Count / 2;
            double? median = Missions.Count == 0 ? null : Missions.Count % 2 == 1 ? Missions[middle] :
                Missions[middle - 1] / 2 + Missions[middle] / 2;
            return new(Bundle.Scenario.Id, Bundle.Rules.RulesVersion, Policy, Games, Missions.Count, Games - Missions.Count,
                FirstSeed ?? 0, LastSeed, DaysChecked, Missions.Count == 0 ? null : Missions[0], median,
                Missions.Count == 0 ? null : Missions[^1]);
        }
    }
}

internal sealed class CrashCheckException(string kind, string message) : Exception(message)
{
    internal string Kind { get; } = kind;
}

// Read-only instrumentation of ADR-0002 and the existing Sim InvariantTests. No state repair or simulation rules.
internal static class CrashRunChecks
{
    internal static void Check(Session session)
    {
        foreach (var capability in session.Capabilities)
        {
            Require(capability.BufferMilli >= 0, $"{capability.SiteId}.{capability.CapabilityId} buffer is negative");
            Require(capability.TodayPermille is >= 0 and <= 1000, $"{capability.SiteId}.{capability.CapabilityId} condition outside 0..1000");
        }
        foreach (var funding in session.Funding)
            Require(funding.ObligatedCents >= 0 && funding.ObligatedCents <= funding.AmountCents, $"funding {funding.Id} over-obligated or negative");
        foreach (var arrangement in session.Arrangements)
        {
            Require(arrangement.ObligatedCents >= 0 && arrangement.IncurredCents >= 0, $"{arrangement.Id} has negative costs");
            if (arrangement.Uca is { Definitized: false } uca)
            {
                var cap = uca.ProposalReceived ? session.Rules.Uca.ObligationCapAfterProposal : session.Rules.Uca.ObligationCapBeforeProposal;
                Require(arrangement.ObligatedCents <= (long)Math.Ceiling(uca.NteCents * cap), $"{arrangement.Id} exceeds its UCA obligation cap");
            }
            if (arrangement.Status == ArrangementStatus.Active)
                Require(arrangement.StartDay <= session.Day - 1 && arrangement.EndDay >= session.Day - 1, $"{arrangement.Id} active outside its period");
            if (!arrangement.Unauthorized && !arrangement.IsCoordination && arrangement.Instrument != "hostNation")
                Require(!string.IsNullOrEmpty(arrangement.CauseId), $"{arrangement.Id} has no cause record");
        }
        Require(session.CashCents >= 0, "cash is negative");
        var ids = session.Records.Select(r => r.Id).ToHashSet(StringComparer.Ordinal);
        Require(ids.Count == session.Records.Count, "duplicate cause record IDs");
        foreach (var record in session.Records)
            foreach (var cause in record.CausedBy)
                Require(ids.Contains(cause), $"{record.Id} references missing cause {cause}");
        CheckMetrics(session.Metrics);
    }

    internal static void CheckMetrics(IReadOnlyDictionary<string, double> metrics)
    {
        foreach (var (name, value) in metrics)
            if (!double.IsFinite(value)) throw new CrashCheckException("non-finite", $"metric {name} is {value}");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new CrashCheckException("invariant", message);
    }
}
