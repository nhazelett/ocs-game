using System.Text.Json.Nodes;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>M3-06 (host nation support part): requests under an agreement, its limits, the cost share and withdrawal of both
/// requested and inherited support (SPEC 4.1).</summary>
public class HostNationTests
{
    private const long Price = 100; // cents per gallon-day, the shop's cost share for fuel

    /// <summary>spoke-b-90 with a host nation agreement providing 1,500 gal/day of fuel at Spoke B (cost-shared) and
    /// 400 kWh/day of power at no cost to the shop; optionally withdrawing a capability on a day.</summary>
    private static ScenarioBundle WithAgreement(int? withdrawDay = null, string? withdrawCap = "fuel", int? notice = 2, Action<JsonNode>? more = null)
    {
        var root = JsonNode.Parse(File.ReadAllText(Path.Combine(Repo.Scenario("spoke-b-90"), "scenario.json")))!;
        root["actors"]!["hostNation"] = JsonNode.Parse($$"""
            {"agreementExists":true,"name":"Host nation works ministry","capacity":{"fuel":1500,"power":400},"unitPriceCents":{"fuel":{{Price}}},"sites":["spokeBSite"],"reliability":1.0}
            """);
        if (withdrawDay is { } d)
        {
            var effect = new JsonObject { ["kind"] = "hostNation.withdraw" };
            if (withdrawCap is not null) effect["target"] = withdrawCap;
            if (notice is { } n) effect["days"] = n;
            var ev = JsonNode.Parse($$"""
                {"id":"hostPolitics","label":"Host nation politics turn","window":{"earliestDay":{{d}},"latestDay":{{d}}},"preconditions":[],"effects":[]}
                """)!;
            ev["effects"]!.AsArray().Add(effect);
            root["events"]!.AsArray().Add(ev);
        }
        more?.Invoke(root);
        var dir = Path.Combine(Path.GetTempPath(), $"los-hn-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "scenario.json"), root.ToJsonString());
        return ScenarioBundle.Load(dir, Path.Combine(Repo.Root, "scenarios", "rules"));
    }

    private static HostNationRequestAction Request(string id = "hnFuel", long qty = 1170, int days = 40, string? line = "om1",
                                                   string cap = "fuel", string site = "spokeBSite") => new()
    {
        Id = id, Site = site, Capability = cap, Quantity = qty, DurationDays = days, FundingLine = line,
    };

    private static long Obligated(Session s) => s.View.Funding.Single(f => f.Id == "om1").ObligatedCents;

    private static void RemoveHandshake(JsonNode r)
    {
        var events = r["events"]!.AsArray();
        events.Remove(events.First(e => (string?)e!["id"] == "unitHandshake"));
    }

    [Fact]
    public void A_request_starts_three_to_ten_days_after_it_is_placed_and_obligates_the_cost_share()
    {
        var rules = Repo.Load("spoke-b-90").Rules.Instruments["hostNation"];
        for (var seed = 1; seed <= 20; seed++)
        {
            var s = Session.Create(WithAgreement(), seed);
            var preview = s.View.PreviewHostNation(Request());
            Assert.True(preview.Accepted, string.Join("; ", preview.HardStops));
            Assert.Equal(1170 * Price * 40, preview.TotalCents);
            Assert.Contains(preview.SoftGaps, g => g.Contains("can withdraw"));
            Assert.True(s.Apply(Request()).Accepted);
            Assert.Equal(0, Obligated(s));
            while (s.Arrangements.All(a => a.Id != "hnFuel")) s.AdvanceDay();
            var placed = s.Day - 1;
            var arr = s.Arrangements.Single(a => a.Id == "hnFuel");
            Assert.Equal("hostNation", arr.Instrument);
            Assert.Null(arr.VendorId);
            Assert.InRange(arr.StartDay - placed - 1, rules.StartLagDays.Min, rules.StartLagDays.Max);
            Assert.InRange(arr.StartDay, preview.EarliestStartMin, preview.EarliestStartMax);
            Assert.Equal(preview.TotalCents, Obligated(s));
            Assert.Equal("spokeB", arr.SupplierIslandId);
            Assert.Equal(["delivery", "storage"], arr.Lines);
            while (s.Day <= arr.StartDay) s.AdvanceDay();
            Assert.Contains(s.Records, r => r.Kind == RecordKind.ArrangementStart && r.Subject == "hnFuel" && r.Text.Contains("Host nation works ministry"));
        }
    }

    [Fact]
    public void A_request_with_no_cost_share_needs_no_funding_line_and_obligates_nothing()
    {
        var s = Session.Create(WithAgreement(), 2);
        var req = Request("hnPower", qty: 300, cap: "power", line: null);
        var preview = s.View.PreviewHostNation(req);
        Assert.True(preview.Accepted, string.Join("; ", preview.HardStops));
        Assert.Equal(0, preview.TotalCents);
        Assert.True(s.Apply(req).Accepted);
        while (s.Arrangements.All(a => a.Id != "hnPower")) s.AdvanceDay();
        var arr = s.Arrangements.Single(a => a.Id == "hnPower");
        Assert.Equal(0, arr.ObligatedCents);
        Assert.Null(arr.FundingLineId);
        Assert.Equal(0, Obligated(s));
        while (!s.Finished) s.AdvanceDay();
        Assert.Equal(0, arr.IncurredCents);
        Assert.Equal(0, arr.UnobligatedIncurredCents);
    }

    [Fact]
    public void Requests_outside_the_agreement_are_refused_and_change_nothing()
    {
        var s = Session.Create(WithAgreement(), 1);
        var hash = s.StateHash();
        string[] Stops(HostNationRequestAction o) => [.. s.View.PreviewHostNation(o).HardStops];
        Assert.Contains(Stops(Request(qty: 1600)), h => h.Contains("provides 1500 gal/day"));
        Assert.Contains(Stops(Request(cap: "water")), h => h.Contains("does not cover water"));
        Assert.Contains(Stops(Request(site: "hubSite")), h => h.Contains("does not cover"));
        Assert.Contains(Stops(Request(line: "om2")), h => h.Contains("not usable on day 1"));
        Assert.Contains(Stops(Request(line: null)), h => h.Contains("needs a funding line"));
        Assert.Contains(Stops(Request(days: 0)), h => h.Contains("Duration must be positive"));
        Assert.Contains(Session.Create(Repo.Load("spoke-b-90"), 1).View.PreviewHostNation(Request()).HardStops, h => h.Contains("No host nation support agreement"));
        Assert.Equal(hash, s.StateHash());
        var refused = s.Apply(Request(qty: 1600));
        Assert.False(refused.Accepted);
        Assert.Equal(Stops(Request(qty: 1600)), refused.HardStops);
        // Capacity is shared by requests on the same days, including ones still in shop work.
        Assert.True(s.Apply(Request()).Accepted);
        Assert.Contains(Stops(Request("second", qty: 400)), h => h.Contains("1170 is already requested"));
        Assert.Contains(Stops(Request()), h => h.Contains("already exists"));
        Assert.True(s.View.PreviewHostNation(Request("second", qty: 330)).Accepted);
    }

    [Fact]
    public void A_withdrawal_ends_the_request_returns_the_unspent_share_and_stops_new_requests()
    {
        var s = Session.Create(WithAgreement(withdrawDay: 25, more: RemoveHandshake), 1);
        Assert.True(s.Apply(Request()).Accepted);
        while (s.Day <= 25) s.AdvanceDay();
        var arr = s.Arrangements.Single(a => a.Id == "hnFuel");
        Assert.Equal(26, arr.EndDay);
        var w = s.Records.Single(r => r.Kind == RecordKind.Withdrawal && r.Subject == "hnFuel");
        Assert.Contains("not a contract", w.Text);
        var performed = arr.EndDay - arr.StartDay + 1;
        Assert.Equal(arr.DailyCostCents * performed, arr.ObligatedCents);
        Assert.Equal(arr.ObligatedCents, Obligated(s));
        Assert.Contains("fuel", s.View.HostNationWithdrawn);
        Assert.Contains(s.View.PreviewHostNation(Request("again")).HardStops, h => h.Contains("has withdrawn"));
        while (!s.Finished) s.AdvanceDay();
        Assert.Equal(ArrangementStatus.Ended, arr.Status);
        Assert.Equal(arr.IncurredCents, arr.ObligatedCents);
        var gap = s.View.Debrief().Gaps.First(g => g.SiteId == "spokeBSite" && g.CapabilityId == "fuel" && g.StartDay > 26
                                                         && g.Factors.Any(x => x.Kind == GapFactorKind.HostNationWithdrawn));
        var f = gap.Factors.Single(x => x.Kind == GapFactorKind.HostNationWithdrawn);
        Assert.Equal(GapTag.Avoidable, f.Tag);
        Assert.Contains(w.Id, f.RecordIds);
    }

    [Fact]
    public void A_withdrawal_ends_inherited_host_nation_support_with_notice_from_the_rules()
    {
        var notice = Repo.Load("spoke-b-90").Rules.Instruments["hostNation"].WithdrawalNoticeDays!;
        for (var seed = 1; seed <= 10; seed++)
        {
            var s = Session.Create(WithAgreement(withdrawDay: 30, withdrawCap: "power", notice: null), seed);
            while (s.Day <= 30) s.AdvanceDay();
            var grid = s.Arrangements.Single(a => a.Id == "hubGrid");
            Assert.InRange(grid.EndDay, 30 + notice.Min - 1, 30 + notice.Max - 1);
            var w = s.Records.Single(r => r.Kind == RecordKind.Withdrawal && r.Subject == "hubGrid");
            Assert.Contains("inherited", w.Text);
            Assert.Equal(400, s.Arrangements.Single(a => a.Id == "hubFuelPoint").EndDay);
            while (!s.Finished) s.AdvanceDay();
            // The force arrived with it, so the loss was discoverable, not the player's own choice.
            var gap = s.View.Debrief().Gaps.First(g => g.SiteId == "hubSite" && g.CapabilityId == "power" && g.StartDay > grid.EndDay);
            var f = gap.Factors.Single(x => x.Kind == GapFactorKind.HostNationWithdrawn);
            Assert.Equal(GapTag.Discoverable, f.Tag);
            Assert.Contains(w.Id, f.RecordIds);
        }
    }

    [Fact]
    public void A_withdrawal_before_the_request_is_placed_refuses_it()
    {
        var s = Session.Create(WithAgreement(withdrawDay: 1), 1);
        Assert.True(s.Apply(Request()).Accepted);
        while (s.Day <= 6) s.AdvanceDay();
        Assert.DoesNotContain(s.Arrangements, a => a.Id == "hnFuel");
        Assert.Contains(s.Records, r => r.Kind == RecordKind.Rejected && r.Subject == "hnFuel" && r.Text.Contains("withdrew"));
        Assert.Equal(0, Obligated(s));
    }

    [Fact]
    public void Host_nation_requests_replay_from_saves_and_take_no_cure_notice()
    {
        var bundle = WithAgreement(withdrawDay: 35);
        var s = Session.Create(bundle, 3);
        Assert.True(s.Apply(Request()).Accepted);
        Assert.True(s.Apply(Request("hnPower", qty: 300, cap: "power", line: null)).Accepted);
        for (var d = 0; d < 9; d++) s.AdvanceDay();
        var file = Path.Combine(Path.GetTempPath(), $"los-hn-save-{Guid.NewGuid():N}.json");
        File.WriteAllText(file, s.ToSave().ToJson());
        var resumed = Replay.Resume(bundle, InputLog.Load(file));
        while (!s.Finished) s.AdvanceDay();
        while (!resumed.Finished) resumed.AdvanceDay();
        Assert.Equal(s.StateHash(), resumed.StateHash());
        Assert.DoesNotContain(s.View.PerformanceIssues, i => i.ArrangementId is "hnFuel" or "hnPower");
    }

    [Fact]
    public void Bundle_validation_checks_host_nation_references()
    {
        Assert.Throws<InvalidDataException>(() => WithAgreement(more: r => r["actors"]!["hostNation"]!["island"] = "atlantis"));
        Assert.Throws<InvalidDataException>(() => WithAgreement(more: r => r["actors"]!["hostNation"]!["capacity"]!["warp"] = 5));
        Assert.Throws<InvalidDataException>(() => WithAgreement(more: r => r["actors"]!["hostNation"]!["unitPriceCents"]!["water"] = 5));
        Assert.Throws<InvalidDataException>(() => WithAgreement(more: r => r["actors"]!["hostNation"]!["sites"]!.AsArray().Add("nowhere")));
        Assert.Throws<InvalidDataException>(() => WithAgreement(withdrawDay: 5, more: r =>
            r["events"]!.AsArray().Last()!["effects"]![0]!["target"] = "warp"));
    }
}
