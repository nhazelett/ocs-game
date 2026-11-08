using System.Text.Json;
using LinesOfSupport.Sim;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Policies;
using Xunit;

namespace LinesOfSupport.Harness.Tests;

public sealed class CrashRunTests
{
    private static string Fixture(string scenario) => Path.Combine(AppContext.BaseDirectory, "fixtures", "scenarios", scenario);
    private static string Both => Fixture("spoke-b") + "," + Fixture("spoke-b-90");

    [Fact]
    public void ExactGameCountCoversBothScenariosAllPoliciesAndIndependentSeeds()
    {
        var result = Run(Both, "--games", "13", "--first-seed", "-1", "--json");
        Assert.Equal(0, result.Code); Assert.Empty(result.Error);
        using var json = JsonDocument.Parse(result.Output);
        Assert.Equal(13, json.RootElement.GetProperty("games").GetInt32());
        Assert.Equal(13, json.RootElement.GetProperty("passed").GetInt32());
        Assert.Equal(0, json.RootElement.GetProperty("failed").GetInt32());
        Assert.Empty(json.RootElement.GetProperty("failures").EnumerateArray());
        var rows = json.RootElement.GetProperty("results").EnumerateArray().ToArray();
        Assert.Equal(6, rows.Length);
        for (var i = 0; i < rows.Length; i++)
        {
            var row = rows[i]; var scenario = i < 3 ? "spoke-b" : "spoke-b-90";
            var policy = PolicyCatalog.Names[i % 3]; var games = i == 0 ? 3 : 2;
            Assert.Equal(scenario, row.GetProperty("scenario").GetString());
            Assert.Equal("2025-10", row.GetProperty("rulesVersion").GetString());
            Assert.Equal(policy, row.GetProperty("policy").GetString());
            Assert.Equal(games, row.GetProperty("games").GetInt32());
            Assert.Equal(games, row.GetProperty("passed").GetInt32());
            Assert.Equal(0, row.GetProperty("failed").GetInt32());
            Assert.Equal(-1, row.GetProperty("firstSeed").GetInt64());
            Assert.Equal(games - 2, row.GetProperty("lastSeed").GetInt64());
            var bundle = ScenarioBundle.Load(Fixture(scenario));
            var samples = Enumerable.Range(-1, games).Select(seed => Replay.Run(bundle, seed, PolicyCatalog.Create(policy))).ToArray();
            Assert.Equal(samples.Sum(s => s.LastDay), row.GetProperty("daysChecked").GetInt32());
            var mission = samples.Select(s => s.Metrics["mission.output"]).Order().ToArray();
            Assert.Equal(mission[0], row.GetProperty("missionMin").GetDouble());
            Assert.Equal(games % 2 == 1 ? mission[games / 2] : mission[0] / 2 + mission[1] / 2, row.GetProperty("missionMedian").GetDouble());
            Assert.Equal(mission[^1], row.GetProperty("missionMax").GetDouble());
        }
        Assert.Equal(result, Run(Both, "--games", "13", "--first-seed", "-1", "--json"));
    }

    [Fact]
    public void TextReportIsASummaryTable()
    {
        var result = Run(Both, "--games", "6");
        Assert.Equal(0, result.Code);
        Assert.Contains("6 games; 6 passed; 0 failed", result.Output);
        Assert.Contains("| Scenario | Rules | Policy | Games | Passed | Failed |", result.Output);
        Assert.Contains("| spoke-b-90 | 2025-10 | textbook | 1 | 1 | 0 |", result.Output);
    }

    [Theory]
    [InlineData("--games", "0")]
    [InlineData("--games", "5")]
    [InlineData("--games", "NaN")]
    [InlineData("--first-seed", "NaN")]
    [InlineData("--policy", "greedy")]
    [InlineData("--seeds", "10")]
    public void InvalidOptionsAreUsageErrorsWithoutSuccessReport(string option, string value)
    {
        var result = Run(Both, option, value);
        Assert.Equal(2, result.Code); Assert.Empty(result.Output);
    }

    [Fact]
    public void MissingOrRepeatedScenariosAreRefusedBeforePlaying()
    {
        Assert.Equal(1, Run(Fixture("missing"), "--games", "6").Code);
        Assert.Equal(2, Run(Fixture("spoke-b") + "," + Fixture("spoke-b"), "--games", "6").Code);
        Assert.Equal(2, Run(Fixture("spoke-b") + ",", "--games", "6").Code);
    }

    [Fact]
    public void AnExceptionRecordsItsReproductionCoordinatesAndTheMatrixContinues()
    {
        var scenarios = new[] { ScenarioLoader.Load(Fixture("spoke-b")), ScenarioLoader.Load(Fixture("spoke-b-90")) };
        var report = CrashRunReport.Create(scenarios, 6, 23, name => name == "greedy" ? new ThrowingPolicy() : PolicyCatalog.Create(name));
        Assert.Equal(6, report.Games); Assert.Equal(4, report.Passed); Assert.Equal(2, report.Failed);
        Assert.All(report.Failures, failure =>
        {
            Assert.Equal("greedy", failure.Policy); Assert.Equal(23, failure.Seed);
            Assert.Equal(1, failure.Day); Assert.Equal("decisions", failure.Phase);
            Assert.Equal("exception", failure.Kind); Assert.Contains("deliberate crash", failure.Message);
        });
        Assert.Equal(new[] { "spoke-b", "spoke-b-90" }, report.Failures.Select(f => f.Scenario));
        Assert.All(report.Results.Where(r => r.Policy == "greedy"), r => Assert.Null(r.MissionMedian));
        Assert.Contains("FAIL spoke-b / greedy / seed 23 / day 1 / decisions / exception", Text(report));
        using var json = JsonDocument.Parse(report.ToJson());
        Assert.Equal(2, json.RootElement.GetProperty("failed").GetInt32());
    }

    [Theory]
    [InlineData("buffer")]
    [InlineData("condition")]
    [InlineData("funding")]
    [InlineData("cost")]
    [InlineData("cause")]
    public void InvariantCheckerRejectsCorruptedPublicState(string mutation)
    {
        var session = Session.Create(ScenarioBundle.Load(Fixture("spoke-b")), 1);
        session.AdvanceDay(); CrashRunChecks.Check(session);
        switch (mutation)
        {
            case "buffer": session.Capabilities[0].BufferMilli = -1; break;
            case "condition": session.Capabilities[0].TodayPermille = 1001; break;
            case "funding": session.Funding[0].ObligatedCents = session.Funding[0].AmountCents + 1; break;
            case "cost": session.Arrangements[0].IncurredCents = -1; break;
            case "cause":
                // Sim exposes its backing list as IReadOnlyList; corrupt only this isolated test session.
                ((List<CauseRecord>)session.Records).Add(new CauseRecord("bad", 1, RecordKind.Event, "test", "test", ["missing-cause"]));
                break;
        }
        var error = Assert.Throws<CrashCheckException>(() => CrashRunChecks.Check(session));
        Assert.Equal("invariant", error.Kind);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void NonFiniteMetricsAreFailures(double bad)
    {
        var error = Assert.Throws<CrashCheckException>(() => CrashRunChecks.CheckMetrics(new Dictionary<string, double> { ["test.metric"] = bad }));
        Assert.Equal("non-finite", error.Kind); Assert.Contains("test.metric", error.Message);
    }

    private sealed class ThrowingPolicy : IPolicy
    {
        public string Name => "greedy";
        public IEnumerable<string> PrepChoices(ScenarioBundle bundle) => [];
        public IEnumerable<PlayerAction> Decide(SessionView view) => throw new InvalidOperationException("deliberate crash");
    }

    private static string Text(CrashRunReport report) { using var writer = new StringWriter(); report.WriteText(writer); return writer.ToString(); }
    private static CommandResult Run(string scenarios, params string[] options)
    {
        using var output = new StringWriter(); using var error = new StringWriter();
        var code = HarnessApp.Run(["crash-run", scenarios, .. options], output, error);
        return new(code, output.ToString(), error.ToString());
    }
    private sealed record CommandResult(int Code, string Output, string Error);
}
