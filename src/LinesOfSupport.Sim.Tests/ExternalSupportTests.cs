using System.Text.Json.Nodes;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>M3-05 (Sim side): the external support request from the OCS seat, with approval lag and mobilization
/// (SPEC 4.1, SPEC 5 approach 4).</summary>
public class ExternalSupportTests
{
    private static string Golden(string file) => Path.Combine(Repo.Scenario("spoke-b-90"), "golden", file);

    private static RequestExternalSupportAction Request(string id = "ext", long qty = 470, int days = 60) => new()
    {
        Id = id, Card = "req0007", Lines = ["generation", "fuelSupply", "operator", "maintenance"], Quantity = qty,
        DurationDays = days, FundingLine = "om1",
    };

    private static ScenarioBundle Variant(Action<JsonNode> change)
    {
        var root = JsonNode.Parse(File.ReadAllText(Path.Combine(Repo.Scenario("spoke-b-90"), "scenario.json")))!;
        change(root);
        var dir = Path.Combine(Path.GetTempPath(), $"los-ext-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "scenario.json"), root.ToJsonString());
        return ScenarioBundle.Load(dir, Path.Combine(Repo.Root, "scenarios", "rules"));
    }

    private static long Obligated(Session s) => s.View.Funding.Single(f => f.Id == "om1").ObligatedCents;

    [Fact]
    public void Approval_then_mobilization_puts_the_contractor_on_site_in_month_two_over_many_seeds()
    {
        var rules = Repo.Load("spoke-b-90").Rules.Instruments["externalSupport"];
        var starts = new HashSet<int>();
        for (var seed = 1; seed <= 30; seed++)
        {
            var s = Session.Create(Repo.Load("spoke-b-90"), seed);
            var preview = s.View.PreviewExternalSupport(Request());
            Assert.True(preview.Accepted, string.Join("; ", preview.HardStops));
            Assert.True(s.Apply(Request()).Accepted);
            Assert.Equal(0, Obligated(s));
            Assert.DoesNotContain(s.Arrangements, a => a.Id == "ext");
            while (s.View.ExternalSupportRequests().Single().Status != "Awarded" && !s.Finished) s.AdvanceDay();
            var view = s.View.ExternalSupportRequests().Single();
            var lag = view.AwardedDay!.Value - view.SubmittedDay!.Value;
            Assert.InRange(lag, rules.ApprovalLagDays!.Min, rules.ApprovalLagDays.Max);
            var mobilization = view.StartDay!.Value - view.AwardedDay.Value - 1;
            Assert.InRange(mobilization, rules.MobilizationDays!.Min, rules.MobilizationDays.Max);
            Assert.InRange(view.StartDay.Value, preview.EarliestStartMin, preview.EarliestStartMax);
            // Nothing about it helps day 7 (SPEC 5 approach 4): at the best rolls it starts after week three.
            Assert.True(view.StartDay > 21);
            // The external office obligates the whole task order from the unit's line at award.
            Assert.Equal(preview.TotalCents, view.TotalCents);
            Assert.Equal(view.TotalCents, Obligated(s));
            var arr = s.Arrangements.Single(a => a.Id == "ext");
            Assert.Equal("externalSupport", arr.Instrument);
            Assert.Equal(ArrangementStatus.Pending, arr.Status);
            Assert.Equal(view.StartDay, arr.StartDay);
            Assert.Equal(view.StartDay + 59, arr.EndDay);
            // Self-managed: supplied on the site's own island, and its fuel line covers the site's fuel.
            Assert.Equal("spokeB", arr.SupplierIslandId);
            Assert.True(arr.SuppliedQuantityMilli.ContainsKey("fuel"));
            while (s.Day <= arr.StartDay) s.AdvanceDay();
            Assert.Equal(ArrangementStatus.Active, arr.Status);
            Assert.Contains(s.Records, r => r.Kind == RecordKind.ArrangementStart && r.Subject == "ext" && r.Text.Contains("Theater base support contractor"));
            starts.Add(view.StartDay.Value);
        }
        Assert.True(starts.Count > 3, "seeds should give visibly different arrival days");
    }

    [Fact]
    public void The_view_shows_an_answer_range_until_the_award_and_the_start_day_only_after()
    {
        var s = Session.Create(Repo.Load("spoke-b-90"), 1);
        Assert.True(s.Apply(Request()).Accepted);
        Assert.Equal("Preparing", s.View.ExternalSupportRequests().Single().Status);
        while (s.View.ExternalSupportRequests().Single().Status == "Preparing") s.AdvanceDay();
        var v = s.View.ExternalSupportRequests().Single();
        Assert.Equal("InApproval", v.Status);
        Assert.Equal(v.SubmittedDay + 10, v.ExpectedAnswerMin);
        Assert.Equal(v.SubmittedDay + 20, v.ExpectedAnswerMax);
        Assert.Null(v.StartDay);
        Assert.Null(v.AwardedDay);
        // Submission and award reach the morning board as higher-HQ news.
        var board = s.View.MorningBoard();
        Assert.Contains(board.Items, i => i.Category == BoardCategory.HigherHq && i.Text.Contains("submitted through the SCO"));
        while (s.View.ExternalSupportRequests().Single().Status == "InApproval") s.AdvanceDay();
        Assert.Contains(s.View.MorningBoard().Items, i => i.Category == BoardCategory.HigherHq && i.Text.Contains("awarded ext"));
        Assert.NotNull(s.View.ExternalSupportRequests().Single().StartDay);
    }

    [Fact]
    public void Requests_outside_the_contract_s_limits_are_refused_and_change_nothing()
    {
        var s = Session.Create(Repo.Load("spoke-b-90"), 1);
        var hash = s.StateHash();
        string[] Stops(RequestExternalSupportAction r) => [.. s.View.PreviewExternalSupport(r).HardStops];
        Assert.Contains(Stops(Request(days: 30)), h => h.Contains("at least 60 days"));
        Assert.Contains(Stops(Request() with { Lines = ["generation", "fuelSupply", "maintenance"] }), h => h.Contains("self-managed") && h.Contains("operators"));
        Assert.Contains(Stops(Request() with { Lines = ["generation", "operator", "maintenance", "nope"] }), h => h.Contains("is not a power line"));
        Assert.Contains(Stops(Request(qty: 2500)), h => h.Contains("2000 kW/day"));
        Assert.Contains(Stops(Request() with { FundingLine = "om2" }), h => h.Contains("not usable on day 1"));
        Assert.Contains(Stops(Request() with { FundingLine = "" }), h => h.Contains("needs a funding line"));
        Assert.Contains(Stops(Request(qty: 1900)), h => h.Contains("left for an estimated"));
        Assert.Contains(Stops(Request() with { Cor = "nobody" }), h => h.Contains("not on the roster"));
        Assert.Contains(Stops(Request() with { Card = null, Site = "spokeBSite", Capability = "fuel", Lines = ["delivery"] }), h => h.Contains("does not provide fuel"));
        Assert.Contains(Stops(Request() with { Id = "" }), h => h.Contains("needs an id"));
        // Spoke B (30 days) has no external support contract in theater.
        var none = Session.Create(Repo.Load("spoke-b"), 1);
        Assert.Contains(none.View.PreviewExternalSupport(Request()).HardStops, h => h.Contains("no external support contract"));
        // Preview and refused applies leave the game unchanged but for the Rejected record.
        Assert.Equal(hash, s.StateHash());
        var result = s.Apply(Request(days: 30));
        Assert.False(result.Accepted);
        Assert.Equal(Stops(Request(days: 30)), result.HardStops);
        Assert.Empty(s.View.ExternalSupportRequests());
        Assert.True(s.Apply(Request()).Accepted);
        Assert.Contains(s.View.PreviewExternalSupport(Request()).HardStops, h => h.Contains("already used"));
        // Capacity is shared across requests.
        Assert.Contains(s.View.PreviewExternalSupport(Request("ext2", qty: 1600)).HardStops, h => h.Contains("470 is already requested"));
    }

    [Fact]
    public void An_award_the_line_can_no_longer_pay_for_fails_and_the_debrief_names_the_funding()
    {
        // Two requests each fit om1 when made; the money is obligated only at award, so the second finds too little left.
        var s = Session.Create(Repo.Load("spoke-b-90"), 1);
        Assert.True(s.Apply(Request("extA", qty: 900)).Accepted);
        Assert.True(s.Apply(Request("extB", qty: 900)).Accepted);
        while (s.View.ExternalSupportRequests().Any(r => r.Status is "Preparing" or "InApproval")) s.AdvanceDay();
        var views = s.View.ExternalSupportRequests();
        Assert.Single(views, v => v.Status == "Awarded");
        var refused = views.Single(v => v.Status == "Refused");
        Assert.Contains("left of the", refused.RefusedReason);
        Assert.DoesNotContain(s.Arrangements, a => a.Id == refused.Id);
        Assert.Contains(s.Records, r => r.Kind == RecordKind.ExternalSupport && r.Subject == refused.Id && r.Text.Contains("could not award"));
        Assert.Equal(views.Single(v => v.Status == "Awarded").TotalCents, Obligated(s));
    }

    [Fact]
    public void Approach_4_alone_leaves_month_one_dark_and_the_debrief_says_why()
    {
        var s = Replay.Run(Repo.Load("spoke-b-90"), InputLog.Load(Golden("approach4-external-only.json")));
        var ext = s.Arrangements.Single(a => a.Id == "extPower");
        Assert.True(ext.StartDay > 21);
        var gap = s.View.Debrief().Gaps.Single(g => g.CapabilityId == "power" && g.SiteId == "spokeBSite" && g.StartDay == 7);
        Assert.Equal(ext.StartDay - 1, gap.EndDay);
        var factor = gap.Factors.Single(f => f.Kind == GapFactorKind.Uncovered);
        Assert.Equal(GapTag.Avoidable, factor.Tag);
        Assert.Contains("external support contract", factor.Text);
        Assert.Contains("a bridge was needed", factor.Text);
    }

    [Fact]
    public void Approach_4_paired_with_a_bridge_beats_both_halves()
    {
        double Mission(string file) => Replay.Run(Repo.Load("spoke-b-90"), InputLog.Load(Golden(file))).Metrics["mission.output"];
        var paired = Mission("approach4-external-with-uca-bridge.json");
        Assert.True(paired > Mission("approach4-external-only.json"));
        Assert.True(paired > Mission("approach3-uca-turnkey-with-bridge.json"));
        var s = Replay.Run(Repo.Load("spoke-b-90"), InputLog.Load(Golden("approach4-external-with-uca-bridge.json")));
        Assert.Equal(0, s.Metrics["site.spokeBSite.power.downDays"]);
        Assert.Equal(0, s.Metrics["cost.unobligatedIncurredCents"]);
    }

    [Fact]
    public void Saves_and_counterfactuals_carry_the_request()
    {
        var bundle = Repo.Load("spoke-b-90");
        var log = InputLog.Load(Golden("approach4-external-only.json"));
        var full = Replay.Run(bundle, log);
        // Saved while the request is in approval, resumed, played out: same game.
        var partial = Session.Create(bundle, log.Seed, log.PrepChoices);
        foreach (var day in Enumerable.Range(1, 9))
        {
            foreach (var a in log.Actions.Where(a => a.Day == day)) partial.Apply(a);
            partial.AdvanceDay();
        }
        var file = Path.Combine(Path.GetTempPath(), $"los-ext-save-{Guid.NewGuid():N}.json");
        File.WriteAllText(file, partial.ToSave().ToJson());
        var resumed = Replay.Resume(bundle, InputLog.Load(file));
        while (!resumed.Finished) resumed.AdvanceDay();
        Assert.Equal(full.StateHash(), resumed.StateHash());
        // Removing the request in a counterfactual removes the contract and its month-two power.
        var index = log.Actions.FindIndex(a => a is RequestExternalSupportAction);
        var cf = Counterfactual.Run(bundle, log, CounterfactualChange.Remove(index));
        Assert.DoesNotContain(cf.Alternative.Arrangements, a => a.Id == "extPower");
        Assert.True(cf.Alternative.Metrics["mission.output"] < full.Metrics["mission.output"]);
    }

    [Fact]
    public void Bundle_validation_checks_external_support_references()
    {
        Assert.Throws<InvalidDataException>(() => Variant(root => root["actors"]!["externalSupport"]!["offers"]![0]!["capability"] = "warp"));
        Assert.Throws<InvalidDataException>(() => Variant(root => root["actors"]!["externalSupport"]!["offers"]![0]!["lines"]!.AsArray().Add("nope")));
        Assert.Throws<InvalidDataException>(() => Variant(root => root["actors"]!["externalSupport"]!["offers"]![0]!["lines"] = new JsonArray("operator")));
        Assert.Throws<InvalidDataException>(() => Variant(root => root["actors"]!["externalSupport"]!["offers"]![0]!["sites"] = new JsonArray("nowhere")));
    }
}
