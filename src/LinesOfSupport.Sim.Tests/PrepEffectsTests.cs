using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>
/// DESK-76 (docs/design/desk-redesign.md D32; gameplay review 17 Nov section 4): every preparation choice offered does
/// something, says what in one line, and the page says where the player is going. The three that only bring information
/// show what the Sim already knows, and change no replay.
/// </summary>
public class PrepEffectsTests
{
    [Fact]
    public void The_tutorial_offers_only_what_changes_something_and_nothing_idle_is_ticked()
    {
        var b = Repo.Load("tutorial");
        var v = PrepScreen.View(b);
        // Purchase cards already work on day 1 there; no warrant is set; no partner force or host nation agreement.
        Assert.Equal(["cashAndPayingAgent", "systemAccess", "pullVehicles", "vendorLists", "templatesAndKit"], v.Offered.Select(o => o.Id));
        // Every offered choice fits: the tutorial's preparation is not a trade-off (9 of 14 days).
        Assert.Equal(9, v.OfferedDays);
        Assert.Equal(["cashAndPayingAgent", "systemAccess"], v.Defaults);
        Assert.All(v.Offered, o => Assert.False(string.IsNullOrWhiteSpace(o.Says)));   // Maj Cole's voice, not form labels
        Assert.All(v.Offered, o => Assert.False(string.IsNullOrWhiteSpace(o.Changes)));
        Assert.Equal("Finance restores your cash 3 days after it runs low, not 5.", v.Options.Single(o => o.Id == "cashAndPayingAgent").Changes);
        Assert.Equal("The contract-writing system works from day 3, not day 25.", v.Options.Single(o => o.Id == "systemAccess").Changes);

        var cards = PrepScreen.Choice(b, "gpcAccounts");
        Assert.False(cards.ChangesHere);
        Assert.Null(cards.Changes);
        Assert.False(cards.Default);
        Assert.True(PrepScreen.Check(b, ["gpcAccounts"]).Accepted);   // old saves and logs still start
    }

    [Fact]
    public void The_page_says_where_the_player_is_going()
    {
        Assert.StartsWith("You're going to the hub on the main island: three hundred people", PrepScreen.View(Repo.Load("tutorial")).Where);
        Assert.Contains("Spoke B opens on day 7 with a hundred and eighty people", PrepScreen.View(Repo.Load("spoke-b")).Where);
        Assert.Null(PrepScreen.View(Repo.Load("sustainment")).Where);
    }

    [Fact]
    public void Spoke_b_still_cannot_take_everything()
    {
        var v = PrepScreen.View(Repo.Load("spoke-b"));
        Assert.True(v.OfferedDays > v.BudgetDays);
        Assert.Equal("Purchase cards work from day 1; without this there are none this deployment.", v.Options.Single(o => o.Id == "gpcAccounts").Changes);
        Assert.Equal("Each request says what the partner force can spare for it before you ask.", v.Options.Single(o => o.Id == "partnerAgreements").Changes);
    }

    [Fact]
    public void The_vendor_list_fills_in_the_listed_vendors_cards()
    {
        var b = Repo.Load("tutorial");
        VendorCard Bottling(Session s) => s.View.VendorCards().Single(c => c.Id == "harborBottling");
        var without = Bottling(Session.Create(b, 1, ["systemAccess"]));
        Assert.Null(without.AcceptsCash);
        var with = Bottling(Session.Create(b, 1, ["systemAccess", "vendorLists"]));
        Assert.True(with.AcceptsCash);
        Assert.NotNull(with.Sells.Single().UnitPriceCents);
        Assert.Equal("They are on the vendor list you brought from reach-back, with their terms.", with.KnownFrom);
        Assert.Null(with.StillToLearn);
        Assert.Contains("vendor cards for Harbor Bottling, Harbor Equipment Co, Coastal Rentals and Main Island Sanitation start filled in",
            PrepScreen.View(b).Options.Single(o => o.Id == "vendorLists").Changes);
    }

    [Fact]
    public void Pulled_agreements_show_on_the_requests_they_could_serve()
    {
        var b = Repo.Load("tutorial");
        RequestStanding Forklifts(string[] prep)
        {
            var s = Session.Create(b, 1, prep);
            while (s.Day < 4) s.AdvanceDay();
            return s.View.WhereThingsStand().Single(r => r.CardId == "tutForklift");
        }
        Assert.Empty(Forklifts(["systemAccess"]).OnFile);
        Assert.Equal(["Harbor Equipment Co has a BPA this office can call against for material handling equipment, up to $25,000 a call, open until day 120."],
            Forklifts(["systemAccess", "pullVehicles"]).OnFile);
    }

    [Fact]
    public void Confirmed_partner_agreements_say_what_the_partner_can_spare()
    {
        var b = Repo.Load("spoke-b");
        string[] Power(string[] prep) => [.. Session.Create(b, 1, prep).View.WhereThingsStand().Single(r => r.CardId == "req0007").OnFile];
        Assert.Empty(Power(["systemAccess"]));
        Assert.Contains("Under the ACSA, the partner force can spare up to 4,000 gal/day of fuel; the J4 asks for it.", Power(["systemAccess", "partnerAgreements"]));
    }
}
