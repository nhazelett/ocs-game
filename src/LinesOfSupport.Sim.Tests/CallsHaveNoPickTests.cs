using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-65 (docs/design/desk-redesign.md D27): fast, cheap and good is the open market's trade. A BPA call or an
/// IDIQ order is priced and timed by its vehicle, so it shows no pick, pays no premium, and old logs that carry a pick on a
/// call load and ignore it.</summary>
public class CallsHaveNoPickTests
{
    private static AwardAction Call(params string[] pick) => new()
    {
        Id = "gens", Instrument = "bpaCall", Vendor = "vendorC", Vehicle = "bosBpa", Card = "req0007",
        Quantity = 360, StartDay = 7, DurationDays = 60, FundingLine = "om1",
        Priorities = pick.Length == 0 ? null : [.. pick],
    };

    [Theory]
    [InlineData("fast", "good")]
    [InlineData("cheap", "good")]
    [InlineData("fast", "cheap")]
    [InlineData("fast")]
    [InlineData("fast", "cheap", "good")]
    public void A_call_ignores_any_pick(params string[] pick)
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1);
        var plain = s.View.Preview(Call());
        var picked = s.View.Preview(Call(pick));
        Assert.True(plain.Accepted, string.Join("; ", plain.HardStops));
        Assert.True(picked.Accepted, string.Join("; ", picked.HardStops));
        Assert.Equal(plain.DailyCents, picked.DailyCents);
        Assert.Equal(plain.TotalCents, picked.TotalCents);
        Assert.Equal(plain.ShopHoursMax, picked.ShopHoursMax);
        Assert.Equal(plain.EarliestStartMax, picked.EarliestStartMax);
        Assert.Equal(plain.Trade, picked.Trade);
    }

    [Fact]
    public void A_call_preview_says_its_vehicle_sets_the_price_and_ordering_time()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1);
        var p = s.View.Preview(Call("fast", "good"));
        Assert.NotNull(p.Trade);
        Assert.Equal("The Harbor Equipment Co BPA sets the price and the time to place a call, shown below, so there is no fast, cheap or good to pick on a call.", p.Trade);
        Assert.DoesNotContain("premium", p.Trade);
        Assert.DoesNotContain("bosBpa", p.Trade);
    }

    [Fact]
    public void A_call_carries_no_pick_into_the_record_or_the_arrangement()
    {
        var a = Session.Create(Repo.Load("spoke-b"), 1);
        var b = Session.Create(Repo.Load("spoke-b"), 1);
        Assert.True(a.Apply(Call()).Accepted);
        Assert.True(b.Apply(Call("fast", "cheap")).Accepted);
        Assert.DoesNotContain("Picked", b.Records.Last(r => r.Subject == "gens").Text);
        while (b.Arrangements.All(x => x.Id != "gens") && b.Day < 20) { a.AdvanceDay(); b.AdvanceDay(); }
        var plain = a.Arrangements.Single(x => x.Id == "gens");
        var picked = b.Arrangements.Single(x => x.Id == "gens");
        Assert.Null(picked.Pick);
        Assert.Equal(plain.ReliabilityPermille, picked.ReliabilityPermille);
        Assert.Equal(plain.DailyCostCents, picked.DailyCostCents);
    }

    [Fact]
    public void An_idiq_order_has_no_pick_and_says_its_ordering_time()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1);
        Assert.True(s.Apply(new EstablishIdiqAction
        {
            Id = "fuelIdiq", Vendor = "vendorD", Capability = "fuel", ScopeLines = ["delivery"], CeilingCents = 30_000_000, FundingLine = "om1",
        }).Accepted);
        while (s.View.Vehicles.All(v => v.Id != "fuelIdiq") && s.Day < 25) s.AdvanceDay();
        var order = new AwardAction
        {
            Id = "fuel", Instrument = "idiqOrder", Vendor = "vendorD", Vehicle = "fuelIdiq", Site = "spokeBSite", Capability = "fuel",
            Lines = ["delivery"], Quantity = 1000, DurationDays = 5, FundingLine = "om1",
        };
        var plain = s.View.Preview(order);
        var picked = s.View.Preview(order with { Priorities = ["fast", "good"] });
        Assert.True(picked.Accepted, string.Join("; ", picked.HardStops));
        Assert.Equal(plain.TotalCents, picked.TotalCents);
        Assert.EndsWith("sets the price and the time to place an order, shown below, so there is no fast, cheap or good to pick on an order.", picked.Trade);
    }

    [Fact]
    public void Open_market_written_buys_keep_pick_two()
    {
        var rules = Repo.Load("spoke-b").Rules.Instruments;
        Assert.True(rules["purchaseOrder"].PicksTwo);
        Assert.True(rules["uca"].PicksTwo);
        Assert.False(rules["bpaCall"].PicksTwo);
        Assert.False(rules["idiqOrder"].PicksTwo);
        Assert.False(rules["gpc"].PicksTwo);
        Assert.False(rules["sf44"].PicksTwo);
    }

    [Fact]
    public void The_first_play_log_loads_and_its_call_pays_no_premium()
    {
        // The 16 Nov Claude play picked fast and good on the forklift BPA call (play note 18) and paid 15% for it.
        var log = InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-16", "input-log.json"));
        var s = Replay.Run(Repo.Load("tutorial"), log, stopAfterDay: 4);
        Assert.Contains(s.Records, r => r.Subject == "player-award-2");
        Assert.DoesNotContain(s.Records, r => r.Subject == "player-award-2" && r.Text.Contains("Picked", StringComparison.Ordinal));
        var later = Replay.Run(Repo.Load("tutorial"), log, stopAfterDay: 6);
        Assert.Contains(later.Records, r => r.Subject == "player-award-3" && r.Text.Contains("Picked fast and good.", StringComparison.Ordinal));
    }
}
