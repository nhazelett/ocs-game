using System.Text.Json.Nodes;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>M3-06 (ACSA part): ordering from a partner force, its stock limit, reimbursement and its withdrawal (SPEC 4.1).</summary>
public class AcsaTests
{
    private const long Price = 400; // cents per gallon-day

    /// <summary>spoke-b-90 with a partner force on Second Island sparing 1,500 gal/day of fuel; optionally withdrawing it.</summary>
    private static ScenarioBundle WithPartner(int? withdrawDay = null, Action<JsonNode>? more = null)
    {
        var root = JsonNode.Parse(File.ReadAllText(Path.Combine(Repo.Scenario("spoke-b-90"), "scenario.json")))!;
        root["actors"]!["acsaPartner"] = JsonNode.Parse($$"""
            {"present":true,"name":"Coalition logistics battalion","island":"spokeB","stock":{"fuel":1500},"unitPriceCents":{"fuel":{{Price}}},"reliability":1.0}
            """);
        if (withdrawDay is { } d)
            root["events"]!.AsArray().Add(JsonNode.Parse($$"""
                {"id":"partnerTempo","label":"The partner's own operations surge","window":{"earliestDay":{{d}},"latestDay":{{d}}},"preconditions":[],"effects":[{"kind":"acsa.withdraw","target":"fuel","days":2}]}
                """));
        more?.Invoke(root);
        var dir = Path.Combine(Path.GetTempPath(), $"los-acsa-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "scenario.json"), root.ToJsonString());
        return ScenarioBundle.Load(dir, Path.Combine(Repo.Root, "scenarios", "rules"));
    }

    private static AcsaOrderAction Order(string id = "acsaFuel", long qty = 1170, int days = 40, string line = "om1") => new()
    {
        Id = id, Site = "spokeBSite", Capability = "fuel", Quantity = qty, DurationDays = days, FundingLine = line,
    };

    private static long Obligated(Session s) => s.View.Funding.Single(f => f.Id == "om1").ObligatedCents;

    [Fact]
    public void An_order_starts_two_to_five_days_after_it_is_placed_and_obligates_the_reimbursement()
    {
        var rules = Repo.Load("spoke-b-90").Rules.Instruments["acsa"];
        for (var seed = 1; seed <= 20; seed++)
        {
            var s = Session.Create(WithPartner(), seed);
            var preview = s.View.PreviewAcsa(Order());
            Assert.True(preview.Accepted, string.Join("; ", preview.HardStops));
            Assert.Equal(1170 * Price * 40, preview.TotalCents);
            Assert.Contains(preview.SoftGaps, g => g.Contains("can withdraw"));
            Assert.True(s.Apply(Order()).Accepted);
            Assert.Equal(0, Obligated(s));
            while (s.Arrangements.All(a => a.Id != "acsaFuel")) s.AdvanceDay();
            var placed = s.Day - 1;
            var arr = s.Arrangements.Single(a => a.Id == "acsaFuel");
            Assert.Equal("acsa", arr.Instrument);
            Assert.InRange(arr.StartDay - placed - 1, rules.StartLagDays.Min, rules.StartLagDays.Max);
            Assert.InRange(arr.StartDay, preview.EarliestStartMin, preview.EarliestStartMax);
            Assert.Equal(preview.TotalCents, Obligated(s));
            Assert.Equal("spokeB", arr.SupplierIslandId);
            Assert.Equal(["delivery", "storage"], arr.Lines);
            while (s.Day <= arr.StartDay) s.AdvanceDay();
            Assert.Contains(s.Records, r => r.Kind == RecordKind.ArrangementStart && r.Subject == "acsaFuel" && r.Text.Contains("Coalition logistics battalion"));
        }
    }

    [Fact]
    public void Orders_beyond_the_partner_s_limits_are_refused_and_change_nothing()
    {
        var s = Session.Create(WithPartner(), 1);
        var hash = s.StateHash();
        string[] Stops(AcsaOrderAction o) => [.. s.View.PreviewAcsa(o).HardStops];
        Assert.Contains(Stops(Order(qty: 1600)), h => h.Contains("can spare 1500 gal/day"));
        Assert.Contains(Stops(Order() with { Capability = "power" }), h => h.Contains("no power to spare"));
        Assert.Contains(Stops(Order(line: "om2")), h => h.Contains("not usable on day 1"));
        Assert.Contains(Stops(Order(line: "")), h => h.Contains("needs a funding line"));
        Assert.Contains(Stops(Order(days: 0)), h => h.Contains("duration must be positive"));
        Assert.Contains(Session.Create(Repo.Load("spoke-b-90"), 1).View.PreviewAcsa(Order()).HardStops, h => h.Contains("no partner force"));
        Assert.Equal(hash, s.StateHash());
        var refused = s.Apply(Order(qty: 1600));
        Assert.False(refused.Accepted);
        Assert.Equal(Stops(Order(qty: 1600)), refused.HardStops);
        // Stock is shared by orders on the same days, including ones still in shop work.
        Assert.True(s.Apply(Order()).Accepted);
        Assert.Contains(Stops(Order("second", qty: 400)), h => h.Contains("1170 is already ordered"));
        Assert.Contains(Stops(Order()), h => h.Contains("already used"));
        Assert.True(s.View.PreviewAcsa(Order("second", qty: 330)).Accepted);
    }

    [Fact]
    public void A_withdrawal_ends_the_order_returns_the_unspent_money_and_stops_new_orders()
    {
        // Without the unit's own handshake event, nothing else fills the gap the withdrawal leaves.
        var s = Session.Create(WithPartner(withdrawDay: 20, more: r =>
        {
            var events = r["events"]!.AsArray();
            events.Remove(events.First(e => (string?)e!["id"] == "unitHandshake"));
        }), 1);
        Assert.True(s.Apply(Order()).Accepted);
        while (s.Day <= 20) s.AdvanceDay();
        var arr = s.Arrangements.Single(a => a.Id == "acsaFuel");
        Assert.Equal(21, arr.EndDay);
        var w = s.Records.Single(r => r.Kind == RecordKind.Withdrawal && r.Subject == "acsaFuel");
        Assert.Contains("not a contract", w.Text);
        var performed = arr.EndDay - arr.StartDay + 1;
        Assert.Equal(arr.DailyCostCents * performed, arr.ObligatedCents);
        Assert.Equal(arr.ObligatedCents, Obligated(s));
        Assert.Contains("fuel", s.View.AcsaWithdrawn);
        Assert.Contains(s.View.PreviewAcsa(Order("again")).HardStops, h => h.Contains("has withdrawn"));
        while (!s.Finished) s.AdvanceDay();
        Assert.Equal(ArrangementStatus.Ended, arr.Status);
        Assert.Equal(arr.IncurredCents, arr.ObligatedCents);
        // With nothing contractual behind it, the fuel gap after the withdrawal is the player's.
        var gap = s.View.Debrief().Gaps.First(g => g.SiteId == "spokeBSite" && g.CapabilityId == "fuel" && g.StartDay > 21
                                                         && g.Factors.Any(x => x.Kind == GapFactorKind.AcsaWithdrawn));
        var f = gap.Factors.Single(x => x.Kind == GapFactorKind.AcsaWithdrawn);
        Assert.Equal(GapTag.Avoidable, f.Tag);
        Assert.Contains(w.Id, f.RecordIds);
    }

    [Fact]
    public void A_withdrawal_before_the_order_is_placed_refuses_it()
    {
        var s = Session.Create(WithPartner(withdrawDay: 1), 1);
        Assert.True(s.Apply(Order()).Accepted);
        while (s.Day <= 6) s.AdvanceDay();
        Assert.DoesNotContain(s.Arrangements, a => a.Id == "acsaFuel");
        Assert.Contains(s.Records, r => r.Kind == RecordKind.Rejected && r.Subject == "acsaFuel" && r.Text.Contains("withdrew"));
        Assert.Equal(0, Obligated(s));
    }

    [Fact]
    public void Acsa_orders_replay_from_saves_and_take_no_cure_notice_or_cor()
    {
        var bundle = WithPartner(withdrawDay: 30);
        var s = Session.Create(bundle, 3);
        Assert.True(s.Apply(Order()).Accepted);
        for (var d = 0; d < 9; d++) s.AdvanceDay();
        var file = Path.Combine(Path.GetTempPath(), $"los-acsa-save-{Guid.NewGuid():N}.json");
        File.WriteAllText(file, s.ToSave().ToJson());
        var resumed = Replay.Resume(bundle, InputLog.Load(file));
        while (!s.Finished) s.AdvanceDay();
        while (!resumed.Finished) resumed.AdvanceDay();
        Assert.Equal(s.StateHash(), resumed.StateHash());
        Assert.DoesNotContain(s.View.PerformanceIssues, i => i.ArrangementId == "acsaFuel");
        Assert.Contains(s.View.Records, r => r.Subject == "acsaFuel" && s.View.Known(r));
    }

    [Fact]
    public void Bundle_validation_checks_acsa_references()
    {
        Assert.Throws<InvalidDataException>(() => WithPartner(more: r => r["actors"]!["acsaPartner"]!["island"] = "atlantis"));
        Assert.Throws<InvalidDataException>(() => WithPartner(more: r => r["actors"]!["acsaPartner"]!["stock"]!["warp"] = 5));
        Assert.Throws<InvalidDataException>(() => WithPartner(more: r => r["actors"]!["acsaPartner"]!["unitPriceCents"]!["water"] = 5));
        Assert.Throws<InvalidDataException>(() => WithPartner(withdrawDay: 5, more: r =>
            r["events"]!.AsArray().Last()!["effects"]![0]!["target"] = "warp"));
    }
}
