using System.Text.Json.Nodes;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>M3-06 (off-island part): stateside sourcing by airlift or sealift, the airlift bump, customs on arrival, and
/// the debrief factor for a late lift (SPEC 4.1 "Stateside or off-island sourcing").</summary>
public class OffIslandTests
{
    /// <summary>spoke-b-90 with two off-island vendors entering at the hub: generators by air or sea, fuel by sea.</summary>
    private static ScenarioBundle WithStateside(Action<JsonNode>? more = null)
    {
        var root = JsonNode.Parse(File.ReadAllText(Path.Combine(Repo.Scenario("spoke-b-90"), "scenario.json")))!;
        var vendors = root["vendors"]!.AsArray();
        vendors.Add(JsonNode.Parse("""
            {"id":"statesideGen","name":"Stateside generator lessor","island":"hub","lift":["airlift","sealift"],
             "offers":[{"capability":"power","lines":["generation","maintenance"],"capacityPerDay":3000,"leadTimeDays":2,"unitPriceCents":150},
                       {"capability":"fuel","lines":["delivery"],"capacityPerDay":20000,"leadTimeDays":0,"unitPriceCents":300}],
             "reliability":0.99,"acceptsGpc":true,"acceptsCash":false,"vetted":true,"infoTier":"estimated"}
            """));
        vendors.Add(JsonNode.Parse("""
            {"id":"statesideFuel","name":"Stateside fuel supplier","island":"hub","lift":["sealift"],
             "offers":[{"capability":"fuel","lines":["delivery","storage"],"capacityPerDay":20000,"leadTimeDays":0,"unitPriceCents":300}],
             "reliability":0.99,"acceptsGpc":false,"acceptsCash":false,"vetted":true,"infoTier":"estimated"}
            """));
        more?.Invoke(root);
        var dir = Path.Combine(Path.GetTempPath(), $"los-lift-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "scenario.json"), root.ToJsonString());
        return ScenarioBundle.Load(dir, Path.Combine(Repo.Root, "scenarios", "rules"));
    }

    private static AwardAction Gen(string? lift = "airlift", string id = "liftGen", string instrument = "purchaseOrder") => new()
    {
        Id = id, Instrument = instrument, Vendor = "statesideGen", Site = "spokeBSite", Capability = "power",
        Lines = ["generation", "maintenance"], Quantity = 500, DurationDays = 60, FundingLine = "om1", Lift = lift,
    };

    private static Session PlayUntilStarted(Session s, string id)
    {
        while (!s.Finished && s.Arrangements.FirstOrDefault(a => a.Id == id) is not { Status: ArrangementStatus.Active }) s.AdvanceDay();
        return s;
    }

    [Fact]
    public void Airlift_arrives_inside_the_preview_range_and_never_in_week_one()
    {
        var bumped = 0; var held = 0;
        for (var seed = 1; seed <= 30; seed++)
        {
            var s = Session.Create(WithStateside(), seed);
            var preview = s.View.Preview(Gen());
            Assert.True(preview.Accepted, string.Join("; ", preview.HardStops));
            Assert.Contains(preview.SoftGaps, g => g.Contains("bump"));
            Assert.True(preview.EarliestStartMin > 7);
            Assert.True(s.Apply(Gen()).Accepted);
            PlayUntilStarted(s, "liftGen");
            var arr = s.Arrangements.Single(a => a.Id == "liftGen");
            Assert.Equal(ArrangementStatus.Active, arr.Status);
            Assert.InRange(arr.StartDay, preview.EarliestStartMin, preview.EarliestStartMax);
            Assert.Equal(59, arr.EndDay - arr.StartDay);
            Assert.Contains(s.Records, r => r.Subject == "liftGen" && r.Text.Contains("ships by airlift"));
            bumped += s.Records.Count(r => r.Subject == "liftGen" && r.Text.Contains("bumped"));
            held += s.Records.Count(r => r.Subject == "liftGen" && r.Text.StartsWith("Customs"));
        }
        Assert.True(bumped > 0, "no airlift bump in 30 seeds");
        Assert.True(held > 0, "no customs hold in 30 seeds");
    }

    [Fact]
    public void Sealift_takes_a_month_and_onward_delivery_rides_the_surface_link()
    {
        var lift = Repo.Load("spoke-b-90").Rules.Lift;
        var s = Session.Create(WithStateside(), 4);
        var fuel = new AwardAction
        {
            Id = "seaFuel", Instrument = "purchaseOrder", Vendor = "statesideFuel", Site = "spokeBSite", Capability = "fuel",
            Lines = ["delivery", "storage"], Quantity = 1200, DurationDays = 40, FundingLine = "om1", Lift = "sealift",
        };
        var preview = s.View.Preview(fuel);
        Assert.True(preview.Accepted, string.Join("; ", preview.HardStops));
        Assert.True(preview.EarliestStartMin >= 1 + lift.SealiftTransitDays.Min);
        Assert.True(s.Apply(fuel).Accepted);
        PlayUntilStarted(s, "seaFuel");
        var arr = s.Arrangements.Single(a => a.Id == "seaFuel");
        Assert.True(arr.StartDay > lift.SealiftTransitDays.Min);
        Assert.Equal("hub", arr.SupplierIslandId);
        Assert.DoesNotContain(s.Records, r => r.Subject == "seaFuel" && r.Text.Contains("bumped"));
    }

    [Fact]
    public void Lift_mode_hard_stops()
    {
        var s = Session.Create(WithStateside(), 1);
        var hash = s.StateHash();
        string[] Stops(AwardAction a) => [.. s.View.Preview(a).HardStops];
        Assert.Contains(Stops(Gen(lift: null)), h => h.Contains("needs a lift mode"));
        Assert.Contains(Stops(Gen(lift: "teleport")), h => h.Contains("does not ship by teleport"));
        Assert.Contains(Stops(Gen() with { Capability = "fuel", Lines = ["delivery"] }), h => h.Contains("does not fly"));
        Assert.Contains(Stops(Gen(lift: "sealift") with { Vendor = "vendorE", Capability = "fuel", Lines = ["delivery"] }), h => h.Contains("is local"));
        Assert.Contains(Stops(Gen(instrument: "sf44") with { FundingLine = null }), h => h.Contains("cash buy in person"));
        Assert.True(s.View.Preview(Gen(lift: "sealift")).Accepted);
        Assert.Equal(hash, s.StateHash());
        Assert.False(s.Apply(Gen(lift: null)).Accepted);
    }

    [Fact]
    public void A_gap_while_the_lift_is_late_carries_an_unforeseeable_lift_delay()
    {
        var found = false;
        for (var seed = 1; seed <= 40 && !found; seed++)
        {
            var s = Session.Create(WithStateside(), seed);
            Assert.True(s.Apply(Gen()).Accepted);
            while (!s.Finished) s.AdvanceDay();
            var delays = s.Records.Where(r => r.Subject == "liftGen" && (r.Text.Contains("bumped") || r.Text.StartsWith("Customs"))).ToList();
            if (delays.Count == 0) continue;
            var arr = s.Arrangements.Single(a => a.Id == "liftGen");
            var f = s.View.Debrief().Gaps.Where(g => g.SiteId == "spokeBSite" && g.CapabilityId == "power")
                     .SelectMany(g => g.Factors).FirstOrDefault(x => x.Kind == GapFactorKind.LiftDelay);
            if (f is null) continue;
            found = true;
            Assert.Equal(GapTag.Unforeseeable, f.Tag);
            Assert.True(f.FirstDay < arr.StartDay);
            Assert.Contains(delays[0].Id, f.RecordIds);
            Assert.Contains($"arrived day {arr.StartDay}", f.Text);
        }
        Assert.True(found, "no seed in 1..40 produced a lift delay over a power gap");
    }

    [Fact]
    public void Off_island_awards_replay_from_saves()
    {
        var bundle = WithStateside();
        var s = Session.Create(bundle, 7);
        Assert.True(s.Apply(Gen()).Accepted);
        for (var d = 0; d < 12; d++) s.AdvanceDay();
        var file = Path.Combine(Path.GetTempPath(), $"los-lift-save-{Guid.NewGuid():N}.json");
        File.WriteAllText(file, s.ToSave().ToJson());
        var resumed = Replay.Resume(bundle, InputLog.Load(file));
        while (!s.Finished) s.AdvanceDay();
        while (!resumed.Finished) resumed.AdvanceDay();
        Assert.Equal(s.StateHash(), resumed.StateHash());
    }

    [Fact]
    public void Bundle_validation_checks_lift_modes()
        => Assert.Throws<InvalidDataException>(() => WithStateside(r => r["vendors"]!.AsArray().Last()!["lift"] = new JsonArray("rocket")));
}
