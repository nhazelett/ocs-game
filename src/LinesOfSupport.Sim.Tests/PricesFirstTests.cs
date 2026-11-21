using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Policies;

namespace LinesOfSupport.Sim.Tests;

/// <summary>
/// DESK-134 (design doc section 20): a written purchase order with a vendor that must price the work first cannot be signed
/// before its priced proposal arrives, so the writing waits for it; a UCA lets the work start while it comes.
/// </summary>
public class PricesFirstTests
{
    private static AwardAction Power(string instrument, string vendor = "vendorB", string? assignee = null) => new()
    {
        Id = "p", Instrument = instrument, Vendor = vendor, Site = "spokeBSite", Capability = "power",
        Lines = vendor == "vendorB" ? ["generation", "fuelSupply", "operator", "maintenance"] : ["generation", "maintenance"],
        Quantity = 514, StartDay = 7, DurationDays = 60, FundingLine = "om1", Assignee = assignee,
    };

    [Fact]
    public void The_preview_says_a_purchase_order_waits_for_the_price_and_a_uca_does_not()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1);
        var po = s.View.Preview(Power("purchaseOrder"));
        var uca = s.View.Preview(Power("uca"));
        Assert.True(po.Accepted, string.Join("; ", po.HardStops));
        Assert.Contains("Meridian Site Services needs about 14 to 20 days to price this; a purchase order waits for that, a UCA lets them start now.", po.SoftGaps);
        Assert.Contains(po.SoftGaps, g => g.Contains("the priced proposal takes 14 to 20 days", StringComparison.Ordinal));
        Assert.DoesNotContain(uca.SoftGaps, g => g.Contains("to price this", StringComparison.Ordinal));
        // Day 1 plus the proposal, the writing and the vendor's lead: no earlier than day 16 plus its lead.
        Assert.True(po.EarliestStartMin >= 1 + 14 + 1 + 1, $"{po.EarliestStartMin}");
        Assert.True(uca.EarliestStartMax < po.EarliestStartMin, $"UCA {uca.EarliestStartMax}, PO {po.EarliestStartMin}");
        // D46 (DESK-156): the proposal's days are in the quote's notes, not the pinned summary.
        Assert.Contains("It is written once Meridian Site Services' priced proposal comes, between day 15 and day 21.", po.SoftGaps);
        Assert.DoesNotContain(po.SoftGaps, g => g.StartsWith("Finance certifies", StringComparison.Ordinal));
        Assert.DoesNotContain(uca.SoftGaps, g => g.Contains("priced proposal comes", StringComparison.Ordinal));
    }

    [Fact]
    public void A_vendor_that_does_not_price_first_is_unchanged()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1);
        var po = s.View.Preview(Power("purchaseOrder", "vendorC"));
        Assert.DoesNotContain(po.SoftGaps, g => g.Contains("price this", StringComparison.Ordinal) || g.Contains("priced proposal", StringComparison.Ordinal));
    }

    [Fact]
    public void The_order_is_written_when_the_proposal_comes_and_the_plan_says_so()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1);
        var applied = s.Apply(Power("purchaseOrder", assignee: "avery"));
        Assert.True(applied.Accepted, applied.Message);
        var rec = s.Records.Last(r => r.Subject == "p");
        var m = System.Text.RegularExpressions.Regex.Match(rec.Text, @"Meridian Site Services prices the work first: its priced proposal is expected on day (\d+), and the order is written when it arrives\.");
        Assert.True(m.Success, rec.Text);
        var due = int.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        Assert.InRange(due, 15, 21);
        var work = s.Work.Single(w => w.Kind == WorkKind.Award);
        Assert.Equal(due, work.WaitUntilDay);
        var avery = s.View.DayPlan().People.Single(p => p.MemberId == "avery");
        Assert.Contains($"waits for Meridian Site Services' priced proposal, due on day {due}.", avery.Sentence);

        while (s.Day < due) s.AdvanceDay();
        Assert.DoesNotContain(s.Arrangements, a => a.Id == "p");
        while (!s.Arrangements.Any(a => a.Id == "p") && !s.Finished) s.AdvanceDay();
        var arr = s.Arrangements.Single(a => a.Id == "p");
        Assert.True(arr.CreatedDay >= due, $"signed day {arr.CreatedDay}, proposal day {due}");
        Assert.Null(arr.Uca);
    }

    [Fact]
    public void Textbook_buys_spoke_b_power_on_a_uca_because_the_order_would_start_late()
    {
        var s = Replay.Run(Repo.Load("spoke-b"), 1, new TextbookPolicy());
        var power = s.Arrangements.Single(a => a.Id == "t-req0007-enduring");
        Assert.Equal("uca", power.Instrument);
        Assert.NotNull(power.Uca);
        Assert.True(power.Uca!.ProposalReceived);
    }
}
