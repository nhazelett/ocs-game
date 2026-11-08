using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

/// <summary>M2-12: establishing a BPA (SPEC 4.1).</summary>
public class EstablishBpaTests
{
    private static EstablishBpaAction Bpa(string id = "fuelBpa") => new()
    {
        Id = id, Vendor = "vendorD", Capability = "fuel", ScopeLines = ["delivery"], CallLimitCents = 5_000_000,
    };

    [Fact]
    public void Establishing_takes_shop_days_then_the_bpa_takes_calls()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1);
        var before = s.View.Vehicles.Count;
        var r = s.Apply(Bpa());
        Assert.True(r.Accepted, r.Message);
        Assert.Equal(before, s.View.Vehicles.Count);
        Assert.Contains(s.Work, w => w.Kind == WorkKind.EstablishVehicle && w.CompletedDay is null);
        while (s.View.Vehicles.All(v => v.Id != "fuelBpa") && s.Day < 10) s.AdvanceDay();
        var veh = s.View.Vehicles.Single(v => v.Id == "fuelBpa");
        Assert.Equal("bpa", veh.Kind);
        Assert.Equal(5_000_000, veh.CallLimitCents);
        Assert.Equal(0, s.Metrics["cost.obligatedCents"]);

        var call = new AwardAction { Id = "f1", Instrument = "bpaCall", Vendor = "vendorD", Vehicle = "fuelBpa", Site = "spokeBSite", Capability = "fuel",
                                     Lines = ["delivery"], Quantity = 500, DurationDays = 5, FundingLine = "om1" };
        var p = s.View.Preview(call);
        Assert.True(p.Accepted, string.Join("; ", p.HardStops));
        Assert.True(s.Apply(call).Accepted);
        Assert.Contains(s.View.ActionEditor("spokeBSite", "fuel")!.Vehicles, v => v.Id == "fuelBpa" && v.OpenToday);
    }

    [Fact]
    public void Calls_outside_the_new_scope_or_over_its_limit_are_hard_stops()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1);
        s.Apply(Bpa());
        while (s.View.Vehicles.All(v => v.Id != "fuelBpa")) s.AdvanceDay();
        var big = s.View.Preview(new AwardAction { Id = "f2", Instrument = "bpaCall", Vendor = "vendorD", Vehicle = "fuelBpa", Site = "spokeBSite", Capability = "fuel",
                                                   Lines = ["delivery"], Quantity = 1500, DurationDays = 30, FundingLine = "om1" });
        Assert.Contains(big.HardStops, h => h.Contains("call limit"));
    }

    [Fact]
    public void Bad_bpas_are_refused_and_change_nothing()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1);
        var p = s.View.PreviewEstablish(Bpa() with { ScopeLines = ["storage"] });
        Assert.False(p.Accepted);
        Assert.Contains(p.HardStops, h => h.Contains("does not offer"));
        Assert.False(s.Apply(Bpa() with { Vendor = "hubUtilities" }).Accepted);
        Assert.False(s.Apply(Bpa() with { CallLimitCents = 300_000_000 }).Accepted);
        Assert.False(s.Apply(Bpa() with { Id = "bosBpa" }).Accepted);
        Assert.False(s.Apply(Bpa() with { ScopeLines = [] }).Accepted);
        Assert.DoesNotContain(s.Work, w => w.Kind == WorkKind.EstablishVehicle);
        var ok = s.View.PreviewEstablish(Bpa());
        Assert.True(ok.Accepted);
        Assert.True(ok.ShopHoursMin > 0 && ok.EarliestStartMin > s.Day);
    }

    [Fact]
    public void Establish_round_trips_through_an_input_log()
    {
        var b = Repo.Load("spoke-b");
        var s = Session.Create(b, 4);
        s.Apply(Bpa());
        while (!s.Finished) s.AdvanceDay();
        var log = InputLog.Load(WriteTemp(s.ToInputLog().ToJson()));
        Assert.Equal(s.StateHash(), Replay.Run(b, log).StateHash());
        Assert.Contains("\"kind\": \"establishBpa\"", s.ToInputLog().ToJson());
    }

    private static string WriteTemp(string json)
    {
        var path = Path.Combine(Path.GetTempPath(), $"log-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, json);
        return path;
    }
}
