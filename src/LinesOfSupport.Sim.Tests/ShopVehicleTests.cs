using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-10 (D5): a vehicle for the contracting office, bought on the shop's own initiative, cuts the hours of
/// vendor meetings and SF 44 trips on the shop's island. One rule number.</summary>
public class ShopVehicleTests
{
    private static readonly string[] Prep = ["gpcAccounts", "cashAndPayingAgent", "systemAccess"];

    private static AwardAction Vehicle(int day = 1, int start = 2, int days = 19) => new()
    {
        Day = day, Id = "officeVan", Instrument = "gpc", Vendor = "coastalRentals", Site = "hubSite", Capability = "shopVehicle",
        Quantity = 1, StartDay = start, DurationDays = days, FundingLine = "om1",
    };

    private static void To(Session s, int day)
    {
        while (s.Day < day && !s.Finished) s.AdvanceDay();
    }

    [Fact]
    public void The_vehicle_is_on_the_own_initiative_list_with_who_sells_it()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        var v = Assert.Single(s.View.OwnInitiative);
        Assert.Equal(("hubSite", "shopVehicle"), (v.SiteId, v.CapabilityId));
        Assert.Equal(["coastalRentals"], v.Vendors);
        Assert.False(v.Have);
        Assert.Equal("Nobody will ask for it. It gets you downtown: while you have one, a vendor meeting or an SF 44 delivery trip on Main Island takes half the hours.", v.Why);
    }

    [Fact]
    public void With_a_vehicle_a_vendor_meeting_takes_half_the_hours()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        var meet = new MeetVendorAction { Vendor = "sanitation" };
        Assert.Equal(s.Rules.GoingToPeople.MeetVendor.ShopHours, s.View.PreviewVisit(meet).ShopHoursMin);
        var r = s.Apply(Vehicle());
        Assert.True(r.Accepted, string.Join("; ", r.HardStops));
        // A day's notice and the card's start lag: it is running by day 4.
        To(s, 4);
        Assert.True(s.View.HasShopVehicle);
        Assert.True(s.View.OwnInitiative.Single().Have);
        Assert.Equal((s.Rules.GoingToPeople.MeetVendor.ShopHours + 1) / 2, s.View.PreviewVisit(meet).ShopHoursMin);
        var applied = s.Apply(meet);
        Assert.True(applied.Accepted, string.Join("; ", applied.HardStops));
        Assert.Contains(s.Work, w => w.Payload is not null && w.Label.Contains("Main Island Sanitation") && w.HoursTotalMilli == 1000);
    }

    [Fact]
    public void With_a_vehicle_an_SF_44_trip_on_the_island_takes_half_the_hours()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        Assert.True(s.Apply(Vehicle()).Accepted);
        To(s, 9);
        Assert.True(s.Apply(new AwardAction
        {
            Id = "waste", Instrument = "sf44", Vendor = "sanitation", Card = "tutWaste", Quantity = 3600, StartDay = 13, DurationDays = 8,
        }).Accepted);
        Assert.Contains("1 shop hour.", s.Records.Last(r => r.Subject == "waste" && r.Kind == RecordKind.Action).Text);
        To(s, 21);
        var trips = s.Work.Where(w => w.Label == "SF 44 delivery and payment at Hub").ToList();
        Assert.Equal(8, trips.Count);
        Assert.All(trips, w => Assert.Equal(1000, w.HoursTotalMilli));
    }

    [Fact]
    public void The_vehicle_is_not_a_site_need_so_it_is_not_scored_and_nobody_argues_about_it()
    {
        string Output(bool van)
        {
            var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
            if (van) s.Apply(Vehicle());
            To(s, 21);
            Assert.DoesNotContain(s.Records, r => r.Subject == "officeVan" && r.Kind is RecordKind.ExecutionConfusion or RecordKind.Shortfall);
            return string.Join("|", s.View.Debrief().Gaps.Select(g => g.SiteId + g.CapabilityId));
        }
        Assert.Equal(Output(false), Output(true));
    }
}
