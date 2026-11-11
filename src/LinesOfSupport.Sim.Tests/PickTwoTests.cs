using System.Text.Json.Nodes;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-06 (docs/design/desk-redesign.md D7): fast, cheap, good, pick two, on written contracts. Each pick costs
/// the third: competing takes shop days, speed with quality costs a premium, the first quote raises failure odds.</summary>
public class PickTwoTests
{
    private static AwardAction PowerPo(params string[] pick) => new()
    {
        Id = "po", Instrument = "purchaseOrder", Vendor = "vendorA", Site = "spokeBSite", Capability = "power",
        Lines = ["generation", "maintenance"], Quantity = 60, DurationDays = 10, FundingLine = "om1",
        Priorities = pick.Length == 0 ? null : [.. pick],
    };

    [Fact]
    public void No_pick_changes_nothing()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1);
        var p = s.View.Preview(PowerPo());
        Assert.True(p.Accepted);
        Assert.Null(p.Trade);
        Assert.True(s.Apply(PowerPo()).Accepted);
        Assert.DoesNotContain("Picked", s.Records.Last(r => r.Subject == "po").Text);
    }

    [Fact]
    public void Fast_and_good_pays_the_premium()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1);
        var plain = s.View.Preview(PowerPo());
        var paid = s.View.Preview(PowerPo("good", "fast"));
        Assert.True(paid.Accepted);
        Assert.Equal(plain.DailyCents * 1150 / 1000, paid.DailyCents);
        Assert.InRange(paid.TotalCents, plain.TotalCents * 1150 / 1000 - 2, plain.TotalCents * 1150 / 1000 + 2);
        Assert.Equal(plain.ShopHoursMax, paid.ShopHoursMax);
        Assert.StartsWith("Fast and good: you pay a 15% premium", paid.Trade);
        Assert.True(s.Apply(PowerPo("fast", "good")).Accepted);
        Assert.EndsWith("Picked fast and good.", s.Records.Last(r => r.Subject == "po").Text);
    }

    [Fact]
    public void Cheap_and_good_takes_more_shop_work_at_the_plain_price()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1);
        var plain = s.View.Preview(PowerPo());
        var competed = s.View.Preview(PowerPo("cheap", "good"));
        Assert.Equal(plain.TotalCents, competed.TotalCents);
        Assert.True(competed.ShopHoursMin > plain.ShopHoursMin);
        Assert.True(competed.EarliestStartMin > plain.EarliestStartMin);
        Assert.Equal("Cheap and good: you compete it among vendors, which adds about 2 days of shop work before award.", competed.Trade);

        var a = Session.Create(Repo.Load("spoke-b"), 1);
        var b = Session.Create(Repo.Load("spoke-b"), 1);
        Assert.True(a.Apply(PowerPo()).Accepted);
        Assert.True(b.Apply(PowerPo("cheap", "good")).Accepted);
        var hours = (Session x) => x.View.Work.Single(w => w.Kind == WorkKind.Award).HoursTotalMilli;
        Assert.True(hours(b) > hours(a));
    }

    [Fact]
    public void Fast_and_cheap_lowers_the_vendor_reliability_on_the_contract()
    {
        var a = Session.Create(Repo.Load("spoke-b"), 1);
        var b = Session.Create(Repo.Load("spoke-b"), 1);
        Assert.True(a.Apply(PowerPo()).Accepted);
        var quick = b.View.Preview(PowerPo("fast", "cheap"));
        Assert.StartsWith("Fast and cheap: you take the first quote", quick.Trade);
        Assert.True(b.Apply(PowerPo("fast", "cheap")).Accepted);
        while (a.Arrangements.All(x => x.Id != "po")) { a.AdvanceDay(); b.AdvanceDay(); }
        var plain = a.Arrangements.Single(x => x.Id == "po");
        var first = b.Arrangements.Single(x => x.Id == "po");
        Assert.Equal(plain.ReliabilityPermille - 15, first.ReliabilityPermille);
        Assert.Equal("fast+cheap", first.Pick);
        Assert.Null(plain.Pick);
    }

    [Theory]
    [InlineData("sf44", new[] { "fast", "cheap" }, "Fast, cheap or good is picked on written contracts only")]
    [InlineData("purchaseOrder", new[] { "fast" }, "Pick two of fast, cheap and good")]
    [InlineData("purchaseOrder", new[] { "fast", "cheap", "good" }, "Pick two of fast, cheap and good")]
    [InlineData("purchaseOrder", new[] { "fast", "fast" }, "Pick two of fast, cheap and good")]
    public void A_pick_must_be_two_of_the_three_on_a_written_contract(string instrument, string[] pick, string stop)
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1);
        var p = s.View.Preview(PowerPo() with { Instrument = instrument, Priorities = [.. pick] });
        Assert.Contains(stop, p.HardStops);
    }

    [Fact]
    public void The_debrief_names_the_first_quote_when_the_vendor_falls_down()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"los-pick-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        foreach (var f in Directory.GetFiles(Path.Combine(Repo.Root, "scenarios", "rules")))
        {
            var root = JsonNode.Parse(File.ReadAllText(f))!;
            if (root["pickTwo"] is JsonObject pt) pt["firstQuoteFailurePermille"] = 1000;
            File.WriteAllText(Path.Combine(dir, Path.GetFileName(f)), root.ToJsonString());
        }
        var s = Session.Create(ScenarioBundle.Load(Repo.Scenario("spoke-b"), dir), 1);
        Assert.True(s.Apply(PowerPo("fast", "cheap") with { DurationDays = 20 }).Accepted);
        while (s.Day < 20) s.AdvanceDay();
        var factors = s.View.Debrief().Gaps.SelectMany(g => g.Factors).Where(f => f.Kind == Views.GapFactorKind.VendorFailure).ToList();
        Assert.NotEmpty(factors);
        Assert.All(factors, f => Assert.Contains("You took the first quote (fast and cheap)", f.Text));
    }
}
