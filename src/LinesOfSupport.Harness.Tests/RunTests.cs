using System.Text.Json.Nodes;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Policies;
using Xunit;

namespace LinesOfSupport.Harness.Tests;

public sealed class RunTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "los-run-" + Guid.NewGuid().ToString("N"));
    private static string Fixture(string scenario) => Path.Combine(AppContext.BaseDirectory, "fixtures", "scenarios", scenario);

    public static IEnumerable<object[]> Logs() => new[] { "spoke-b", "spoke-b-90" }
        .SelectMany(s => Directory.GetFiles(Path.Combine(Fixture(s), "golden"), "approach*.json")
            .Order(StringComparer.Ordinal).Select(p => new object[] { s, Path.GetFileName(p) }));

    [Theory]
    [MemberData(nameof(Logs))]
    public void ReplayMatchesEveryReviewedApproachReportWithoutChangingTheLog(string scenario, string file)
    {
        var log = Path.Combine(Fixture(scenario), "golden", file);
        var before = File.ReadAllBytes(log);
        var result = Run(Fixture(scenario), "--log", log);
        Assert.Equal(0, result.Code);
        Assert.Empty(result.Error);
        var golden = File.ReadAllText(Path.ChangeExtension(log, ".expected.txt"));
        Assert.Equal(Normalize(golden), Normalize(result.Output));
        Assert.Equal(before, File.ReadAllBytes(log));
        Assert.DoesNotContain("PLACEHOLDER", result.Output);
    }

    [Theory]
    [InlineData("do-nothing", 0)]
    [InlineData("greedy", -12)]
    [InlineData("textbook", 42)]
    public void PolicyRunMatchesSimAndIsRepeatable(string policy, int seed)
    {
        var folder = Fixture("spoke-b");
        var result = Run(Path.Combine(folder, "scenario.json"), "--policy", policy, "--seed", seed.ToString());
        var bundle = ScenarioBundle.Load(folder);
        var expected = SessionReport.ToText(Replay.Run(bundle, seed, PolicyCatalog.Create(policy)), $"{bundle.Scenario.Title} / policy {policy}");
        Assert.Equal(0, result.Code);
        Assert.Equal(expected, result.Output);
        Assert.Equal(result, Run(folder, "--policy", policy, "--seed", seed.ToString()));
    }

    [Fact]
    public void DefaultsAreSeedZeroAndDoNothing()
    {
        Assert.Equal(Run(Fixture("spoke-b"), "--policy", "do-nothing", "--seed", "0"), Run(Fixture("spoke-b")));
    }

    [Theory]
    [InlineData("{")]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("{\"scenario\":\"spoke-b\",\"rulesVersion\":\"2025-10\",\"actions\":[{\"kind\":\"unknown\",\"day\":1}]}")]
    [InlineData("{\"scenario\":\"spoke-b\",\"rulesVersion\":\"2025-10\",\"actions\":[{\"day\":1}]}")]
    public void InvalidLogIsADataErrorWithoutPartialOutput(string json)
    {
        Directory.CreateDirectory(root);
        var log = Path.Combine(root, "bad.json");
        File.WriteAllText(log, json);
        var result = Run(Fixture("spoke-b"), "--log", log);
        Assert.Equal(1, result.Code);
        Assert.StartsWith("Error:", result.Error);
        Assert.Empty(result.Output);
    }

    [Theory]
    [InlineData("scenario", "other")]
    [InlineData("rulesVersion", "other")]
    public void LogMustMatchScenarioAndRules(string key, string value)
    {
        Directory.CreateDirectory(root);
        var source = Path.Combine(Fixture("spoke-b"), "golden", "approach1-as-written.json");
        var data = JsonNode.Parse(File.ReadAllText(source))!;
        data[key] = value;
        var log = Path.Combine(root, "mismatch.json");
        File.WriteAllText(log, data.ToJsonString());
        var result = Run(Fixture("spoke-b"), "--log", log);
        Assert.Equal(1, result.Code);
        Assert.Empty(result.Output);
    }

    [Fact]
    public void MissingLogAndConflictingReplayOptionsAreExplicitErrors()
    {
        Assert.Equal(1, Run(Fixture("spoke-b"), "--log", Path.Combine(root, "missing.json")).Code);
        Assert.Equal(2, Run(Fixture("spoke-b"), "--log", "unused.json", "--seed", "1").Code);
        Assert.Equal(2, Run(Fixture("spoke-b"), "--log", "unused.json", "--policy", "textbook").Code);
    }

    private static CommandResult Run(string scenario, params string[] options)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var code = HarnessApp.Run(["run", scenario, .. options], output, error);
        return new(code, output.ToString(), error.ToString());
    }

    private static string Normalize(string text) => text.Replace("\r\n", "\n");
    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, recursive: true); }
    private sealed record CommandResult(int Code, string Output, string Error);
}
