using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

/// <summary>M2R-35: the award editor knows what goes together before the player picks it.</summary>
public class AwardChoicesTests
{
    [Fact]
    public void Host_nation_support_cannot_be_bought_and_says_why()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1);
        var c = s.View.AwardChoices("spokeBSite", "lodging", vendorId: "hubUtilities")!;
        Assert.All(c.Instruments, i => Assert.False(i.Available));
        Assert.All(c.Instruments, i => Assert.Contains("host nation support", i.Reason));
        var hn = s.View.AwardChoices("spokeBSite", "lodging")!.Vendors.Single(v => v.Id == "hubUtilities");
        Assert.False(hn.Available);
        Assert.Equal("Main Island Utilities (host nation) is host nation support. It is requested under the host nation agreement, not bought with cash, the card or a contract.", hn.Reason);
    }

    [Fact]
    public void The_partner_force_is_the_J4s_and_a_vendor_that_takes_no_cash_greys_the_cash_buy()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1);
        var fuel = s.View.AwardChoices("spokeBSite", "fuel")!;
        Assert.Contains("the J4's to arrange", fuel.Vendors.Single(v => v.Id == "partnerForce").Reason);
        Assert.True(fuel.Vendors.Single(v => v.Id == "vendorE").Available);
        // Meridian Site Services takes neither cash nor the card.
        var power = s.View.AwardChoices("spokeBSite", "power", "req0007", vendorId: "vendorB")!;
        // Day 1 has no paying agent yet, which greys the cash buy for everyone; once there is one, the vendor is the reason.
        Assert.Equal("No paying agent with cash on hand yet.", power.Instruments.Single(i => i.Id == "sf44").Reason);
        Assert.True(s.Apply(new FindFinanceAction()).Accepted);
        for (var d = 0; d < 6; d++) s.AdvanceDay();
        Assert.Equal("Meridian Site Services does not take cash.", s.View.AwardChoices("spokeBSite", "power", "req0007", vendorId: "vendorB")!.Instruments.Single(i => i.Id == "sf44").Reason);
        Assert.True(power.Instruments.Single(i => i.Id == "purchaseOrder").Available);
        Assert.Equal("This shop holds no BPA with Meridian Site Services.", power.Instruments.Single(i => i.Id == "bpaCall").Reason);
    }

    [Fact]
    public void Picking_the_instrument_first_greys_the_vendors_it_cannot_reach()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1);
        var c = s.View.AwardChoices("spokeBSite", "power", "req0007", instrumentId: "bpaCall")!;
        Assert.True(c.Vendors.Single(v => v.Id == "vendorC").Available);
        Assert.All(c.Vendors.Where(v => v.Id != "vendorC"), v => Assert.False(v.Available));
        Assert.All(c.Funding, f => Assert.True(f.Available || f.Reason is not null));
        var cash = s.View.AwardChoices("spokeBSite", "power", "req0007", instrumentId: "sf44")!;
        Assert.All(cash.Funding, f => Assert.Equal("A cash buy is paid from the cash box, not a funding line.", f.Reason));
        Assert.All(cash.Vehicles, v => Assert.False(v.Available));
    }

    [Theory]
    [InlineData("spoke-b")]
    [InlineData("spoke-b-90")]
    [InlineData("sustainment")]
    [InlineData("tutorial")]
    public void Every_greyed_pair_is_one_the_game_would_refuse_and_every_reason_is_a_plain_sentence(string scenario)
    {
        var b = Repo.Load(scenario);
        foreach (var day in new[] { 1, 6, 12 })
        {
            var s = Session.Create(b, 1);
            for (var d = 1; d < day; d++) s.AdvanceDay();
            foreach (var site in b.Scenario.Sites)
                foreach (var cap in site.Capabilities)
                {
                    var open = s.View.AwardChoices(site.Id, cap.Id)!;
                    foreach (var v in open.Vendors)
                    {
                        var c = s.View.AwardChoices(site.Id, cap.Id, vendorId: v.Id)!;
                        foreach (var i in c.Instruments)
                        {
                            Assert.Equal(i.Available, i.Reason is null);
                            if (i.Reason is null) continue;
                            Assert.EndsWith(".", i.Reason);
                            Assert.True(char.IsUpper(i.Reason[0]), i.Reason);
                            Assert.DoesNotMatch(@"\b(vendor[A-Z]|req\d{4}|om\d|[a-z]+Bpa|[a-z]+Site)\b", i.Reason);
                            var vehicle = s.Vehicles.FirstOrDefault(x => x.Vendor == v.Id)?.Id;
                            var p = s.View.Preview(new AwardAction { Id = "probe", Instrument = i.Id, Vendor = v.Id, Vehicle = vehicle, Site = site.Id, Capability = cap.Id,
                                                                     Quantity = 1, DurationDays = 1, FundingLine = b.Scenario.FundingLines.FirstOrDefault()?.Id });
                            Assert.False(p.Accepted, $"{scenario} day {day}: {v.Label} by {i.Label} is greyed ({i.Reason}) but the game accepts it");
                        }
                    }
                }
        }
    }

    [Fact]
    public void Asking_changes_nothing()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1);
        var before = s.StateHash();
        s.View.AwardChoices("spokeBSite", "power", "req0007", "vendorC", "bpaCall");
        Assert.Equal(before, s.StateHash());
        Assert.Null(s.View.AwardChoices("nowhere", "power"));
    }
}
