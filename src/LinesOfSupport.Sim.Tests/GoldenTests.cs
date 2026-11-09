using LinesOfSupport.Sim.Policies;

namespace LinesOfSupport.Sim.Tests;

/// <summary>
/// Golden files: each Spoke B approach log, and each policy at seed 1, must reproduce its reviewed report exactly.
/// A rule change that moves one fails here on purpose. To accept a reviewed change, run the tests with
/// UPDATE_GOLDEN=1 and commit the regenerated *.expected.txt files with the reason in the PR.
/// </summary>
public class GoldenTests
{
    private static readonly string[] Scenarios = ["spoke-b", "spoke-b-90", "sustainment"];

    public static IEnumerable<object[]> ApproachLogs()
        => Scenarios.SelectMany(sc => Directory.GetFiles(Path.Combine(Repo.Scenario(sc), "golden"), "approach*.json")
                    .OrderBy(p => p, StringComparer.Ordinal).Select(p => new object[] { sc, Path.GetFileName(p) }));

    public static IEnumerable<object[]> PolicyRuns()
        => Scenarios.SelectMany(sc => new[] { "do-nothing", "greedy", "textbook" }.Select(p => new object[] { sc, p }));

    [Theory]
    [MemberData(nameof(ApproachLogs))]
    public void Approach_logs_match_their_reviewed_reports(string scenario, string file)
    {
        var bundle = Repo.Load(scenario);
        var path = Path.Combine(Repo.Scenario(scenario), "golden", file);
        var log = InputLog.Load(path);
        var session = Replay.Run(bundle, log);
        var title = $"{bundle.Scenario.Title} / {Path.GetFileNameWithoutExtension(file)}\n{log.Note}";
        Check(path.Replace(".json", ".expected.txt"), SessionReport.ToText(session, title));
    }

    [Theory]
    [MemberData(nameof(PolicyRuns))]
    public void Policies_at_seed_1_match_their_reviewed_reports(string scenario, string policy)
    {
        var bundle = Repo.Load(scenario);
        var session = Replay.Run(bundle, 1, PolicyCatalog.Create(policy));
        Check(Path.Combine(Repo.Scenario(scenario), "golden", $"policy-{policy}-seed1.expected.txt"),
              SessionReport.ToText(session, $"{bundle.Scenario.Title} / policy {policy}"));
    }

    private static void Check(string expectedPath, string actual)
    {
        actual = actual.Replace("\r\n", "\n");
        if (Environment.GetEnvironmentVariable("UPDATE_GOLDEN") == "1" || !File.Exists(expectedPath))
        {
            File.WriteAllText(expectedPath, actual);
            if (Environment.GetEnvironmentVariable("UPDATE_GOLDEN") != "1")
                Assert.Fail($"{Path.GetFileName(expectedPath)} did not exist; wrote it. Review and commit it, then rerun.");
            return;
        }
        var expected = File.ReadAllText(expectedPath).Replace("\r\n", "\n");
        if (expected != actual)
        {
            var actualPath = expectedPath.Replace(".expected.txt", ".actual.txt");
            File.WriteAllText(actualPath, actual);
            Assert.Fail($"{Path.GetFileName(expectedPath)} changed. Diff it against {Path.GetFileName(actualPath)}; if the change is intended, rerun with UPDATE_GOLDEN=1.");
        }
    }
}
