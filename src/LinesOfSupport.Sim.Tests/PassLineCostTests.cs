using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

/// <summary>D43 (DESK-129; Claude play 20 Nov, third play, note 1): the commander's allowance reads as a total, and a buy
/// whose quoted start can fall after the day the request wants it says what that costs against it.</summary>
public class PassLineCostTests
{
    private static readonly string[] Prep = ["gpcAccounts", "cashAndPayingAgent", "systemAccess"];

    private static AwardAction Forklifts(string instrument, string? pick = null) => new()
    {
        Id = "fork", Instrument = instrument, Vendor = "coastalRentals", Card = "tutForklift", Quantity = 3, StartDay = 8,
        DurationDays = 9, FundingLine = instrument == "gpc" ? null : "om1", Priorities = pick?.Split('+').ToList(),
    };

    [Fact]
    public void A_purchase_order_that_may_start_a_day_late_says_it_uses_the_whole_allowance_and_a_card_buy_says_nothing()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        // Dunn asked on day 4 and his operators added on day 5, so D48 (DESK-172) has no part at risk to name.
        while (s.Day < 4) s.AdvanceDay();
        Assert.True(s.Apply(new AskAboutAction { Card = "tutForklift", Person = "dunn" }).Accepted);
        s.AdvanceDay();
        Assert.True(s.Apply(new AddLineAction { Card = "tutForklift", Line = "operator" }).Accepted);
        const string cost = "If it starts on day 9, material handling equipment at Hub is out 1 day in all: all the commander allows.";
        var po = s.View.AwardSummary(Forklifts("purchaseOrder", "fast+good"))!;
        Assert.Equal(cost, po.PassLineCost);
        Assert.Contains("It starts on day 8 or 9, after finance certifies the funds on day 6 and the buy is written; it is wanted from day 8. " + cost, po.Sentence);
        Assert.False(po.Attention);
        var card = s.View.AwardSummary(Forklifts("gpc"))!;
        Assert.Null(card.PassLineCost);
        Assert.DoesNotContain("commander allows", card.Sentence);
    }

    [Fact]
    public void The_bar_counts_down_days_against_the_allowance_and_the_commander_says_it_is_a_total()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        var bar = s.View.Bar();
        Assert.Contains("Water is out no more than one day in all while you're here. Forklifts the same: no more than one day in all.", bar.Spoken);
        Assert.Contains(bar.Lines, l => l.Text == "Material handling equipment at Hub: out 0 of the 1 day allowed so far.");
        Assert.Contains(bar.Lines, l => l.Text == "Waste removal at Hub: out 0 days so far; none allowed.");
    }
}
