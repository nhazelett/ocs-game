using System.Text.Json.Nodes;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;

namespace LinesOfSupport.Sim.Tests;

/// <summary>M3-19: a contract with a notice clause terminated with notice runs on for the notice and costs nothing
/// more; terminated without it, it stops at once and the vendor files a claim the shop must settle.</summary>
public class ClaimTests
{
    private const long Daily = 150_000;

    private static JsonObject BFuel(int endDay = 90, int? notice = 15) => new()
    {
        ["id"] = "bFuel", ["instrument"] = "purchaseOrder", ["vendor"] = "vendorD", ["site"] = "spokeBSite", ["capability"] = "fuel",
        ["lines"] = new JsonArray("delivery", "storage"), ["startDay"] = -30, ["endDay"] = endDay,
        ["quantity"] = 1170, ["dailyCostCents"] = Daily, ["obligatedCents"] = Daily * endDay, ["fundingLine"] = "om1",
        ["noticeDays"] = notice is { } n ? n : null,
    };

    private static ScenarioBundle Variant(JsonObject row, Action<JsonNode>? edit = null)
    {
        var root = JsonNode.Parse(File.ReadAllText(Path.Combine(Repo.Scenario("spoke-b-90"), "scenario.json")))!;
        root["inheritedArrangements"]!.AsArray().Add(row);
        edit?.Invoke(root);
        var dir = Path.Combine(Path.GetTempPath(), $"los-claim-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "scenario.json"), root.ToJsonString());
        return ScenarioBundle.Load(dir, Path.Combine(Repo.Root, "scenarios", "rules"));
    }

    private static int TerminatedOn(Session s, string id)
        => s.Records.Single(r => r.Kind == RecordKind.ArrangementEnd && r.Subject == id && r.Text.Contains("terminated")).Day;

    private static Session TerminateOnDay(ScenarioBundle b, int day, bool notice, int runTo = 60)
    {
        var s = Session.Create(b, 1);
        while (s.Day < day) s.AdvanceDay();
        var r = s.Apply(new TerminateAction { Arrangement = "bFuel", WithNotice = notice });
        Assert.True(r.Accepted, r.Message);
        while (s.Day <= runTo) s.AdvanceDay();
        return s;
    }

    [Fact]
    public void The_handover_names_the_notice()
        => Assert.Contains("Termination needs 15 days' notice.",
                           Session.Create(Variant(BFuel()), 1).Records.Single(r => r.Kind == RecordKind.ArrangementStart && r.Subject == "bFuel").Text);

    [Fact]
    public void With_notice_it_runs_on_for_the_notice_and_draws_no_claim()
    {
        var s = TerminateOnDay(Variant(BFuel()), 10, notice: true, runTo: 20);
        var done = TerminatedOn(s, "bFuel");
        var a = s.Arrangements.Single(x => x.Id == "bFuel");
        Assert.Equal(done + 15, a.EndDay);
        Assert.Equal(ArrangementStatus.Active, a.Status);
        Assert.Empty(s.View.Claims);
        Assert.Contains("with 15 days' notice", s.Records.Single(r => r.Kind == RecordKind.ArrangementEnd && r.Subject == "bFuel").Text);
    }

    [Fact]
    public void Without_notice_it_stops_at_once_and_the_vendor_claims()
    {
        var bundle = Variant(BFuel());
        var s = TerminateOnDay(bundle, 10, notice: false);
        var done = TerminatedOn(s, "bFuel");
        var a = s.Arrangements.Single(x => x.Id == "bFuel");
        Assert.Equal(done, a.EndDay);
        var claim = Assert.Single(s.View.Claims);
        var days = bundle.Rules.Claims.SettlementDays;
        Assert.Equal(Daily * days, claim.AmountCents);
        Assert.NotNull(claim.SettledDay);
        Assert.Contains(s.Work, w => w.Kind == WorkKind.ClaimSettlement && w.CompletedDay == claim.SettledDay
                                     && w.HoursTotalMilli == (long)(bundle.Rules.Claims.SettleShopHours * 1000));
        // Performance to the termination day plus the settlement, all paid from om1 (the rest of the obligation came back).
        var performed = Daily * Math.Max(0, done - 1 + 1);
        Assert.Equal(performed + claim.AmountCents, a.IncurredCents);
        Assert.Equal(0, a.UnobligatedIncurredCents);
        var debrief = s.View.Debrief();
        Assert.Equal(1, debrief.Scorecard.Claims);
        Assert.Equal(claim.AmountCents, debrief.Scorecard.ClaimCents);
        Assert.Contains("filed a claim on bFuel", Assert.Single(debrief.Claims!).Text);
    }

    [Fact]
    public void A_claim_is_capped_at_the_days_cut_short()
    {
        var s = TerminateOnDay(Variant(BFuel(endDay: 14)), 10, notice: false, runTo: 30);
        var done = TerminatedOn(s, "bFuel");
        Assert.True(14 - done < 10);
        Assert.Equal(Daily * (14 - done), Assert.Single(s.View.Claims).AmountCents);
    }

    [Fact]
    public void Notice_is_refused_on_a_contract_without_a_clause()
    {
        var s = Session.Create(Variant(BFuel(notice: null)), 1);
        var r = s.Apply(new TerminateAction { Arrangement = "bFuel", WithNotice = true });
        Assert.False(r.Accepted);
        Assert.Contains("no termination notice clause", r.Message);
        Assert.True(s.Apply(new TerminateAction { Arrangement = "bFuel" }).Accepted);
        while (s.Day <= 30) s.AdvanceDay();
        Assert.Empty(s.View.Claims);
    }

    [Fact]
    public void A_vendor_in_default_has_no_claim()
    {
        // Island Fuel at 0.3: an issue opens quickly; the cure notice goes out; some seeds end in default.
        var bundle = Variant(BFuel(), root => root["vendors"]!.AsArray().Single(v => (string?)v!["id"] == "vendorD")!["reliability"] = 0.3);
        var found = false;
        for (var seed = 1; seed <= 60 && !found; seed++)
        {
            var s = Session.Create(bundle, seed);
            while (s.Day <= 60 && !found)
            {
                foreach (var i in s.View.PerformanceIssues.Where(i => i.ArrangementId == "bFuel" && i.CureNoticeDay is null && i.Outcome == CureOutcome.None).ToList())
                    s.Apply(new CureNoticeAction { Issue = i.Id });
                if (s.View.PerformanceIssues.Any(i => i.ArrangementId == "bFuel" && i.Outcome == CureOutcome.Default))
                {
                    found = true;
                    Assert.True(s.Apply(new TerminateAction { Arrangement = "bFuel" }).Accepted);
                    for (var k = 0; k < 10; k++) s.AdvanceDay();
                    Assert.Contains(s.Records, r => r.Kind == RecordKind.ArrangementEnd && r.Subject == "bFuel");
                    Assert.Empty(s.View.Claims);
                }
                else s.AdvanceDay();
            }
        }
        Assert.True(found, "no seed reached default");
    }

    [Fact]
    public void A_negative_notice_is_refused()
    {
        var ex = Assert.Throws<InvalidDataException>(() => Variant(BFuel(notice: -1)));
        Assert.Contains("noticeDays must not be negative", ex.Message);
    }
}
