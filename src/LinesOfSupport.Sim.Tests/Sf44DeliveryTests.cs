using System.Text.Json.Nodes;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;

namespace LinesOfSupport.Sim.Tests;

/// <summary>
/// DESK-09 (D11): an SF 44 for more than one day is paid in cash at each delivery, and each delivery is a trip. A closed
/// link to an off-island site stops the delivery and the payment; a delivery the cash cannot cover does not happen.
/// </summary>
public class Sf44DeliveryTests
{
    private static ScenarioBundle Modified(string id, Action<JsonNode> edit)
    {
        var root = JsonNode.Parse(File.ReadAllText(Path.Combine(Repo.Scenario(id), "scenario.json")))!;
        edit(root);
        var dir = Path.Combine(Path.GetTempPath(), $"los-sf44-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "scenario.json"), root.ToJsonString());
        return ScenarioBundle.Load(dir, Path.Combine(Repo.Root, "scenarios", "rules"));
    }

    private static void To(Session s, int day)
    {
        while (s.Day < day && !s.Finished) s.AdvanceDay();
    }

    private static AwardAction Waste(int days = 8, string? assignee = null) => new()
    {
        Day = 9, Id = "waste", Instrument = "sf44", Vendor = "sanitation", Card = "tutWaste",
        Quantity = 3600, StartDay = 13, DurationDays = days, FundingLine = "om1", Assignee = assignee,
    };

    private static Session TutorialAt9(ScenarioBundle? b = null)
    {
        var s = Session.Create(b ?? Repo.Load("tutorial"), 1, ["gpcAccounts", "cashAndPayingAgent", "systemAccess"]);
        To(s, 9);
        return s;
    }

    [Fact]
    public void Cash_goes_out_at_each_delivery_not_at_the_award()
    {
        var s = TutorialAt9();
        var cash = s.CashCents;
        Assert.True(s.Apply(Waste()).Accepted);
        var a = s.Arrangements.Single(x => x.Id == "waste");
        Assert.True(a.PaidPerDelivery);
        Assert.Equal(cash, s.CashCents);
        Assert.Equal(0, a.ObligatedCents);
        Assert.Contains("Paid in cash at each delivery, $144 a day", s.Records.Last(r => r.Subject == "waste" && r.Kind == RecordKind.Action).Text);
        To(s, 13);
        var before = s.CashCents;
        s.AdvanceDay();
        Assert.Equal(before - a.DailyCostCents - a.MobilizationCents, s.CashCents);
        Assert.Equal(a.DailyCostCents + a.MobilizationCents, a.ObligatedCents);
        To(s, 21);
        Assert.Equal(a.IncurredCents, a.ObligatedCents);
        Assert.Equal(8 * a.DailyCostCents + a.MobilizationCents, a.ObligatedCents);
    }

    [Fact]
    public void Each_delivery_is_a_trip_for_whoever_bought_it()
    {
        var s = TutorialAt9();
        Assert.True(s.Apply(Waste(assignee: "novak")).Accepted);
        To(s, 14);
        var trips = s.Work.Where(w => w.Label == "SF 44 delivery and payment at Hub").ToList();
        Assert.Single(trips);
        Assert.Equal("novak", trips[0].Assignee);
        Assert.Equal(13, trips[0].CreatedDay);
        Assert.NotNull(trips[0].CompletedDay);
        Assert.Contains("a trip for SrA Eli Novak and the paying agent: 2 shop hours", s.Records.Last(r => r.Subject == "waste" && r.Kind == RecordKind.Action).Text);
        To(s, 21);
        Assert.Equal(8, s.Work.Count(w => w.Label == "SF 44 delivery and payment at Hub"));
    }

    [Fact]
    public void A_delivery_the_cash_box_cannot_cover_does_not_happen_and_says_so()
    {
        // $300 in the box: the first delivery ($144 and mobilization) fits, the box runs dry after that.
        var s = TutorialAt9(Modified("tutorial", sc => sc["shop"]!["cashOnHandCents"] = 30000));
        Assert.True(s.Apply(Waste()).Accepted);
        To(s, 21);
        var a = s.Arrangements.Single(x => x.Id == "waste");
        var held = s.Records.Where(r => r.Subject == "waste" && r.Text.StartsWith("No delivery on")).ToList();
        Assert.NotEmpty(held);
        Assert.Contains("it is paid in cash at the door", held[0].Text);
        Assert.Contains("Finance tops it up on day", held[0].Text);
        // Nothing was delivered or owed on a held day: what was paid is what was incurred, and it is short of the full buy.
        Assert.Equal(a.IncurredCents, a.ObligatedCents);
        Assert.True(a.ObligatedCents < 8 * a.DailyCostCents + a.MobilizationCents);
        Assert.Contains(s.Records, r => r.Kind == RecordKind.Shortfall && r.Text.Contains("no cash for the SF 44 delivery"));
    }

    [Fact]
    public void A_closed_barge_stops_the_delivery_and_the_payment()
    {
        // Spoke B: the barge is closed for three days from day 16 (bargeWeather).
        var s = Session.Create(Repo.Load("spoke-b"), 1, ["cashAndPayingAgent"]);
        To(s, 5);
        var r = s.Apply(new AwardAction
        {
            Id = "fuelCash", Instrument = "sf44", Vendor = "vendorE", Site = "spokeBSite", Capability = "fuel",
            Quantity = 100, StartDay = 14, DurationDays = 8,
        });
        Assert.True(r.Accepted, string.Join("; ", r.HardStops));
        Assert.Contains("6 shop hours", s.Records.Last(x => x.Subject == "fuelCash" && x.Kind == RecordKind.Action).Text);
        To(s, 22);
        var a = s.Arrangements.Single(x => x.Id == "fuelCash");
        var held = s.Records.Where(x => x.Subject == "fuelCash" && x.Text.StartsWith("No delivery on")).ToList();
        Assert.Equal([16, 17, 18], held.Select(x => x.Day));
        Assert.All(held, x => Assert.Contains("is not running, so the goods and the cash both stayed on", x.Text));
        Assert.Equal(5 * a.DailyCostCents + a.MobilizationCents, a.ObligatedCents);
        Assert.Equal(a.IncurredCents, a.ObligatedCents);
    }

    [Fact]
    public void The_first_delivery_has_to_fit_the_cash_box_and_a_one_day_buy_is_paid_on_the_spot()
    {
        var s = TutorialAt9(Modified("tutorial", sc => sc["shop"]!["cashOnHandCents"] = 10000));
        var stop = s.Apply(Waste());
        Assert.False(stop.Accepted);
        Assert.Contains(stop.HardStops, h => h.Contains("the first delivery costs"));

        var t = TutorialAt9();
        var cash = t.CashCents;
        Assert.True(t.Apply(Waste(days: 1)).Accepted);
        var one = t.Arrangements.Single(x => x.Id == "waste");
        Assert.False(one.PaidPerDelivery);
        Assert.Equal(cash - one.ObligatedCents, t.CashCents);
        Assert.Contains("paid on the spot", t.Records.Last(r => r.Subject == "waste" && r.Kind == RecordKind.Action).Text);
    }
}
