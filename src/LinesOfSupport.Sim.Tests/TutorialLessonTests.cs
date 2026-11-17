using System.Text.Json.Nodes;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Policies;

namespace LinesOfSupport.Sim.Tests;

/// <summary>
/// DESK-80 (docs/design/desk-redesign.md D31 and section 10 Sim notes): the tutorial ends when its lessons do, and its
/// second half asks something. The forklift lesson is priced in the tutorial only (Coastal Rentals reliable with its
/// drivers, the scenario's own missing-operator odds without them), the length is set from a 200-seed batch, and the
/// dining hall slips on days 11 and 12 so every play meets the decision on a contractor who slips.
/// </summary>
public class TutorialLessonTests
{
    private sealed record Game(Session Session, int LongestQuietRun);

    private static readonly Lazy<Dictionary<string, List<Game>>> Batch = new(() =>
    {
        var bundle = Repo.Load("tutorial");
        var games = new Dictionary<string, List<Game>>();
        foreach (var name in PolicyCatalog.Names)
        {
            var list = games[name] = [];
            for (var seed = 0; seed < 200; seed++)
            {
                var policy = PolicyCatalog.Create(name);
                var s = Session.Create(bundle, seed, policy.PrepChoices(bundle));
                int run = 0, longest = 0;
                while (!s.Finished)
                {
                    foreach (var a in policy.Decide(s.View).ToList()) s.Apply(a);
                    run = s.View.QuietDay() ? run + 1 : 0;
                    longest = Math.Max(longest, run);
                    s.AdvanceDay();
                }
                list.Add(new Game(s, longest));
            }
        }
        return games;
    });

    private static bool ForkliftsFailed(Session s)
    {
        var rented = s.Arrangements.Where(a => a.CapabilityId == "mhe" && a.Instrument != "hostNation").Select(a => a.Id).ToHashSet();
        return s.Records.Any(r => r.Kind == RecordKind.VendorFailure && rented.Contains(r.Subject ?? ""));
    }

    [Fact]
    public void The_tutorial_prices_its_own_forklift_lesson_and_nothing_else_moves()
    {
        Assert.Equal(16, Repo.Load("tutorial").Scenario.Days);
        Assert.Equal(100, Repo.Load("tutorial").Rules.Model.Failure.MissingOperatorPermillePerDay);
        Assert.Equal(0.99, Repo.Load("tutorial").Scenario.Vendors.Single(v => v.Id == "coastalRentals").Reliability);
        // Spoke B keeps the rules value until DESK-81 prices its own lesson.
        foreach (var id in new[] { "spoke-b", "spoke-b-90", "sustainment" })
            Assert.Equal(20, Repo.Load(id).Rules.Model.Failure.MissingOperatorPermillePerDay);
    }

    [Fact]
    public void Forklifts_without_drivers_fail_before_the_end_in_most_seeds_and_with_them_rarely()
    {
        // Greedy buys the request as written (no drivers); textbook asks SMSgt Dunn and adds them. 17 Nov study: 126 and
        // 118 seeds of 200 with a failure over 20 days. Built: 136 and 22 over 16 days.
        var without = Batch.Value["greedy"].Count(g => ForkliftsFailed(g.Session));
        var with = Batch.Value["textbook"].Count(g => ForkliftsFailed(g.Session));
        Assert.InRange(without, 120, 200);
        Assert.InRange(with, 0, 30);
    }

    [Fact]
    public void Every_play_meets_the_dining_slip_and_textbook_answers_it_with_a_cure_notice()
    {
        foreach (var (name, games) in Batch.Value)
            foreach (var g in games)
                Assert.True(g.Session.PerformanceIssues.Any(i => i.ArrangementId == "hubDining"), $"{name} seed {g.Session.Seed}: no dining issue");
        Assert.All(Batch.Value["textbook"], g =>
            Assert.Contains(g.Session.PerformanceIssues, i => i.ArrangementId == "hubDining" && i.CureNoticeDay is not null));
    }

    [Fact]
    public void Textbook_never_has_more_than_three_quiet_days_in_a_row()
    {
        var longest = Batch.Value["textbook"].Max(g => g.LongestQuietRun);
        Assert.InRange(longest, 0, 3);
    }

    private static ScenarioBundle Variant(string target, int days)
    {
        var root = JsonNode.Parse(File.ReadAllText(Path.Combine(Repo.Scenario("tutorial"), "scenario.json")))!;
        var ev = root["events"]!.AsArray().Single(e => (string?)e!["id"] == "diningSlips")!;
        ev["effects"]![0]!["target"] = target;
        ev["effects"]![0]!["days"] = days;
        var dir = Path.Combine(Path.GetTempPath(), $"los-slips-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "scenario.json"), root.ToJsonString());
        return ScenarioBundle.Load(dir, Path.Combine(Repo.Root, "scenarios", "rules"));
    }

    [Theory]
    [InlineData("nope", 2, "vendor.slips target nope is not an inherited arrangement")]
    [InlineData("hubDining", 0, "vendor.slips needs a positive number of days")]
    public void Bad_slip_events_are_refused(string target, int days, string message)
    {
        var ex = Assert.Throws<InvalidDataException>(() => Variant(target, days));
        Assert.Contains(message, ex.Message);
    }

    [Fact]
    public void A_slip_draws_what_the_day_would_have_drawn()
    {
        // The slips force two failures and two reports without taking a draw of their own: with the event removed, every
        // stream is where it was, so the rest of the game replays as before.
        var root = JsonNode.Parse(File.ReadAllText(Path.Combine(Repo.Scenario("tutorial"), "scenario.json")))!;
        var events = root["events"]!.AsArray();
        events.Remove(events.Single(e => (string?)e!["id"] == "diningSlips"));
        root["fixedSituations"]!.AsArray().Remove(root["fixedSituations"]!.AsArray().Single(x => (string?)x == "diningSlips"));
        var dir = Path.Combine(Path.GetTempPath(), $"los-noslips-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "scenario.json"), root.ToJsonString());
        var without = Replay.Run(ScenarioBundle.Load(dir, Path.Combine(Repo.Root, "scenarios", "rules")), 1, PolicyCatalog.Create("greedy"));
        var with = Replay.Run(Repo.Load("tutorial"), 1, PolicyCatalog.Create("greedy"));
        static IEnumerable<string> NotDining(Session s) => s.Records.Where(r => r.Subject != "hubDining" && !r.Text.Contains("dining", StringComparison.OrdinalIgnoreCase)
                                                                                && !r.Text.Contains("food service", StringComparison.OrdinalIgnoreCase)).Select(r => $"{r.Day} {r.Text}");
        Assert.Equal(NotDining(without), NotDining(with));
    }
}
