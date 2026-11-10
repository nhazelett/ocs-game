using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>M3-06: establishing an IDIQ, ordering from it, the minimum guarantee and the capacity event (SPEC 4.1).</summary>
public class IdiqTests
{
    private const long Ceiling = 30_000_000;      // $300,000
    private const long Guarantee = 1_500_000;     // 5% of the ceiling: rules idiqEstablish.obligatesMinimumGuaranteeFraction

    private static EstablishIdiqAction Idiq(string id = "fuelIdiq") => new()
    {
        Id = id, Vendor = "vendorD", Capability = "fuel", ScopeLines = ["delivery"], CeilingCents = Ceiling, FundingLine = "om1",
    };

    private static AwardAction Order(string id, long qty, int days, string site = "spokeBSite", string? line = "om1") => new()
    {
        Id = id, Instrument = "idiqOrder", Vendor = "vendorD", Vehicle = "fuelIdiq", Site = site, Capability = "fuel",
        Lines = ["delivery"], Quantity = qty, DurationDays = days, FundingLine = line,
    };

    /// <summary>Spoke B with the IDIQ applied on day 1 and played until it is awarded.</summary>
    private static Session Established(int seed = 1)
    {
        var s = Session.Create(Repo.Load("spoke-b"), seed);
        Assert.True(s.Apply(Idiq()).Accepted);
        while (s.View.Vehicles.All(v => v.Id != "fuelIdiq") && s.Day < 25) s.AdvanceDay();
        Assert.Contains(s.View.Vehicles, v => v.Id == "fuelIdiq");
        return s;
    }

    private static long LineObligated(Session s) => s.View.Funding.Single(f => f.Id == "om1").ObligatedCents;

    [Fact]
    public void Establishing_takes_8_to_15_shop_days_and_obligates_the_minimum_guarantee()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1);
        var preview = s.View.PreviewEstablish(Idiq());
        Assert.True(preview.Accepted, string.Join("; ", preview.HardStops));
        Assert.Equal(Guarantee, preview.TotalCents);
        Assert.Contains(preview.SoftGaps, g => g.Contains("owed to the vendor"));
        Assert.Contains(preview.SoftGaps, g => g.Contains("capacity"));
        // Shop days are person-days of 8 hours, slower while manual (system online day 12).
        Assert.True(preview.ShopHoursMin >= 8 * 8 && preview.ShopHoursMax >= 15 * 8);

        Assert.True(s.Apply(Idiq()).Accepted);
        Assert.Equal(0, LineObligated(s));
        Assert.Equal(0, s.Metrics["cost.obligatedCents"]);
        while (s.View.Vehicles.All(v => v.Id != "fuelIdiq")) s.AdvanceDay();
        var veh = s.View.Vehicles.Single(v => v.Id == "fuelIdiq");
        Assert.Equal("idiq", veh.Kind);
        Assert.Equal(Ceiling, veh.CeilingCents);
        Assert.Equal(Guarantee, LineObligated(s));
        // The guarantee is obligated money even before anything is ordered.
        Assert.Equal(Guarantee, s.Metrics["cost.obligatedCents"]);
        Assert.Contains(s.Records, r => r.Kind == RecordKind.WorkDone && r.Subject == "fuelIdiq" && r.Text.Contains("Minimum guarantee"));
        var option = s.View.ActionEditor("spokeBSite", "fuel")!.Vehicles.Single(v => v.Id == "fuelIdiq");
        Assert.Equal(Ceiling, option.CeilingCents);
        Assert.Equal(0, option.OrderedCents);
        Assert.Equal(Guarantee, option.GuaranteeUnorderedCents);
    }

    [Fact]
    public void Idiq_orders_are_greyed_until_an_idiq_is_in_place()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1);
        var before = s.View.ActionEditor("spokeBSite", "fuel")!.Instruments.Single(i => i.Id == "idiqOrder");
        Assert.Equal("no IDIQ known to this shop is open for ordering", before.UnavailableReason);
        Assert.True(before.Binding);
        var s2 = Established();
        Assert.Null(s2.View.ActionEditor("spokeBSite", "fuel")!.Instruments.Single(i => i.Id == "idiqOrder").UnavailableReason);
    }

    [Fact]
    public void Bad_idiqs_are_refused_and_change_nothing()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1);
        Assert.Contains(s.View.PreviewEstablish(Idiq() with { ScopeLines = ["storage"] }).HardStops, h => h.Contains("does not offer"));
        Assert.Contains(s.View.PreviewEstablish(Idiq() with { CeilingCents = 0 }).HardStops, h => h.Contains("ceiling"));
        Assert.Contains(s.View.PreviewEstablish(Idiq() with { MinimumGuaranteeCents = 100_00 }).HardStops, h => h.Contains("nominal"));
        Assert.Contains(s.View.PreviewEstablish(Idiq() with { MinimumGuaranteeCents = Ceiling + 1 }).HardStops, h => h.Contains("cannot exceed the ceiling"));
        Assert.Contains(s.View.PreviewEstablish(Idiq() with { FundingLine = "" }).HardStops, h => h.Contains("funding line"));
        Assert.Contains(s.View.PreviewEstablish(Idiq() with { CeilingCents = 2_000_000_000 }).HardStops, h => h.Contains("has $900,000 left"));
        Assert.Contains(s.View.PreviewEstablish(Idiq() with { Id = "bosBpa" }).HardStops, h => h.Contains("already exists"));
        Assert.Contains(s.View.PreviewEstablish(Idiq() with { ScopeLines = [] }).HardStops, h => h.Contains("scope line"));
        Assert.True(s.Apply(Idiq()).Accepted);
        Assert.False(s.Apply(Idiq()).Accepted);                       // same id while the first is in work
        Assert.False(s.Apply(new EstablishBpaAction { Id = "fuelIdiq", Vendor = "vendorD", Capability = "fuel", ScopeLines = ["delivery"] }).Accepted);
        Assert.Single(s.Work, w => w.Kind == WorkKind.EstablishVehicle);
    }

    [Fact]
    public void Orders_spend_the_guarantee_first_then_the_funding_line()
    {
        var s = Established();
        // 200 gal/day x $7 x 5 days = $7,000 plus mobilization: inside the $15,000 guarantee.
        var small = Order("o1", 200, 5);
        var p = s.View.Preview(small);
        Assert.True(p.Accepted, string.Join("; ", p.HardStops));
        Assert.True(p.TotalCents < Guarantee);
        Assert.Contains(p.SoftGaps, g => g.Contains("minimum guarantee"));
        Assert.DoesNotContain(p.SoftGaps, g => g.Contains("may decline"));
        var obligatedBefore = LineObligated(s);
        Assert.True(s.Apply(small).Accepted);
        while (s.Arrangements.All(a => a.Id != "o1")) s.AdvanceDay();
        var o1 = s.Arrangements.Single(a => a.Id == "o1");
        Assert.Equal(p.TotalCents, o1.ObligatedCents);
        Assert.Equal(p.TotalCents, o1.FromGuaranteeCents);
        Assert.Equal(obligatedBefore, LineObligated(s));               // no new money: the guarantee covered it
        var idiq = s.View.Idiqs.Single(i => i.VehicleId == "fuelIdiq");
        Assert.Equal(Guarantee - p.TotalCents, idiq.GuaranteeUnorderedCents);
        // Obligated cost counts the guarantee once, whether ordered or not.
        Assert.Equal(Guarantee, s.Metrics["cost.obligatedCents"]);

        var big = Order("o2", 300, 10);
        var p2 = s.View.Preview(big);
        Assert.True(p2.Accepted, string.Join("; ", p2.HardStops));
        Assert.True(s.Apply(big).Accepted);
        while (s.Arrangements.All(a => a.Id != "o2")) s.AdvanceDay();
        var o2 = s.Arrangements.Single(a => a.Id == "o2");
        Assert.Equal(Guarantee - p.TotalCents, o2.FromGuaranteeCents);
        Assert.Equal(obligatedBefore + p2.TotalCents - o2.FromGuaranteeCents, LineObligated(s));
        Assert.Equal(0, s.View.Idiqs.Single(i => i.VehicleId == "fuelIdiq").GuaranteeUnorderedCents);
        Assert.Equal(p.TotalCents + p2.TotalCents, s.Metrics["cost.obligatedCents"]);
        Assert.DoesNotContain(s.Records, r => r.Kind == RecordKind.Declined);
    }

    [Fact]
    public void A_guarantee_never_ordered_stays_obligated_to_the_end()
    {
        var s = Established();
        while (!s.Finished) s.AdvanceDay();
        Assert.Equal(Guarantee, s.Metrics["cost.obligatedCents"]);
        Assert.Equal(Guarantee, LineObligated(s));
    }

    [Fact]
    public void Ceiling_scope_vehicle_and_ordering_period_are_hard_stops()
    {
        var s = Established();
        // 700 gal/day x $7 x 30 days = $147,000 a time: the third order passes the $300,000 ceiling.
        Assert.True(s.Apply(Order("c1", 700, 30, "hubSite")).Accepted);
        Assert.True(s.Apply(Order("c2", 100, 20)).Accepted);
        var over = s.View.Preview(Order("c3", 700, 30));
        Assert.Contains(over.HardStops, h => h.Contains("over its ceiling of $300,000"));
        Assert.Contains(s.View.Preview(Order("c4", 100, 5) with { Lines = ["storage"] }).HardStops, h => h.Contains("outside fuelIdiq's scope") || h.Contains("does not offer"));
        Assert.Contains(s.View.Preview(Order("c5", 100, 5) with { Vehicle = "bosBpa" }).HardStops, h => h.Contains("is not an IDIQ"));
        Assert.Contains(s.View.Preview(Order("c6", 100, 5) with { Vehicle = null }).HardStops, h => h.Contains("needs an IDIQ vehicle"));

        var t = Session.Create(Repo.Load("spoke-b"), 1);
        t.Apply(Idiq() with { OrderingPeriodDays = 1 });
        while (t.View.Vehicles.All(v => v.Id != "fuelIdiq")) t.AdvanceDay();
        t.AdvanceDay(); t.AdvanceDay();
        Assert.Contains(t.View.Preview(Order("late", 100, 5)).HardStops, h => h.Contains("ordering period ended"));
    }

    [Fact]
    public void An_order_above_capacity_is_accepted_and_delivers_short_with_a_capacity_event()
    {
        var s = Established();
        // A purchase order for more than vendorD's 700 gal/day is refused at award ...
        var po = new AwardAction { Id = "po", Instrument = "purchaseOrder", Vendor = "vendorD", Site = "spokeBSite", Capability = "fuel",
                                   Lines = ["delivery"], Quantity = 900, DurationDays = 10, FundingLine = "om1" };
        Assert.Contains(s.View.Preview(po).HardStops, h => h.Contains("can supply"));
        // ... an IDIQ order is binding, so it is accepted, with a soft gap saying what will happen.
        var lockUp = Order("hubFuel", 700, 20, "hubSite");
        var spoke = Order("spokeFuel", 400, 15);
        Assert.True(s.Apply(lockUp).Accepted);
        var p = s.View.Preview(spoke);
        Assert.True(p.Accepted, string.Join("; ", p.HardStops));
        Assert.Contains(p.SoftGaps, g => g.Contains("capacity event"));
        Assert.True(s.Apply(spoke).Accepted);
        while (s.Arrangements.All(a => a.Id != "spokeFuel" || a.Status != ArrangementStatus.Active) && !s.Finished) s.AdvanceDay();
        s.AdvanceDay();
        var ev = s.Records.First(r => r.Kind == RecordKind.VendorCapacity);
        Assert.Equal("spokeFuel", ev.Subject);                          // the older order is served first
        Assert.Contains("gets 0 of its 400", ev.Text);
        Assert.DoesNotContain(s.Records, r => r.Kind == RecordKind.VendorCapacity && r.Subject == "hubFuel");
        // One record per episode, not one a day.
        s.AdvanceDay(); s.AdvanceDay();
        Assert.Single(s.Records, r => r.Kind == RecordKind.VendorCapacity && r.Subject == "spokeFuel");
        // A capacity shortfall is not a performance issue: nobody failed, the vendor ran out.
        Assert.DoesNotContain(s.View.PerformanceIssues, i => i.ArrangementId == "spokeFuel");
        while (!s.Finished) s.AdvanceDay();
        var debrief = s.View.Debrief();
        var factor = debrief.Gaps.Where(g => g.SiteId == "spokeBSite" && g.CapabilityId == "fuel")
                                 .SelectMany(g => g.Factors).FirstOrDefault(f => f.Kind == GapFactorKind.VendorCapacity);
        Assert.NotNull(factor);
        Assert.Equal(GapTag.Avoidable, factor!.Tag);
        Assert.Contains(ev.Id, factor.RecordIds);
        Assert.Contains(s.Records, r => r.Kind == RecordKind.Shortfall && r.Subject == "spokeBSite.fuel" && r.CausedBy.Contains(ev.Id));
    }

    [Fact]
    public void Terminating_an_order_paid_from_the_guarantee_returns_nothing_to_the_line()
    {
        var s = Established();
        Assert.True(s.Apply(Order("g1", 200, 5)).Accepted);
        while (s.Arrangements.All(a => a.Id != "g1")) s.AdvanceDay();
        var obligated = LineObligated(s);
        Assert.True(s.Apply(new TerminateAction { Arrangement = "g1" }).Accepted);
        for (var i = 0; i < 4; i++) s.AdvanceDay();
        Assert.Equal(obligated, LineObligated(s));
        Assert.Equal(s.Arrangements.Single(a => a.Id == "g1").FromGuaranteeCents, s.Arrangements.Single(a => a.Id == "g1").ObligatedCents);
    }

    [Fact]
    public void Idiq_games_round_trip_through_input_logs_and_saves()
    {
        var b = Repo.Load("spoke-b");
        var s = Session.Create(b, 4);
        s.Apply(Idiq());
        while (s.View.Vehicles.All(v => v.Id != "fuelIdiq")) s.AdvanceDay();
        s.Apply(Order("r1", 700, 20, "hubSite"));
        s.Apply(Order("r2", 300, 15));
        for (var i = 0; i < 3; i++) s.AdvanceDay();
        var save = InputLog.Load(WriteTemp(s.ToSave().ToJson()));
        var resumed = Replay.Resume(b, save);
        Assert.Equal(s.StateHash(), resumed.StateHash());
        while (!s.Finished) { s.AdvanceDay(); resumed.AdvanceDay(); }
        Assert.Equal(s.StateHash(), resumed.StateHash());
        var log = InputLog.Load(WriteTemp(s.ToInputLog().ToJson()));
        Assert.Equal(s.StateHash(), Replay.Run(b, log).StateHash());
        Assert.Contains("\"kind\": \"establishIdiq\"", s.ToInputLog().ToJson());
    }

    private static string WriteTemp(string json)
    {
        var path = Path.Combine(Path.GetTempPath(), $"log-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, json);
        return path;
    }
}
