using System.Text.Json.Nodes;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Policies;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>M3-03: funding lines with fiscal-year ends, purpose, a late replacement and the debrief's funding gap (SPEC 4).</summary>
public class FundingTests
{
    private static FundingLineState Line(Session s, string id) => s.View.Funding.Single(f => f.Id == id);

    private static void PlayTo(Session s, int day) { while (s.Day < day && !s.Finished) s.AdvanceDay(); }

    /// <summary>A copy of a scenario with its JSON changed, loaded against the shared rules.</summary>
    private static ScenarioBundle Variant(string scenario, Action<JsonNode> change)
    {
        var root = JsonNode.Parse(File.ReadAllText(Path.Combine(Repo.Scenario(scenario), "scenario.json")))!;
        change(root);
        var dir = Path.Combine(Path.GetTempPath(), $"los-funding-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "scenario.json"), root.ToJsonString());
        return ScenarioBundle.Load(dir, Path.Combine(Repo.Root, "scenarios", "rules"));
    }

    private static AwardAction PowerPo(string line = "om1", int days = 10, int? start = null) => new()
    {
        Id = "po", Instrument = "purchaseOrder", Vendor = "vendorA", Site = "spokeBSite", Capability = "power",
        Lines = ["generation", "maintenance"], Quantity = 60, DurationDays = days, StartDay = start, FundingLine = line,
    };

    private static AwardAction FuelPo(string line = "om1") => new()
    {
        Id = "fuelPo", Instrument = "purchaseOrder", Vendor = "vendorE", Site = "hubSite", Capability = "fuel",
        Lines = ["delivery"], Quantity = 100, DurationDays = 10, FundingLine = line,
    };

    [Fact]
    public void The_replacement_line_is_released_late_on_a_seeded_day_with_notice_from_the_comptroller()
    {
        var releases = new HashSet<int>();
        var notices = new HashSet<int>();
        for (var seed = 1; seed <= 5; seed++)
        {
            var s = Session.Create(Repo.Load("spoke-b-90"), seed);
            Assert.Equal(76, Line(s, "om2").AvailableDay);
            Assert.Equal(76, Line(s, "om2").PlannedAvailableDay);
            PlayTo(s, 76);
            var om2 = Line(s, "om2");
            Assert.InRange(om2.AvailableDay, 79, 86);               // daysRange 3 to 10
            var late = s.Records.Single(r => r.Kind == RecordKind.Funding && r.Subject == "om2" && r.Text.Contains("will not be released"));
            Assert.InRange(late.Day, 62, 70);                        // the event window
            Assert.Contains($"expect it on day {om2.AvailableDay}", late.Text);
            Assert.Contains(late.CausedBy, id => s.Records.Any(r => r.Id == id && r.Kind == RecordKind.Event && r.Subject == "fyFundingLate"));
            // Nothing could pay on day 76 (om1 ended with day 75); the lapse is announced the evening before.
            Assert.False(Line(s, "om1").UsableOn(76));
            Assert.False(om2.UsableOn(76));
            Assert.Contains(s.Records, r => r.Kind == RecordKind.Funding && r.Day == 75 && r.Text.StartsWith("No funding line has money usable from day 76"));
            PlayTo(s, om2.AvailableDay);
            Assert.Contains(s.Records, r => r.Kind == RecordKind.Funding && r.Day == om2.AvailableDay - 1 && r.Text.StartsWith("Comptroller released the O&M line from day"));
            releases.Add(om2.AvailableDay);
            notices.Add(late.Day);
        }
        Assert.True(releases.Count >= 3 || notices.Count >= 3, "five seeds should give visibly different funding games");
    }

    [Fact]
    public void The_comptroller_warns_ten_days_before_the_fiscal_year_end_and_records_the_expiry()
    {
        var s = Session.Create(Repo.Load("spoke-b-90"), 1);
        PlayTo(s, 76);
        var warn = s.Records.Single(r => r.Kind == RecordKind.Funding && r.Subject == "om1" && r.Text.Contains("still unobligated"));
        Assert.Equal(75 - Session.FundingExpiryWarningDays, warn.Day);
        var expired = s.Records.Single(r => r.Kind == RecordKind.Funding && r.Subject == "om1" && r.Text.Contains("reached its fiscal-year end"));
        Assert.Equal(75, expired.Day);
    }

    [Fact]
    public void Funding_notices_reach_the_morning_board_linked_to_the_ledger()
    {
        var s = Session.Create(Repo.Load("spoke-b-90"), 1);
        PlayTo(s, 76);
        var board = s.View.MorningBoard();
        var lapse = board.Items.Single(i => i.Text.StartsWith("No funding line has money usable"));
        Assert.Equal(BoardLinkKind.Funding, lapse.LinkKind);
        Assert.Equal(BoardSeverity.Warning, lapse.Severity);
        var expiry = board.Items.Single(i => i.Text.Contains("reached its fiscal-year end"));
        Assert.Equal((BoardLinkKind.Funding, "om1"), (expiry.LinkKind, expiry.LinkId));

        PlayTo(s, Line(s, "om2").AvailableDay);
        var release = s.View.MorningBoard().Items.Single(i => i.Text.StartsWith("Comptroller released"));
        Assert.Equal(BoardSeverity.Info, release.Severity);
        Assert.Equal("om2", release.LinkId);
    }

    [Fact]
    public void The_editor_hard_stops_expired_and_unreleased_lines_and_shows_the_late_release()
    {
        var s = Session.Create(Repo.Load("spoke-b-90"), 1);
        Assert.True(s.View.Preview(PowerPo()).Accepted, string.Join("; ", s.View.Preview(PowerPo()).HardStops));
        Assert.Contains(s.View.Preview(PowerPo("om2")).HardStops, h => h.Contains("is not usable on day 1"));
        PlayTo(s, 77);
        Assert.Contains(s.View.Preview(PowerPo()).HardStops, h => h.Contains("The O&M line good through day 75 is not usable on day 77"));
        Assert.Contains(s.View.Preview(PowerPo("om2")).HardStops, h => h.Contains("The O&M line from day 76 is not usable on day 77"));
        Assert.False(s.Apply(PowerPo()).Accepted);
        var option = s.View.ActionEditor("spokeBSite", "power")!.Funding.Single(f => f.Id == "om2");
        Assert.False(option.UsableToday);
        Assert.Equal(76, option.PlannedAvailableDay);
        Assert.True(option.AvailableDay > option.PlannedAvailableDay);
    }

    [Fact]
    public void An_award_that_reaches_completion_after_the_fiscal_year_end_is_rejected_and_named_in_the_debrief()
    {
        var s = Session.Create(Repo.Load("spoke-b-90"), 1);
        PlayTo(s, 75);
        // Usable today; the award completes after day 75. Some vendors refuse this office by now (unpaid handshakes).
        var po = new[] { ("vendorA", "maintenance"), ("vendorC", "maintenance"), ("hubUtilities", "operator"), ("vendorB", "operator") }
            .Select(v => PowerPo(days: 15) with { Vendor = v.Item1, Lines = ["generation", v.Item2] })
            .First(a => s.View.Preview(a).Accepted);
        var applied = s.Apply(po);
        Assert.True(applied.Accepted, applied.Message);
        PlayTo(s, 91);
        var rejected = s.Records.Single(r => r.Kind == RecordKind.Rejected && r.Subject == "po");
        Assert.Contains("expired with day 75", rejected.Text);
        Assert.DoesNotContain(s.Arrangements, a => a.Id == "po");

        var gap = s.View.Debrief().Gaps.Single(g => g.SiteId == "spokeBSite" && g.CapabilityId == "power" && g.StartDay <= 76 && g.EndDay >= 77);
        var lapse = gap.Factors.Single(f => f.Kind == GapFactorKind.FundingLapse);
        Assert.Equal(GapTag.Avoidable, lapse.Tag);
        Assert.Contains("The O&M line good through day 75 reached its fiscal-year end with day 75", lapse.Text);
        Assert.Contains("late (the comptroller warned on day", lapse.Text);
        Assert.Contains("Meridian Site Services power at Spoke B reached award with no money to pay for it", lapse.Text);
        Assert.Contains(rejected.Id, lapse.RecordIds);
    }

    [Fact]
    public void A_funding_gap_before_any_money_is_released_is_unforeseeable()
    {
        // Spoke B with the only line released on day 10: Spoke B opens day 7 with nothing able to pay for anything.
        var b = Variant("spoke-b", n => n["fundingLines"]![0]!["availableDay"] = 10);
        var s = Replay.Run(b, 1, PolicyCatalog.Create("do-nothing"));
        var gap = s.View.Debrief().Gaps.First(g => g.SiteId == "spokeBSite" && g.CapabilityId == "power" && g.StartDay == 7);
        var lapse = gap.Factors.Single(f => f.Kind == GapFactorKind.FundingLapse);
        Assert.Equal(GapTag.Unforeseeable, lapse.Tag);
        Assert.Contains("days 1 to 9", lapse.Text);
        Assert.Contains("line was released day 10", lapse.Text);
    }

    [Fact]
    public void A_line_certified_for_one_capability_cannot_pay_for_another()
    {
        var b = Variant("spoke-b", n => n["fundingLines"]![0]!["capabilities"] = new JsonArray("fuel"));
        var s = Session.Create(b, 1);
        Assert.True(s.View.Preview(FuelPo()).Accepted, string.Join("; ", s.View.Preview(FuelPo()).HardStops));
        var stops = s.View.Preview(PowerPo()).HardStops;
        Assert.Contains(stops, h => h.Contains("is certified for fuel only, not power"));
        Assert.False(s.Apply(PowerPo()).Accepted);
        Assert.False(s.View.ActionEditor("spokeBSite", "power")!.Funding.Single().PaysForThis);
        Assert.True(s.View.ActionEditor("hubSite", "fuel")!.Funding.Single().PaysForThis);
        Assert.Equal(["fuel"], s.View.ActionEditor("hubSite", "fuel")!.Funding.Single().Capabilities!);

        var idiq = new EstablishIdiqAction { Id = "pIdiq", Vendor = "vendorA", Capability = "power", ScopeLines = ["generation"], CeilingCents = 30_000_000, FundingLine = "om1" };
        Assert.Contains(s.View.PreviewEstablish(idiq).HardStops, h => h.Contains("certified for fuel only"));

        // The textbook policy only points money at what it is certified for: it never tries power on om1.
        var t = Replay.Run(b, 1, PolicyCatalog.Create("textbook"));
        Assert.DoesNotContain(t.Records, r => r.Kind == RecordKind.Rejected && r.Text.Contains("certified"));
    }

    [Fact]
    public void A_severable_service_crossing_the_fiscal_year_end_is_capped_at_twelve_months()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1);
        Assert.Contains(s.View.Preview(PowerPo(days: 366)).HardStops, h => h.Contains("may last at most 12 months"));
        Assert.DoesNotContain(s.View.Preview(PowerPo(days: 365)).HardStops, h => h.Contains("12 months"));
    }

    [Fact]
    public void Scenario_loading_rejects_bad_funding_references()
    {
        Assert.Throws<InvalidDataException>(() => Variant("spoke-b", n => n["fundingLines"]![0]!["capabilities"] = new JsonArray("lasers")));
        Assert.Throws<InvalidDataException>(() => Variant("spoke-b", n => n["fundingLines"]![0]!["availableDay"] = 80));
        Assert.Throws<InvalidDataException>(() => Variant("spoke-b-90", n =>
            n["events"]!.AsArray().Single(e => (string?)e!["id"] == "fyFundingLate")!["effects"]![0]!["target"] = "om9"));
    }
}
