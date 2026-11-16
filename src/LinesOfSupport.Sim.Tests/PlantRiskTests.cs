using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Policies;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>
/// DESK-59 (docs/design/desk-redesign.md D26): goods delivered carry no plant risk. The 16 Nov Claude play (note 12)
/// bought bottled water from Island Mart and was warned that it would fail more often without plant operators and
/// maintenance, parts Island Mart does not sell.
/// </summary>
public class PlantRiskTests
{
    private static readonly string[] Prep = ["gpcAccounts", "cashAndPayingAgent", "systemAccess"];

    [Fact]
    public void Bottled_water_from_a_shop_carries_no_operator_or_maintenance_warning()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        foreach (var vendor in new[] { "islandMart", "harborBottling" })
        {
            var gaps = s.View.Preview(new AwardAction
            {
                Id = "water", Instrument = "gpc", Vendor = vendor, Card = "tutWater", Quantity = 4500, StartDay = 2, DurationDays = 7,
            }).SoftGaps;
            Assert.DoesNotContain(gaps, g => g.Contains("operators", StringComparison.Ordinal) || g.Contains("maintenance", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void Forklifts_without_drivers_still_warn()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        while (s.Day < 4) s.AdvanceDay();
        var gaps = s.View.Preview(new AwardAction
        {
            Id = "lifts", Instrument = "purchaseOrder", Vendor = "coastalRentals", Card = "tutForklift", Quantity = 3,
            StartDay = 8, DurationDays = 13, FundingLine = "om1",
        }).SoftGaps;
        Assert.Contains(gaps, g => g.StartsWith("Without operators, it fails more often every day it runs.", StringComparison.Ordinal));
    }

    [Fact]
    public void A_plant_vendor_bought_without_its_operators_still_carries_the_risk()
    {
        // Spoke B: Third Island Water Works runs a plant and sells its operators; Coastal Provisions ships water in.
        var s = Session.Create(Repo.Load("spoke-b"), 1, []);
        AwardAction Water(string vendor) => new()
        {
            Id = "w", Instrument = "purchaseOrder", Vendor = vendor, Card = "req0012", Quantity = 1800, DurationDays = 30, FundingLine = "om1",
        };
        while (s.View.RequirementEditor("req0012") is null) s.AdvanceDay();
        Assert.Contains(s.View.Preview(Water("vendorF")).SoftGaps, g => g.StartsWith("Without plant operators", StringComparison.Ordinal));
        Assert.DoesNotContain(s.View.Preview(Water("vendorG")).SoftGaps, g => g.StartsWith("Without plant operators", StringComparison.Ordinal));
    }

    [Fact]
    public void No_goods_failure_is_put_down_to_a_part_left_out()
    {
        foreach (var (scenario, goods) in new[] { ("tutorial", "Island Mart"), ("tutorial", "Harbor Bottling"), ("spoke-b", "Coastal Provisions"), ("spoke-b", "Second Island Fuel") })
            foreach (var policy in new[] { "greedy", "textbook", "do-nothing" })
                for (var seed = 1; seed <= 20; seed++)
                {
                    var s = Replay.Run(Repo.Load(scenario), seed, PolicyCatalog.Create(policy));
                    foreach (var r in s.Records.Where(r => r.Kind == RecordKind.VendorFailure && r.Text.StartsWith(goods, StringComparison.Ordinal)))
                        Assert.DoesNotContain("(no ", r.Text, StringComparison.Ordinal);
                    foreach (var f in s.View.Debrief().Gaps.SelectMany(g => g.Factors).Where(f => f.Kind == GapFactorKind.VendorFailure && f.Text.StartsWith(goods, StringComparison.Ordinal)))
                        Assert.DoesNotContain("was bought without", f.Text, StringComparison.Ordinal);
                }
    }
}
