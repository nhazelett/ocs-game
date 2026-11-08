using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Policies;
using Xunit;

namespace LinesOfSupport.Harness.Tests;

public sealed class BatchTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "los-batch-" + Guid.NewGuid().ToString("N"));
    private string ScenarioPath => Path.Combine(root, "scenarios", "_stub", "scenario.json");

    public BatchTests()
    {
        var fixtures = Path.Combine(AppContext.BaseDirectory, "fixtures");
        foreach (var file in Directory.GetFiles(fixtures, "*.json", SearchOption.AllDirectories))
        {
            var target = Path.Combine(root, Path.GetRelativePath(fixtures, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(4)]
    public void JsonSpreadsMatchIndependentSimRunsForEveryMetric(int count)
    {
        // A real authorable Sim variation makes the day metric differ by seed, so mean cannot stand in for median.
        var fixture = JsonNode.Parse(File.ReadAllText(ScenarioPath))!;
        fixture["variation"] = JsonNode.Parse("""[{"path":"/days","kind":"intRange","min":30,"max":90}]""");
        File.WriteAllText(ScenarioPath, fixture.ToJsonString());
        var result = Run(ScenarioPath, "--json", "--seeds", count.ToString(CultureInfo.InvariantCulture), "--policies", "textbook,do-nothing");
        Assert.Equal(0, result.Code);
        Assert.Empty(result.Error);
        using var json = JsonDocument.Parse(result.Output);
        var data = json.RootElement;
        Assert.Equal("simulated", data.GetProperty("simulationStatus").GetString());
        Assert.Equal("stub", data.GetProperty("scenario").GetString());
        Assert.Equal("2025-10", data.GetProperty("rulesVersion").GetString());
        Assert.Equal(0, data.GetProperty("firstSeed").GetInt32());
        Assert.Equal(count - 1, data.GetProperty("lastSeed").GetInt32());
        var rows = data.GetProperty("results").EnumerateArray().ToArray();
        var bundle = ScenarioBundle.Load(ScenarioPath);
        var index = 0;
        foreach (var policy in new[] { "textbook", "do-nothing" })
        {
            var sessions = Enumerable.Range(0, count).Select(seed => Replay.Run(bundle, seed, PolicyCatalog.Create(policy))).ToArray();
            foreach (var metric in sessions[0].Metrics.Keys.Order(StringComparer.Ordinal))
            {
                var samples = sessions.Select(s => s.Metrics[metric]).Order().ToArray();
                var median = count % 2 == 1 ? samples[count / 2] : (samples[count / 2 - 1] + samples[count / 2]) / 2;
                var row = rows[index++];
                Assert.Equal(policy, row.GetProperty("policy").GetString());
                Assert.Equal(metric, row.GetProperty("metric").GetString());
                Assert.Equal(count, row.GetProperty("runs").GetInt32());
                Assert.Equal(samples[0], row.GetProperty("min").GetDouble());
                Assert.Equal(median, row.GetProperty("median").GetDouble());
                Assert.Equal(samples[^1], row.GetProperty("max").GetDouble());
            }
        }
        Assert.Equal(index, rows.Length);
        Assert.Equal(result, Run(ScenarioPath, "--json", "--seeds", count.ToString(CultureInfo.InvariantCulture), "--policies", "textbook,do-nothing"));
    }

    [Fact]
    public void DefaultBatchRunsTwoHundredSeedsForAllSimPolicies()
    {
        var result = Run(Path.GetDirectoryName(ScenarioPath)!, "--json");
        Assert.Equal(0, result.Code);
        using var json = JsonDocument.Parse(result.Output);
        Assert.Equal(199, json.RootElement.GetProperty("lastSeed").GetInt32());
        var rows = json.RootElement.GetProperty("results").EnumerateArray().ToArray();
        Assert.Equal(PolicyCatalog.Names, rows.Select(r => r.GetProperty("policy").GetString()!).Distinct().ToArray());
        Assert.All(rows, r => Assert.Equal(200, r.GetProperty("runs").GetInt32()));
        foreach (var policy in PolicyCatalog.Names)
        {
            var day = rows.Single(r => r.GetProperty("policy").GetString() == policy && r.GetProperty("metric").GetString() == "day");
            Assert.Equal(30, day.GetProperty("min").GetDouble());
            Assert.Equal(30, day.GetProperty("median").GetDouble());
            Assert.Equal(30, day.GetProperty("max").GetDouble());
            Assert.Contains(rows, r => r.GetProperty("policy").GetString() == policy && r.GetProperty("metric").GetString() == "mission.output");
        }
    }

    [Fact]
    public void TextTableIsReadableInvariantAndLabelsRealSimulation()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            var result = Run(ScenarioPath, "--seeds", "3", "--policies", "textbook");
            Assert.Equal(0, result.Code);
            Assert.Contains("| Policy | Metric | Runs | Min | Median | Max |", result.Output);
            Assert.Contains("| textbook | day | 3 | 30 | 30 | 30 |", result.Output);
            Assert.Contains("| textbook | mission.output | 3 |", result.Output);
            Assert.Contains("raw Sim metric units", result.Output);
            Assert.DoesNotContain("PLACEHOLDER", result.Output);
            Assert.DoesNotContain("not simulated", result.Output);
            var missionRow = result.Output.Split('\n').Single(line => line.StartsWith("| textbook | mission.output |", StringComparison.Ordinal));
            Assert.DoesNotContain(',', missionRow);
            Assert.Empty(result.Error);
        }
        finally { CultureInfo.CurrentCulture = original; }
    }

    [Fact]
    public void NestedScenarioUsesTheSameSharedRulesDirectoryAsValidation()
    {
        var nested = Path.Combine(root, "scenarios", "group", "_stub");
        Directory.CreateDirectory(Path.GetDirectoryName(nested)!);
        Directory.Move(Path.GetDirectoryName(ScenarioPath)!, nested);
        var result = Run(nested, "--seeds", "1", "--policies", "do-nothing", "--json");
        Assert.Equal(0, result.Code);
        using var json = JsonDocument.Parse(result.Output);
        Assert.Equal("2025-10", json.RootElement.GetProperty("rulesVersion").GetString());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InvalidScenarioProducesNoPartialResults(bool json)
    {
        var data = JsonNode.Parse(File.ReadAllText(ScenarioPath))!;
        data["sites"]![0]!["island"] = "missing";
        File.WriteAllText(ScenarioPath, data.ToJsonString());
        var args = new List<string> { "--seeds", "1" };
        if (json) args.Add("--json");
        var result = Run(ScenarioPath, args.ToArray());
        Assert.Equal(1, result.Code);
        Assert.Empty(result.Output);
        Assert.Contains("missing", result.Error);
    }

    [Fact]
    public void NoncanonicalFileCannotValidateOneFileThenSimulateAnother()
    {
        var other = Path.Combine(Path.GetDirectoryName(ScenarioPath)!, "other.json");
        File.Copy(ScenarioPath, other);
        var result = Run(other, "--json", "--seeds", "1");
        Assert.Equal(1, result.Code);
        Assert.Empty(result.Output);
        Assert.Contains("requires a scenario.json", result.Error);
    }

    [Fact]
    public void JsonIsAValueLessBatchFlagOnly()
    {
        Assert.Equal(2, Run(ScenarioPath, "--json", "true").Code);
        Assert.Equal(2, Run(ScenarioPath, "--json", "--json").Code);
        using var output = new StringWriter();
        using var error = new StringWriter();
        Assert.Equal(2, HarnessApp.Run(["run", ScenarioPath, "--json"], output, error));
    }

    private static CommandResult Run(string scenario, params string[] options)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var code = HarnessApp.Run(["batch", scenario, .. options], output, error);
        return new CommandResult(code, output.ToString(), error.ToString());
    }

    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }

    private sealed record CommandResult(int Code, string Output, string Error);
}
