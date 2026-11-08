using LinesOfSupport.Sim.Policies;

namespace LinesOfSupport.Sim.Tests;

/// <summary>
/// Golden files: each Spoke B approach log, and each policy at seed 1, must reproduce its reviewed report exactly.
/// A rule change that moves one fails here on purpose. To accept a reviewed change, run the tests with
/// UPDATE_GOLDEN=1 and commit the regenerated *.expected.txt files with the reason in the PR.
/// </summary>
public class GoldenTests
{
    public static IEnumerable<object[]> ApproachLogs()
        => Directory.GetFiles(Path.Combine(Repo.Scenario("spoke-b"), "golden"), "approach*.json")
                    .OrderBy(p => p, StringComparer.Ordinal).Select(p => new object[] { Path.GetFileName(p) });

    [Theory]
    [MemberData(nameof(ApproachLogs))]
    public void Approach_logs_match_their_reviewed_reports(string file)
    {
        var bundle = Repo.Load("spoke-b");
        var path = Path.Combine(Repo.Scenario("spoke-b"), "golden", file);
        var log = InputLog.Load(path);
        var session = Replay.Run(bundle, log);
        var title = $"{bundle.Scenario.Title} / {Path.GetFileNameWithoutExtension(file)}\n{log.Note}";
        Check(path.Replace(".json", ".expected.txt"), SessionReport.ToText(session, title));
    }

    [Theory]
    [InlineData("do-nothing")]
    [InlineData("greedy")]
    [InlineData("textbook")]
    public void Policies_at_seed_1_match_their_reviewed_reports(string policy)
    {
        var bundle = Repo.Load("spoke-b");
        var session = Replay.Run(bundle, 1, PolicyCatalog.Create(policy));
        Check(Path.Combine(Repo.Scenario("spoke-b"), "golden", $"policy-{policy}-seed1.expected.txt"),
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
