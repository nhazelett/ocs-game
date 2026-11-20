using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-11 (D6, D41): a vendor who ships port to port. A boat between islands is bought on the office's own
/// initiative for the site it serves; while it runs, goods bought on another island reach that site every day, not only on
/// the days the barge or ferry runs, and even where no link runs at all. Weather that closes the link stops it too.</summary>
public class LiftTests
{
    private static readonly string[] Prep = ["gpcAccounts", "cashAndPayingAgent", "systemAccess"];

    private static AwardAction Boat(string site, int start, int days, string id = "boat") => new()
    {
        Id = id, Instrument = "purchaseOrder", Vendor = "harborLighterage", Site = site, Capability = "portLift",
        Quantity = 1, StartDay = start, DurationDays = days, FundingLine = "om1",
    };

    // Food for Spoke C from Coastal Provisions, stocked on Main Island: it crosses on the ferry, two days a week.
    private static AwardAction Food(long qty) => new()
    {
        Id = "food", Instrument = "purchaseOrder", Vendor = "vendorG", Card = "req0014", Quantity = qty, StartDay = 12,
        DurationDays = 19, FundingLine = "om1",
    };

    private static void To(Session s, int day)
    {
        while (s.Day < day && !s.Finished) s.AdvanceDay();
    }

    private static Session Run(bool boat, int seed = 1)
    {
        var s = Session.Create(Repo.Load("spoke-b"), seed, Prep);
        if (boat) Assert.True(s.Apply(Boat("spokeCSite", 10, 21)).Accepted);
        To(s, 3);
        var qty = s.View.RequirementEditor("req0014")!.PeakDemandUnits;
        var r = s.Apply(Food(qty));
        Assert.True(r.Accepted, string.Join("; ", r.HardStops));
        return s;
    }

    /// <summary>Each played day from the food's start: what waited for the ferry, and how (noService or closed).</summary>
    private static List<(int Day, string Reason)> Waits(Session s)
    {
        var waits = new List<(int, string)>();
        while (!s.Finished)
        {
            s.AdvanceDay();
            var map = s.View.MapDay();
            foreach (var h in map.Held.Where(h => h.ArrangementId == "food")) waits.Add((map.Day, h.Reason));
        }
        return waits;
    }

    [Fact]
    public void A_boat_is_on_the_own_initiative_list_for_each_site_off_the_shop_island()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1, Prep);
        var boats = s.View.OwnInitiative.Where(o => o.CapabilityId == "portLift").ToList();
        Assert.Equal(["spokeBSite", "spokeCSite"], boats.Select(b => b.SiteId));
        Assert.All(boats, b => Assert.Equal(["harborLighterage"], b.Vendors));
        Assert.All(boats, b => Assert.False(b.Have));
        Assert.Equal("Nobody will ask for it. Goods bought on another island reach Third Island only on the 2 days a week the barge runs. "
                     + "While you have one, they cross every day, except when weather stops the barge.", boats[1].Why);
        // The tutorial has one island and no boat to sell: its list is the vehicle alone.
        var t = Session.Create(Repo.Load("tutorial"), 1, Prep);
        Assert.DoesNotContain(t.View.OwnInitiative, o => o.CapabilityId == "portLift");
    }

    [Fact]
    public void The_lift_vendor_card_says_its_boat_goes_anywhere()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1, Prep);
        Assert.Null(s.View.VendorCards().Single(c => c.Id == "harborLighterage").IslandsReached);   // not met yet
        Assert.True(s.Apply(new MeetVendorAction { Vendor = "harborLighterage" }).Accepted);
        while (s.View.VendorCards().Single(c => c.Id == "harborLighterage").MetDay is null && !s.Finished) s.AdvanceDay();
        var card = s.View.VendorCards().Single(c => c.Id == "harborLighterage");
        Assert.Equal(s.Scenario.Islands.Select(i => i.Id), card.IslandsReached);
        Assert.Contains(s.Records, r => r.Text.Contains("Their boat sails from Main Island to any island, every day the weather allows.", StringComparison.Ordinal));
        Assert.Contains(card.Sells, o => o.CapabilityId == "portLift");
    }

    [Fact]
    public void Without_a_boat_food_waits_for_the_ferry_and_with_one_it_crosses_every_day_the_weather_allows()
    {
        var without = Waits(Run(boat: false));
        Assert.Contains(without, w => w.Reason == "noService");   // off the ferry's days

        var s = Run(boat: true);
        var all = Waits(s);
        var sails = s.View.Arrangements.Single(a => a.Id == "boat").StartDay;
        var with = all.Where(w => w.Day >= sails).ToList();
        // Nothing waits for a scheduled run once the boat sails; only a weather closure of the ferry holds it.
        Assert.DoesNotContain(with, w => w.Reason == "noService");
        Assert.All(with, w => Assert.Equal("closed", w.Reason));
        Assert.True(with.Count < without.Count(w => w.Day >= sails));
    }

    [Fact]
    public void The_boat_shows_on_the_map_as_a_barge_run_from_the_supplier_island()
    {
        var s = Run(boat: true);
        var runs = new List<Views.MapRun>();
        while (!s.Finished)
        {
            s.AdvanceDay();
            runs.AddRange(s.View.MapDay().Runs.Where(r => r.ArrangementId == "food" && r.LinkId == "lift:boat"));
        }
        Assert.NotEmpty(runs);
        Assert.All(runs, r => Assert.Equal(("hub", "spokeC", "barge"), (r.FromIslandId, r.ToIslandId, r.Mode)));
    }

    [Fact]
    public void A_boat_to_one_island_carries_nothing_to_another()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1, Prep);
        Assert.True(s.Apply(Boat("spokeBSite", 10, 21)).Accepted);
        To(s, 3);
        Assert.True(s.Apply(Food(s.View.RequirementEditor("req0014")!.PeakDemandUnits)).Accepted);
        Assert.Contains(Waits(s), w => w.Reason == "noService");
    }

    [Fact]
    public void A_boat_is_not_a_shop_vehicle_and_is_not_scored()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1, Prep);
        Assert.True(s.Apply(Boat("spokeBSite", 10, 21)).Accepted);
        To(s, 20);
        Assert.False(s.View.HasShopVehicle);
        Assert.True(s.View.OwnInitiative.Single(o => o.SiteId == "spokeBSite" && o.CapabilityId == "portLift").Have);
        To(s, s.LastDay + 1);
        Assert.DoesNotContain(s.Records, r => r.Subject == "boat" && r.Kind is RecordKind.ExecutionConfusion or RecordKind.Shortfall or RecordKind.VendorFailure);
        Assert.DoesNotContain(s.View.Debrief().Gaps, g => g.CapabilityId == "portLift");
    }

    [Fact]
    public void Why_short_points_at_the_boat_while_goods_wait_for_the_next_run()
    {
        var s = Run(boat: false);
        string? why = null;
        while (!s.Finished && why is null)
        {
            s.AdvanceDay();
            var held = s.View.MapDay().Held.FirstOrDefault(h => h.ArrangementId == "food" && h.Reason == "noService");
            if (held is null) continue;
            var w = s.View.WhyShort("spokeCSite", "food");
            if (w is not null && w.Contains("next run", StringComparison.Ordinal)) why = w;
        }
        Assert.NotNull(why);
        Assert.Contains("for the next run to Third Island; a boat of your own to Third Island, bought on your own initiative, would carry it every day", why);
    }
}
