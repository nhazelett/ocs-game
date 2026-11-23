using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-239 / D65: the turnover's payment advice agrees with the actual routes.</summary>
public class HarborPaymentWordsTests
{
    [Fact]
    public void Cash_and_written_orders_are_available_but_the_card_is_not()
    {
        var s = Session.Create(Repo.Load("tutorial"), 20261123,
            ["gpcAccounts", "cashAndPayingAgent", "systemAccess"]);
        var hash = s.StateHash();
        var buy = new AwardAction
        {
            Id = "water", Instrument = "purchaseOrder", Vendor = "harborBottling", Card = "tutWater",
            Quantity = 4500, StartDay = 2, DurationDays = 7, FundingLine = "om1", Priorities = ["fast", "good"],
        };
        Assert.Empty(s.View.Preview(buy).HardStops);
        Assert.Empty(s.View.Preview(buy with { Instrument = "sf44", Priorities = null }).HardStops);
        Assert.Contains(s.View.Preview(buy with { Instrument = "gpc", Priorities = null }).HardStops,
            stop => stop.Contains("Harbor Bottling does not accept the card", StringComparison.Ordinal));
        var turnover = s.View.Turnover();
        foreach (var kind in new[] { TurnoverSectionKind.Vendors, TurnoverSectionKind.Money })
        {
            var text = turnover.Sections.Single(section => section.Kind == kind).Text;
            Assert.Contains("Harbor Bottling", text);
            Assert.Contains("written purchase order", text);
            Assert.DoesNotContain("cash only", text);
            Assert.DoesNotContain("only takes cash", text);
        }
        Assert.Equal(hash, s.StateHash());
    }
}
