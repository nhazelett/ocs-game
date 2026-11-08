using System.Globalization;
using System.Text.Json;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Policies;

namespace LinesOfSupport.Harness;

internal sealed record MetricSpread(string Policy, string Metric, int Runs, double Min, double Median, double Max);

// Presentation and aggregation only. Loading, policies, random seeds and day advance belong to Sim.
internal sealed record BatchReport(string Scenario, string RulesVersion, string SimulationStatus,
    int FirstSeed, int LastSeed, IReadOnlyList<MetricSpread> Results)
{
    internal static BatchReport Create(LoadedScenario scenario, int count, IReadOnlyList<string> policies)
    {
        if (!string.Equals(Path.GetFileName(scenario.FilePath), "scenario.json", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Batch simulation requires a scenario.json file, as required by the Sim loader.");
        var bundle = ScenarioBundle.Load(scenario.FilePath, scenario.RulesDirectory);
        var rows = new List<MetricSpread>();
        foreach (var policy in policies)
        {
            var samples = new SortedDictionary<string, List<double>>(StringComparer.Ordinal);
            for (var seed = 0; seed < count; seed++)
            {
                var session = Replay.Run(bundle, seed, PolicyCatalog.Create(policy));
                var metrics = session.Metrics;
                if (seed > 0 && !samples.Keys.SequenceEqual(metrics.Keys.Order(StringComparer.Ordinal)))
                    throw new InvalidDataException($"Sim metric keys changed across seeds for policy '{policy}'; no partial spread was reported.");
                foreach (var (metric, value) in metrics)
                {
                    if (!double.IsFinite(value))
                        throw new InvalidDataException($"Sim returned a non-finite value for '{metric}', policy '{policy}', seed {seed}.");
                    if (!samples.TryGetValue(metric, out var values))
                        samples.Add(metric, values = []);
                    values.Add(value);
                }
            }
            foreach (var (metric, values) in samples)
            {
                values.Sort();
                var middle = values.Count / 2;
                var median = values.Count % 2 == 1 ? values[middle] : values[middle - 1] / 2 + values[middle] / 2;
                rows.Add(new MetricSpread(policy, metric, values.Count, values[0], median, values[^1]));
            }
        }
        return new BatchReport(bundle.Scenario.Id, bundle.Rules.RulesVersion, "simulated", 0, count - 1, rows);
    }

    internal string ToJson() => JsonSerializer.Serialize(this, new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    });

    internal void WriteText(TextWriter output)
    {
        output.WriteLine($"Scenario: {Scenario}; rules: {RulesVersion}; seeds: {FirstSeed} through {LastSeed}.");
        output.WriteLine("Outcome spread (simulated; raw Sim metric units, including cents and mission fractions):");
        output.WriteLine("| Policy | Metric | Runs | Min | Median | Max |");
        output.WriteLine("| --- | --- | --- | --- | --- | --- |");
        static string Number(double value) => value.ToString("0.######", CultureInfo.InvariantCulture);
        foreach (var row in Results)
            output.WriteLine($"| {row.Policy} | {row.Metric} | {row.Runs} | {Number(row.Min)} | {Number(row.Median)} | {Number(row.Max)} |");
    }
}
