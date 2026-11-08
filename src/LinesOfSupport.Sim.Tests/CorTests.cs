using System.Text.Json.Nodes;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Policies;

namespace LinesOfSupport.Sim.Tests;

/// <summary>M3-04: COR designation, report lag and fidelity by COR quality, the no-COR path, and the COR rotating home.</summary>
public class CorTests
{
    private static string Golden(string file) => Path.Combine(Repo.Scenario("spoke-b-90"), "golden", file);

    private static ScenarioBundle Variant(Action<JsonNode> change)
    {
        var root = JsonNode.Parse(File.ReadAllText(Path.Combine(Repo.Scenario("spoke-b-90"), "scenario.json")))!;
        change(root);
        var dir = Path.Combine(Path.GetTempPath(), $"los-cor-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "scenario.json"), root.ToJsonString());
        return ScenarioBundle.Load(dir, Path.Combine(Repo.Root, "scenarios", "rules"));
    }

    private static void NoRotation(JsonNode root)
    {
        var events = root["events"]!.AsArray();
        events.Remove(events.First(e => (string?)e!["id"] == "corRotates"));
    }

    private static IEnumerable<CauseRecord> Failures(Session s, string arrangement)
        => s.Records.Where(r => r.Kind == RecordKind.VendorFailure && r.Subject == arrangement);

    [Fact]
    public void A_good_COR_reports_every_failure_the_same_day_and_the_relief_reports_late_and_not_always()
    {
        var log = InputLog.Load(Golden("approach1-as-written.json"));
        var sawMiss = false;
        for (var seed = 1; seed <= 12; seed++)
        {
            var s = Replay.Run(Repo.Load("spoke-b-90"), log with { Seed = seed });
            var rotated = s.Cors.Single(c => c.Id == "corReyes").RotatedDay!.Value;
            Assert.InRange(rotated, 44, 60);
            Assert.Equal("poor", s.Cors.Single(c => c.Id == "corReyes").Quality);
            foreach (var f in Failures(s, "gens"))
            {
                var reports = s.CorReports.Where(r => r.ArrangementId == "gens" && r.FailureDay == f.Day).ToList();
                if (f.Day < rotated)
                {
                    var r = Assert.Single(reports);
                    Assert.Equal(f.Day, r.ArriveDay);
                    Assert.Equal("corReyes", r.CorId);
                }
                else if (reports.Count == 0) sawMiss = true;
                else Assert.Equal(f.Day + 4, Assert.Single(reports).ArriveDay);
            }
        }
        Assert.True(sawMiss, "a poor relief should miss some failures across twelve seeds");
    }

    [Fact]
    public void A_fair_COR_reports_two_days_late_and_about_four_failures_in_five()
    {
        var bundle = Variant(root =>
        {
            NoRotation(root);
            root["cors"]!.AsArray().First(c => (string?)c!["id"] == "corReyes")!["quality"] = "fair";
        });
        var log = InputLog.Load(Golden("approach1-as-written.json"));
        int failures = 0, reported = 0;
        for (var seed = 1; seed <= 40; seed++)
        {
            var s = Replay.Run(bundle, log with { Seed = seed });
            foreach (var f in Failures(s, "gens"))
            {
                failures++;
                if (s.CorReports.SingleOrDefault(r => r.ArrangementId == "gens" && r.FailureDay == f.Day) is { } r)
                {
                    reported++;
                    Assert.Equal(f.Day + 2, r.ArriveDay);
                }
            }
        }
        Assert.True(failures > 100, $"only {failures} failures to measure");
        Assert.InRange(reported * 1000 / failures, 700, 900);
    }

    [Fact]
    public void With_no_COR_only_failures_that_show_as_a_shortfall_reach_the_office_and_the_unit_reports_them_late()
    {
        var log = InputLog.Load(Golden("approach1-no-cor.json"));
        var s = Replay.Run(Repo.Load("spoke-b-90"), log);
        var reports = s.CorReports.Where(r => r.ArrangementId == "gens").ToList();
        Assert.NotEmpty(reports);
        Assert.All(reports, r => Assert.Null(r.CorId));
        Assert.All(reports, r => Assert.Equal(r.FailureDay + 3, r.ArriveDay));
        foreach (var f in Failures(s, "gens"))
        {
            var cs = s.Capabilities.Single(c => c.SiteId == "spokeBSite" && c.CapabilityId == "power");
            var opens = s.Scenario.Sites.Single(x => x.Id == "spokeBSite").OpensDay;
            var shortfall = cs.History[f.Day - opens] < 1000;
            Assert.Equal(shortfall, reports.Any(r => r.FailureDay == f.Day));
        }
        Assert.Contains(s.Records, r => r.Kind == RecordKind.CorReport && r.Text.StartsWith("Unit report from Spoke B") && r.Text.Contains("gens has no COR"));
    }

    [Fact]
    public void The_board_hides_a_failure_until_its_report_arrives_but_the_tile_shows_the_mission_effect()
    {
        var bundle = Repo.Load("spoke-b-90");
        var log = InputLog.Load(Golden("approach1-no-cor.json"));
        var full = Replay.Run(bundle, log);
        var first = full.CorReports.First(r => r.ArrangementId == "gens" && r.RecordId is not null);

        var onFailure = Replay.Run(bundle, log, first.FailureDay).View;
        var board = onFailure.MorningBoard();
        Assert.DoesNotContain(board.Items, i => i.Text.StartsWith("gens had equipment down", StringComparison.Ordinal));
        Assert.Contains(board.Tiles, t => t.SiteId == "spokeBSite" && t.CapabilityId == "power" && t.TodayPermille < 1000);
        var hidden = onFailure.Records.Single(r => r.Kind == RecordKind.VendorFailure && r.Subject == "gens" && r.Day == first.FailureDay);
        Assert.False(onFailure.Known(hidden));

        var onArrival = Replay.Run(bundle, log, first.ArriveDay).View.MorningBoard();
        Assert.Contains(onArrival.Items, i => i.RecordId == first.RecordId && i.Text.StartsWith("Unit report from Spoke B"));
    }

    [Fact]
    public void Without_a_roster_failures_are_seen_the_same_day_and_a_COR_cannot_be_named()
    {
        var bundle = Variant(root => { root.AsObject().Remove("cors"); NoRotation(root); });
        var log = InputLog.Load(Golden("approach1-no-cor.json"));
        var s = Replay.Run(bundle, log);
        Assert.False(s.View.CorModeled);
        Assert.Empty(s.CorReports);
        var pi1 = s.PerformanceIssues.First();
        Assert.Equal(pi1.FailureDays.Max(), pi1.OpenedDay);          // seen the day the second failure happens
        Assert.All(s.Records.Where(r => r.Kind == RecordKind.VendorFailure), r => Assert.True(s.View.Known(r)));

        var fresh = Session.Create(bundle, 1);
        var award = (AwardAction)InputLog.Load(Golden("approach1-as-written.json")).Actions[0];
        Assert.Contains("this scenario has no COR roster", fresh.View.Preview(award).HardStops);
    }

    [Fact]
    public void Reported_days_open_the_performance_issue_so_a_missing_COR_delays_the_decision()
    {
        var bundle = Repo.Load("spoke-b-90");
        var withCor = Replay.Run(bundle, InputLog.Load(Golden("approach1-as-written.json")));
        var noCor = Replay.Run(bundle, InputLog.Load(Golden("approach1-no-cor.json")));
        var a = withCor.PerformanceIssues.First();
        var b = noCor.PerformanceIssues.First();
        Assert.Equal(a.FailureDays, b.FailureDays);
        Assert.Equal(a.FailureDays.Max(), a.OpenedDay);
        Assert.Equal(b.FailureDays.Max() + 3, b.OpenedDay);
        Assert.Contains("(unit reports; no COR)", noCor.Records.Single(r => r.Subject == b.Id && r.Kind == RecordKind.Inbox).Text);
        Assert.Contains("(COR SSG Reyes)", withCor.Records.Single(r => r.Subject == a.Id && r.Kind == RecordKind.Inbox).Text);
        // The debrief says how the office heard.
        Assert.Contains(noCor.View.Debrief().Gaps.SelectMany(g => g.Factors), f => f.Text.Contains("With no COR, the unit reported it on day"));
    }

    [Fact]
    public void COR_stops_and_designation()
    {
        var bundle = Repo.Load("spoke-b-90");
        var s = Session.Create(bundle, 1);
        var award = (AwardAction)InputLog.Load(Golden("approach1-as-written.json")).Actions[0];
        Assert.Empty(s.View.Preview(award).HardStops);
        Assert.Contains(s.View.Preview(award with { Cor = null }).SoftGaps, g => g.StartsWith("No COR named"));
        Assert.Contains(s.View.Preview(award with { Cor = "corOkafor" }).SoftGaps, g => g.StartsWith("COR SGT Okafor is fair"));
        Assert.DoesNotContain(s.View.Preview(award).SoftGaps, g => g.Contains("COR"));
        Assert.Contains("COR nobody is not on the roster", s.View.Preview(award with { Cor = "nobody" }).HardStops);

        var log = InputLog.Load(Golden("approach1-as-written.json"));
        var mid = Replay.Run(bundle, log, 20);
        var gens = mid.Arrangements.Single(a => a.Id == "gens");
        Assert.Equal("corReyes", gens.CorId);
        Assert.False(mid.Apply(new DesignateCorAction { Arrangement = "gens", Cor = "corReyes" }).Accepted);
        Assert.False(mid.Apply(new DesignateCorAction { Arrangement = "hubGrid", Cor = "corOkafor" }).Accepted);   // host-nation support
        Assert.False(mid.Apply(new DesignateCorAction { Arrangement = "gens", Cor = "nobody" }).Accepted);
        Assert.True(mid.Apply(new DesignateCorAction { Arrangement = "gens", Cor = "corOkafor" }).Accepted);
        Assert.Equal("corOkafor", gens.CorId);
        Assert.Contains(mid.Records, r => r.Kind == RecordKind.Action && r.Text.StartsWith("SGT Okafor designated COR on gens, replacing SSG Reyes"));
    }

    [Fact]
    public void A_cash_buy_takes_no_COR()
    {
        var bundle = Repo.Load("spoke-b-90");
        var s = Session.Create(bundle, 1, ["cashAndPayingAgent"]);
        var cash = new AwardAction
        {
            Id = "cash", Instrument = "sf44", Vendor = "vendorA", Site = "spokeBSite", Capability = "power",
            Lines = ["generation"], Quantity = 10, DurationDays = 3, Cor = "corReyes",
        };
        Assert.Contains(s.View.Preview(cash).HardStops, h => h.StartsWith("a COR is designated only on a contract with written terms"));
    }

    [Fact]
    public void The_textbook_names_the_best_COR_and_redesignates_when_the_relief_takes_over()
    {
        var s = Replay.Run(Repo.Load("spoke-b-90"), 1, PolicyCatalog.Create("textbook"));
        var written = s.Arrangements.Where(a => a.CreatedDay > 0 && s.Rules.Instruments[a.Instrument].WrittenTerms && a.Status != ArrangementStatus.Declined).ToList();
        Assert.NotEmpty(written);
        Assert.All(written, a => Assert.NotNull(a.CorId));
        var rotated = s.Cors.Single(c => c.Id == "corReyes").RotatedDay!.Value;
        Assert.Contains(s.Records, r => r.Kind == RecordKind.Action && r.Day == rotated + 1 && r.Text.StartsWith("SGT Okafor designated COR"));
        var greedy = Replay.Run(Repo.Load("spoke-b-90"), 1, PolicyCatalog.Create("greedy"));
        Assert.All(greedy.Arrangements.Where(a => a.CreatedDay > 0), a => Assert.Null(a.CorId));
    }

    [Fact]
    public void Bundle_validation_checks_COR_references()
    {
        Assert.Throws<InvalidDataException>(() => Variant(root => root["cors"]![0]!["quality"] = "excellent"));
        Assert.Throws<InvalidDataException>(() => Variant(root =>
            root["events"]!.AsArray().First(e => (string?)e!["id"] == "corRotates")!["effects"]![0]!["target"] = "nobody"));
        Assert.Throws<InvalidDataException>(() => Variant(root => root["cors"]!.AsArray().Add(JsonNode.Parse("""{"id":"corReyes","name":"dup","quality":"good"}"""))));
    }
}
