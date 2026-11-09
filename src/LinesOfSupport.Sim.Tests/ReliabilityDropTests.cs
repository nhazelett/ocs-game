using System.Text.Json.Nodes;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>M3-18: a vendor's performance falls as an authored event, unless the player acted on the early slips (a
/// cure notice, or a replacement delivering). Inherited contracts with written terms now run the M2-18 issue flow.</summary>
public class ReliabilityDropTests
{
    private const int DropDay = 20;

    /// <summary>Spoke B fuel from Island Fuel (vendorD, about 0.88) inherited on a purchase order, watched by a good COR.</summary>
    private static JsonObject BFuel() => new()
    {
        ["id"] = "bFuel", ["instrument"] = "purchaseOrder", ["vendor"] = "vendorD", ["site"] = "spokeBSite", ["capability"] = "fuel",
        ["lines"] = new JsonArray("delivery", "storage"), ["startDay"] = -30, ["endDay"] = 90, ["cor"] = "corReyes",
        ["quantity"] = 1170, ["dailyCostCents"] = 150_000, ["obligatedCents"] = 13_500_000, ["fundingLine"] = "om1",
    };

    private static JsonNode Drop(string target = "bFuel", double value = 0.6, string predicateTarget = "bFuel") => JsonNode.Parse($$"""
        {"id":"fuelSlips","label":"Island Fuel's supplier falls behind","window":{"earliestDay":{{DropDay}},"latestDay":{{DropDay}}},"fixed":true,
         "preconditions":[{"kind":"arrangement.noCureNotice","target":"{{predicateTarget}}"},{"kind":"arrangement.notReplaced","target":"{{predicateTarget}}"}],
         "effects":[{"kind":"vendor.reliabilityDrop","target":"{{target}}","value":{{value}}}]}
        """)!;

    private static ScenarioBundle Variant(JsonNode? ev = null, bool withRow = true)
    {
        var root = JsonNode.Parse(File.ReadAllText(Path.Combine(Repo.Scenario("spoke-b-90"), "scenario.json")))!;
        if (withRow) root["inheritedArrangements"]!.AsArray().Add(BFuel());
        root["events"]!.AsArray().Add(ev ?? Drop());
        var dir = Path.Combine(Path.GetTempPath(), $"los-drop-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "scenario.json"), root.ToJsonString());
        return ScenarioBundle.Load(dir, Path.Combine(Repo.Root, "scenarios", "rules"));
    }

    private static readonly Lazy<ScenarioBundle> Bundle = new(() => Variant());

    private static bool Fired(Session s) => s.Records.Any(r => r.Kind == RecordKind.Event && r.Subject == "fuelSlips");

    /// <summary>Play to the drop day: optionally a cure notice the morning an issue on bFuel arrives.</summary>
    private static Session PlayTo(int seed, int day, bool cure)
    {
        var s = Session.Create(Bundle.Value, seed);
        while (s.Day < day)
        {
            if (cure)
                foreach (var i in s.View.PerformanceIssues.Where(i => i.ArrangementId == "bFuel" && i.CureNoticeDay is null && i.Outcome == CureOutcome.None))
                    s.Apply(new CureNoticeAction { Issue = i.Id });
            s.AdvanceDay();
        }
        return s;
    }

    [Fact]
    public void With_nothing_done_the_drop_lands_on_its_day()
    {
        var s = PlayTo(1, DropDay, cure: false);
        var a = s.Arrangements.Single(x => x.Id == "bFuel");
        var before = a.ReliabilityPermille;
        Assert.True(before > 600);
        if (s.View.PerformanceIssues.Any(i => i.ArrangementId == "bFuel")) return; // this seed opened an issue; covered below
        s.AdvanceDay();
        Assert.True(Fired(s));
        Assert.Equal(600, a.ReliabilityPermille);
    }

    [Fact]
    public void Over_200_seeds_the_drop_fires_exactly_when_no_cure_notice_was_issued()
    {
        int fired = 0, held = 0;
        for (var seed = 1; seed <= 200; seed++)
        {
            var s = PlayTo(seed, DropDay, cure: true);
            var cured = s.View.PerformanceIssues.Any(i => i.ArrangementId == "bFuel" && i.CureNoticeDay is not null)
                        || s.Work.Any(w => w.Kind == WorkKind.CureNotice && w.CompletedDay is null);
            var rel = s.Arrangements.Single(x => x.Id == "bFuel").ReliabilityPermille;
            s.AdvanceDay();
            Assert.Equal(!cured, Fired(s));
            if (Fired(s)) { fired++; Assert.Equal(Math.Min(rel, 600), s.Arrangements.Single(x => x.Id == "bFuel").ReliabilityPermille); }
            else held++;
        }
        // Both branches are exercised: some seeds slipped early enough for the issue and the notice.
        Assert.True(fired > 20, $"fired {fired}");
        Assert.True(held > 20, $"held {held}");
    }

    [Fact]
    public void A_replacement_delivering_before_the_day_holds_the_drop()
    {
        var delivering = 0;
        for (var seed = 1; seed <= 20; seed++)
        {
            var s = Session.Create(Bundle.Value, seed);
            var r = s.Apply(new AwardAction
            {
                Id = "bFuel2", Instrument = "purchaseOrder", Vendor = "vendorE", Site = "spokeBSite", Capability = "fuel",
                Lines = ["delivery", "storage"], Quantity = 300, DurationDays = 80, FundingLine = "om1",
            });
            Assert.True(r.Accepted, r.Message);
            while (s.Day < DropDay) s.AdvanceDay();
            var replaced = s.Arrangements.Any(x => x.Id == "bFuel2" && x.Status == ArrangementStatus.Active);
            var cured = s.View.PerformanceIssues.Any(i => i.ArrangementId == "bFuel" && i.CureNoticeDay is not null);
            s.AdvanceDay();
            if (replaced) delivering++;
            Assert.Equal(!replaced && !cured, Fired(s));
        }
        Assert.True(delivering >= 10, $"replacement delivering by day {DropDay} in {delivering} of 20 seeds");
    }

    [Fact]
    public void Failures_rise_after_the_drop()
    {
        var bundle = Variant();
        var noDrop = Variant(Drop(value: 0.999));
        int After(ScenarioBundle b)
        {
            var total = 0;
            for (var seed = 1; seed <= 40; seed++)
            {
                var s = Session.Create(b, seed);
                while (s.Day <= 50) s.AdvanceDay();
                total += s.Records.Count(r => r.Kind == RecordKind.VendorFailure && r.Subject == "bFuel" && r.Day > DropDay);
            }
            return total;
        }
        Assert.True(After(bundle) > 2 * After(noDrop));
    }

    [Fact]
    public void A_failure_after_slips_that_were_reported_is_avoidable_and_one_without_warning_is_unforeseeable()
    {
        GapFactor? avoidable = null, unforeseeable = null;
        for (var seed = 1; seed <= 200 && (avoidable is null || unforeseeable is null); seed++)
        {
            var s = Session.Create(Bundle.Value, seed);
            while (!s.Finished) s.AdvanceDay();
            if (!Fired(s)) continue;
            foreach (var f in s.View.Debrief().Gaps.SelectMany(g => g.Factors).Where(f => f.Kind == GapFactorKind.VendorFailure && f.Text.StartsWith(s.View.ArrangementName("bFuel"), StringComparison.Ordinal) && f.FirstDay > DropDay))
            {
                if (f.Tag == GapTag.Avoidable) avoidable ??= f;
                if (f.Tag == GapTag.Unforeseeable && !s.View.PerformanceIssues.Any(i => i.ArrangementId == "bFuel" && i.OpenedDay < DropDay)) unforeseeable ??= f;
            }
        }
        Assert.NotNull(avoidable);
        Assert.Contains("had shown the slips", avoidable!.Text);
        Assert.NotNull(unforeseeable);
        Assert.Contains("with no slips reported before", unforeseeable!.Text);
    }

    [Fact]
    public void An_inherited_contract_runs_the_issue_flow_and_takes_a_cor()
    {
        var opened = false;
        for (var seed = 1; seed <= 50 && !opened; seed++)
        {
            var s = PlayTo(seed, 40, cure: false);
            var issue = s.View.PerformanceIssues.FirstOrDefault(i => i.ArrangementId == "bFuel");
            if (issue is null) continue;
            opened = true;
            Assert.True(s.Apply(new CureNoticeAction { Issue = issue.Id }).Accepted);
        }
        Assert.True(opened);
        var t = Session.Create(Bundle.Value, 1);
        var r = t.Apply(new DesignateCorAction { Arrangement = "bFuel", Cor = "corOkafor" });
        Assert.True(r.Accepted, r.Message);
        // Host nation support still has no default clause.
        Assert.False(t.Apply(new DesignateCorAction { Arrangement = "hubGrid", Cor = "corOkafor" }).Accepted);
    }

    [Fact]
    public void Shipped_scenarios_have_no_contract_the_change_makes_curable()
    {
        // Their inherited rows are host nation support (no written terms), so the goldens hold unchanged.
        foreach (var id in new[] { "spoke-b", "spoke-b-90" })
            Assert.All(Session.Create(Repo.Load(id), 1).Arrangements, a => Assert.Equal("hostNation", a.Instrument));
    }

    [Theory]
    [InlineData("nope", 0.6, "bFuel", "vendor.reliabilityDrop target nope is not an inherited arrangement")]
    [InlineData("bFuel", 1.4, "bFuel", "vendor.reliabilityDrop value must be a reliability between 0 and 1")]
    [InlineData("bFuel", 0.6, "nope", "arrangement.noCureNotice target nope is not an inherited arrangement")]
    public void Bad_drop_events_are_refused(string target, double value, string predicateTarget, string message)
    {
        var ex = Assert.Throws<InvalidDataException>(() => Variant(Drop(target, value, predicateTarget)));
        Assert.Contains(message, ex.Message);
    }
}
