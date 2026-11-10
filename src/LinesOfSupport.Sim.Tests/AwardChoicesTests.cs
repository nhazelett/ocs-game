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
        Assert.All(c.Instruments, i => Assert.Contains("agreement made before you arrived", i.Reason));
        var hn = s.View.AwardChoices("spokeBSite", "lodging")!.Vendors.Single(v => v.Id == "hubUtilities");
        Assert.False(hn.Available);
        // Spoke B has no host nation agreement this office can add to (M2R-37): the reason does not point at a request.
        Assert.Equal("Main Island Utilities (host nation) supports the force under an agreement made before you arrived. This office cannot buy from it or add to that support.", hn.Reason);
    }

    [Fact]
    public void Host_nation_reason_names_a_request_only_where_the_agreement_covers_it()
    {
        // Sustainment's agreement covers water and fuel, not power (M2R-37).
        var s = Session.Create(Repo.Load("sustainment"), 1);
        var fuel = s.View.AwardChoices("hubSite", "fuel")!.Vendors.Single(v => v.Id == "hubUtilities");
        Assert.False(fuel.Available);
        Assert.Equal("Main Island Utilities (host nation) is asked for with a host nation request under the agreement, not bought with cash, the card or a contract.", fuel.Reason);
        var power = s.View.AwardChoices("hubSite", "power")!.Vendors.Single(v => v.Id == "hubUtilities");
        Assert.Contains("agreement made before you arrived", power.Reason);
        Assert.DoesNotContain("request", power.Reason);
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

/// <summary>DESK-01: this office cannot end support that is not its own contract.</summary>
public class NotOursToEndTests
{
    [Theory]
    [InlineData("tutorial")]
    [InlineData("spoke-b")]
    public void Host_nation_and_ACSA_support_cannot_be_terminated_and_the_game_is_told_why(string scenario)
    {
        var s = Session.Create(Repo.Load(scenario), 1);
        foreach (var a in s.Arrangements.Where(a => a.Instrument is "hostNation" or "acsa").ToList())
        {
            var why = s.View.TerminateBlocked(a.Id);
            Assert.NotNull(why);
            Assert.EndsWith("It is not this office's contract to end.", why);
            Assert.True(char.IsUpper(why![0]));
            var r = s.Apply(new TerminateAction { Arrangement = a.Id });
            Assert.False(r.Accepted);
            Assert.Contains(why, r.HardStops);
        }
        for (var d = 0; d < 5; d++) s.AdvanceDay();
        Assert.All(s.Arrangements.Where(a => a.Instrument is "hostNation" or "acsa" && a.StartDay <= 1), a => Assert.Equal(ArrangementStatus.Active, a.Status));
    }

    [Fact]
    public void The_offices_own_contract_can_still_be_terminated()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1);
        var own = s.Arrangements.First(a => a.Instrument == "bpaCall");
        Assert.Null(s.View.TerminateBlocked(own.Id));
        Assert.True(s.Apply(new TerminateAction { Arrangement = own.Id }).Accepted);
    }
}
