using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-94 (Fable's review of PR 308): ending a card buy is not a contract termination. A card buy is cancelled with
/// the vendor in a short call (rules cardCancelShopHours): no notice, no settlement, and the days not delivered come back off
/// the card. Contracts and calls keep the shop day a termination takes.</summary>
public class CardCancelTests
{
    private static readonly string[] Prep = ["gpcAccounts", "cashAndPayingAgent", "systemAccess"];

    private static Session TutorialWithCardWater()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        Assert.True(s.Apply(new AwardAction
        {
            Id = "water", Instrument = "gpc", Vendor = "islandMart", Card = "tutWater", Quantity = 4500, StartDay = 2,
            DurationDays = 7, FundingLine = "om1",
        }).Accepted);
        return s;
    }

    [Fact]
    public void A_card_buy_cancels_in_about_an_hour_and_reads_as_a_cancel_not_a_termination()
    {
        var s = TutorialWithCardWater();
        var end = new TerminateAction { Arrangement = "water" };
        var p = s.View.PreviewAny(end);
        Assert.True(p.Accepted, string.Join("; ", p.HardStops));
        Assert.Equal(1, p.ShopHoursMin);
        Assert.Equal(1, p.ShopHoursMax);
        var words = s.View.EndWords("water")!;
        Assert.Equal("Cancel this card buy", words.Button);
        Assert.Equal("A short call to Island Mart cancels it before its first delivery, and the whole charge comes back off the card.", words.Says);
        Assert.Null(s.View.EndWords("hubGrid"));   // host nation support is not the office's to end (DESK-01)

        Assert.True(s.Apply(end).Accepted);
        Assert.Equal("Cancel the card buy for Island Mart water at Hub", s.Work.Last().Label);
        while (s.Work.Last().CompletedDay is null) s.AdvanceDay();
        var water = s.Arrangements.Single(a => a.Id == "water");
        Assert.Equal(ArrangementStatus.Ended, water.Status);
        Assert.Equal(0, water.ObligatedCents);
        Assert.Contains(s.Records, r => r.Text.StartsWith("Card buy for Island Mart water at Hub cancelled with the vendor before its first delivery.")
                                        && r.Text.Contains("credited back to the card"));
        Assert.DoesNotContain(s.Records, r => r.Subject == "water" && r.Text.Contains("terminated"));
        Assert.Empty(s.Claims);
        Assert.StartsWith("Day 1: Cancel this card buy: Island Mart water at Hub", s.View.Decisions().First().Sentence);
    }

    [Fact]
    public void A_card_buy_already_delivering_stops_at_the_end_of_the_day_and_keeps_only_what_it_delivered()
    {
        var s = TutorialWithCardWater();
        while (s.Day < 4) s.AdvanceDay();
        var water = s.Arrangements.Single(a => a.Id == "water");
        Assert.Equal(2, water.StartDay);
        Assert.StartsWith("A short call to Island Mart stops it at the end of the day the call is made", s.View.EndWords("water")!.Says);
        var paid = water.ObligatedCents;
        Assert.True(s.Apply(new TerminateAction { Arrangement = "water" }).Accepted);
        while (s.Work.Last().CompletedDay is null) s.AdvanceDay();
        Assert.Equal(4, water.EndDay);
        var delivered = water.DailyCostCents * 3 + water.MobilizationCents;   // days 2, 3 and 4
        Assert.Equal(delivered, water.ObligatedCents);
        Assert.Contains(s.Records, r => r.Text == $"Card buy for Island Mart water at Hub cancelled with the vendor; it supplies through day 4. {((paid - delivered) / 100m).ToString("$#,##0", System.Globalization.CultureInfo.InvariantCulture)} credited back to the card.");
        Assert.Empty(s.Claims);
    }

    [Fact]
    public void A_contract_still_takes_a_shop_day_to_terminate()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1);
        Assert.True(s.Apply(new AwardAction { Id = "f", Instrument = "purchaseOrder", Vendor = "vendorE", Site = "hubSite", Capability = "fuel", Quantity = 1000, DurationDays = 40, FundingLine = "om1" }).Accepted);
        while (s.Day < 10) s.AdvanceDay();
        var words = s.View.EndWords("f")!;
        Assert.Equal("End this contract", words.Button);
        var p = s.View.PreviewAny(new TerminateAction { Arrangement = "f" });
        Assert.True(p.ShopHoursMin >= 8, $"{p.ShopHoursMin} hours");
        Assert.True(s.Apply(new TerminateAction { Arrangement = "f" }).Accepted);
        Assert.StartsWith("Terminate ", s.Work.Last().Label);
    }
}
