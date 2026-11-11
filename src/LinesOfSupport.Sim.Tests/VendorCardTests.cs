using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-05 (docs/design/desk-redesign.md D6): one card per vendor, blank where the office has not learned a fact.
/// Meeting fills in everything, doing business does too, a survey of their island fills in supply and reach.</summary>
public class VendorCardTests
{
    private static readonly string[] Prep = ["gpcAccounts", "cashAndPayingAgent", "systemAccess"];

    private static Views.VendorCard Card(Session s, string id) => s.View.VendorCards().Single(c => c.Id == id);

    [Fact]
    public void Host_nation_and_partner_suppliers_get_no_card()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1);
        var ids = s.View.VendorCards().Select(c => c.Id).ToList();
        Assert.DoesNotContain("hubUtilities", ids);
        Assert.DoesNotContain("partnerForce", ids);
        Assert.Contains("vendorD", ids);
        Assert.Equal(7, ids.Count);
    }

    [Fact]
    public void An_unmet_vendor_shows_what_it_sells_and_blanks_the_rest()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        var c = Card(s, "harborBottling");
        Assert.Equal("Harbor Bottling", c.Name);
        Assert.Equal("Main Island", c.IslandName);
        var water = Assert.Single(c.Sells);
        Assert.Equal("water", water.CapabilityId);
        Assert.Null(water.CapacityPerDay);
        Assert.Null(water.UnitPriceCents);
        Assert.Null(water.LeadTimeDays);
        Assert.Null(c.AcceptsCash);
        Assert.Null(c.AcceptsGpc);
        Assert.Null(c.IslandsReached);
        Assert.Null(c.KnownFrom);
        Assert.Equal("Not known yet: how they take payment, their prices, how much they can supply and where they deliver. Meet Mr. Marco Silva to find out.", c.StillToLearn);

        // The award editor carries the same blanks as flags; the Sim still decides on the true facts.
        var bottling = s.View.ActionEditor("hubSite", "water", "tutWater")!.Vendors.Single(v => v.Id == "harborBottling");
        Assert.False(bottling.PaymentKnown);
        Assert.False(bottling.CapacityKnown);
    }

    [Fact]
    public void Vendors_the_office_already_knows_are_filled_in_from_the_start()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        var mart = Card(s, "islandMart"); // scenario: observed
        Assert.False(mart.AcceptsCash);
        Assert.True(mart.AcceptsGpc);
        Assert.Equal(["hub"], mart.IslandsReached);
        Assert.Null(mart.StillToLearn);
        Assert.Equal("This office has seen their work before.", mart.KnownFrom);

        var camp = Card(s, "campServices"); // the office holds a BPA with them and buys billeting and dining under it
        Assert.Null(camp.StillToLearn);
        Assert.Equal(["campBpa"], camp.VehicleIds);
        Assert.Equal(["hubBilleting", "hubDining"], camp.ArrangementIds);
        Assert.Equal("This office has done business with them, so their terms are on file.", camp.KnownFrom);
    }

    [Fact]
    public void Meeting_a_vendor_fills_in_the_card_and_says_so()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        Assert.True(s.Apply(new MeetVendorAction { Vendor = "harborBottling" }).Accepted);
        Assert.True(Card(s, "harborBottling").MeetingSetUp);
        Assert.Equal("Not known yet: how they take payment, their prices, how much they can supply and where they deliver. A meeting with Mr. Marco Silva is set up.",
                     Card(s, "harborBottling").StillToLearn);
        for (var i = 0; i < 3 && s.View.VendorMetDay("harborBottling") is null; i++) s.AdvanceDay();

        var c = Card(s, "harborBottling");
        Assert.NotNull(c.MetDay);
        Assert.True(c.AcceptsCash);
        Assert.False(c.AcceptsGpc);
        Assert.Equal(["hub"], c.IslandsReached);
        Assert.NotNull(c.Sells[0].CapacityPerDay);
        Assert.NotNull(c.Sells[0].UnitPriceCents);
        Assert.Null(c.StillToLearn);
        Assert.Equal($"You met them on day {c.MetDay}.", c.KnownFrom);

        var met = s.Records.Last(r => r.Kind == RecordKind.Visit && r.Text.StartsWith("Met ", StringComparison.Ordinal));
        Assert.Contains("They take cash, not the card. They deliver on Main Island only.", met.Text);
        Assert.EndsWith("Their vendor card is filled in.", met.Text);
        Assert.DoesNotContain("directory", met.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void A_survey_of_their_island_fills_in_supply_and_reach_but_not_payment()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1);
        Assert.Null(Card(s, "vendorD").IslandsReached);
        Assert.True(s.Apply(new SurveyAction { Island = "spokeB" }).Accepted);
        for (var i = 0; i < 20 && !s.Records.Any(r => r.Kind == RecordKind.Knowledge && r.Subject == "survey spokeB"); i++) s.AdvanceDay();
        Assert.Contains(s.Records, r => r.Kind == RecordKind.Knowledge && r.Subject == "survey spokeB");

        var c = Card(s, "vendorD");
        Assert.NotNull(c.Sells[0].CapacityPerDay);
        Assert.Null(c.Sells[0].UnitPriceCents);
        Assert.Null(c.AcceptsCash);
        Assert.Equal(["hub", "spokeB"], c.IslandsReached);
        Assert.Equal("Your survey of Second Island showed what they can supply and where they reach.", c.KnownFrom);
        Assert.Equal("Not known yet: how they take payment and their prices. Meet Ms. Hana Sato to find out.", c.StillToLearn);
        var fuel = s.View.ActionEditor("spokeBSite", "fuel", null)!.Vendors.Single(v => v.Id == "vendorD");
        Assert.True(fuel.CapacityKnown);
        Assert.False(fuel.PaymentKnown);
    }

    [Fact]
    public void Every_vendor_the_office_can_buy_from_has_someone_to_meet()
    {
        foreach (var id in new[] { "tutorial", "spoke-b", "spoke-b-90" })
        {
            var s = Session.Create(Repo.Load(id), 1);
            foreach (var c in s.View.VendorCards()) Assert.True(c.Rep is not null, $"{id}: {c.Name} has no rep");
        }
    }

    [Fact]
    public void Card_text_never_shows_an_id()
    {
        foreach (var id in new[] { "tutorial", "spoke-b", "spoke-b-90", "sustainment" })
        {
            var s = Session.Create(Repo.Load(id), 1);
            foreach (var c in s.View.VendorCards())
                foreach (var text in new[] { c.KnownFrom, c.StillToLearn, c.Standing }.OfType<string>())
                    foreach (var vendor in s.Scenario.Vendors.Select(v => v.Id).Where(v => v.Any(char.IsUpper) || v.Any(char.IsDigit)))
                        Assert.DoesNotContain(vendor, text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Reading_the_cards_changes_nothing()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1);
        var hash = s.StateHash();
        _ = s.View.VendorCards();
        _ = s.View.ActionEditor("spokeBSite", "power", null);
        Assert.Equal(hash, s.StateHash());
    }
}
