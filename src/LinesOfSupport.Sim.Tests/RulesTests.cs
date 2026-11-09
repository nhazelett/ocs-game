using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

/// <summary>Hard stops and mechanisms from SPEC 4.1, one test per rule.</summary>
public class RulesTests
{
    private static Session NewSpokeB(params string[] prep) => Session.Create(Repo.Load("spoke-b"), 1, prep);

    private static void AdvanceTo(Session s, int day) { while (s.Day < day) s.AdvanceDay(); }

    [Fact]
    public void Cash_buy_over_the_micro_purchase_threshold_is_stopped()
    {
        var s = NewSpokeB("cashAndPayingAgent");
        var r = s.Apply(new AwardAction { Id = "x", Instrument = "sf44", Vendor = "vendorE", Site = "spokeBSite", Capability = "fuel", Quantity = 1000, DurationDays = 30 });
        Assert.False(r.Accepted);
        Assert.Contains(r.HardStops, h => h.Contains("micro-purchase threshold"));
    }

    [Fact]
    public void Cash_buy_needs_a_paying_agent()
    {
        var s = NewSpokeB();   // paying agent arrives day 5 without prep
        var r = s.Apply(new AwardAction { Id = "x", Instrument = "sf44", Vendor = "vendorE", Site = "spokeBSite", Capability = "fuel", Quantity = 100, DurationDays = 5 });
        Assert.False(r.Accepted);
        Assert.Contains(r.HardStops, h => h.Contains("paying agent"));
    }

    [Fact]
    public void Card_buy_is_stopped_where_the_vendor_does_not_take_the_card()
    {
        var s = NewSpokeB("gpcAccounts");
        var r = s.Apply(new AwardAction { Id = "x", Instrument = "gpc", Vendor = "vendorD", Site = "spokeBSite", Capability = "fuel", Quantity = 100, DurationDays = 5 });
        Assert.False(r.Accepted);
        Assert.Contains(r.HardStops, h => h.Contains("does not accept the card"));
    }

    [Fact]
    public void BPA_call_outside_scope_is_stopped()
    {
        var s = NewSpokeB();
        // Harbor Equipment does not offer operators, and the BPA's scope does not include them either.
        var r = s.Apply(new AwardAction { Id = "x", Instrument = "bpaCall", Vendor = "vendorC", Vehicle = "bosBpa", Site = "spokeBSite", Capability = "power", Lines = ["generation", "operator"], Quantity = 300, DurationDays = 30, FundingLine = "om1" });
        Assert.False(r.Accepted);
        Assert.Contains(r.HardStops, h => h.Contains("outside bosBpa's scope"));
    }

    [Fact]
    public void Vendor_capacity_is_shared_across_awards()
    {
        var s = NewSpokeB();
        Assert.True(s.Apply(new AwardAction { Id = "a", Instrument = "bpaCall", Vendor = "vendorC", Vehicle = "bosBpa", Site = "spokeBSite", Capability = "power", Lines = ["generation"], Quantity = 500, DurationDays = 10, FundingLine = "om1" }).Accepted);
        var r = s.Apply(new AwardAction { Id = "b", Instrument = "bpaCall", Vendor = "vendorC", Vehicle = "bosBpa", Site = "hubSite", Capability = "power", Lines = ["generation"], Quantity = 200, DurationDays = 10, FundingLine = "om1" });
        Assert.False(r.Accepted);
        Assert.Contains(r.HardStops, h => h.Contains("already committed"));
    }

    [Fact]
    public void A_rejected_action_changes_nothing_but_the_record()
    {
        var s = NewSpokeB();
        var before = s.Arrangements.Count;
        s.Apply(new AwardAction { Id = "x", Instrument = "sf44", Vendor = "vendorE", Site = "spokeBSite", Capability = "fuel", Quantity = 1000, DurationDays = 30 });
        Assert.Equal(before, s.Arrangements.Count);
        Assert.Empty(s.Work);
        Assert.Equal(RecordKind.Rejected, s.Records[^1].Kind);
    }

    [Fact]
    public void UCA_cannot_be_definitized_before_the_qualifying_proposal_and_obligations_respect_the_cap()
    {
        var s = NewSpokeB();
        Assert.True(s.Apply(new AwardAction { Id = "u", Instrument = "uca", Vendor = "vendorB", Site = "spokeBSite", Capability = "power", Lines = ["generation", "fuelSupply", "operator", "maintenance"], Quantity = 470, DurationDays = 60, FundingLine = "om1" }).Accepted);
        AdvanceTo(s, 18);
        var uca = s.Arrangements.Single(a => a.Id == "u");
        Assert.NotNull(uca.Uca);
        if (!uca.Uca!.ProposalReceived)
            Assert.False(s.Apply(new DefinitizeAction { Arrangement = "u", FundingLine = "om1" }).Accepted);
        Assert.True(uca.ObligatedCents <= uca.Uca.NteCents / 2);
        Assert.True(uca.IncurredCents > 0, "mobilization should be incurred on the start day");
    }

    [Fact]
    public void Unpaid_UCA_costs_stop_work_after_the_grace_days()
    {
        // A tiny funding line runs dry, so the UCA cannot obligate what it incurs.
        var bundle = Repo.Load("spoke-b");
        var s = Session.Create(bundle, 1);
        Assert.True(s.Apply(new AwardAction { Id = "u", Instrument = "uca", Vendor = "vendorB", Site = "spokeBSite", Capability = "power", Lines = ["generation", "fuelSupply", "operator", "maintenance"], Quantity = 470, DurationDays = 60, FundingLine = "om1" }).Accepted);
        // Drain the line with a large written purchase so little is left for the UCA's mobilization.
        Assert.True(s.Apply(new AwardAction { Id = "drain", Instrument = "purchaseOrder", Vendor = "vendorE", Site = "hubSite", Capability = "fuel", Quantity = 3000, DurationDays = 49, FundingLine = "om1" }).Accepted);
        AdvanceTo(s, 31);
        Assert.Contains(s.Records, r => r.Kind == RecordKind.UcaStopWork && r.Subject == "u");
    }

    [Fact]
    public void Coordination_is_withdrawn_and_fuel_from_another_island_waits_for_the_barge()
    {
        var s = NewSpokeB();
        AdvanceTo(s, 3);
        Assert.True(s.Apply(new CoordinateAction { Id = "c", Site = "spokeBSite", Capability = "fuel", Quantity = 900, FromSite = "hubSite" }).Accepted);
        AdvanceTo(s, 31);
        Assert.Contains(s.Records, r => r.Kind == RecordKind.Withdrawal && r.Subject == "c");
        var fuel = s.Capabilities.Single(c => c.SiteId == "spokeBSite" && c.CapabilityId == "fuel");
        Assert.True(fuel.DownDays > 0, "the storm and the withdrawal should run Spoke B out of fuel");
    }

    [Fact]
    public void Missing_fuel_takes_power_down_through_the_dependency()
    {
        var s = NewSpokeB();
        s.Apply(new AwardAction { Id = "g", Instrument = "bpaCall", Vendor = "vendorC", Vehicle = "bosBpa", Card = "req0007", Quantity = 360, StartDay = 7, DurationDays = 60, FundingLine = "om1" });
        AdvanceTo(s, 11);
        var power = s.Capabilities.Single(c => c.SiteId == "spokeBSite" && c.CapabilityId == "power");
        Assert.Equal(0, power.TodayPermille);
        var shortfall = s.Records.Last(r => r.Kind == RecordKind.Shortfall && r.Subject == "spokeBSite.power");
        Assert.Contains("fuel at 0%", shortfall.Text);
        Assert.NotEmpty(shortfall.CausedBy);
    }

    [Fact]
    public void Prep_budget_is_enforced()
    {
        var bundle = Repo.Load("spoke-b");
        var all = bundle.Rules.PrepChoices.Select(c => c.Id).ToArray();
        Assert.Throws<InvalidDataException>(() => Session.Create(bundle, 1, all));
    }

    [Fact]
    public void Stub_scenario_loads_and_runs_to_the_end()
    {
        var s = Session.Create(Repo.Load("_stub"), 1);
        while (!s.Finished) s.AdvanceDay();
        Assert.Equal(30, s.Metrics["day"]);
    }

    [Fact]
    public void Termination_deobligates_the_unperformed_portion_back_to_a_current_line()
    {
        var s = NewSpokeB();
        Assert.True(s.Apply(new AwardAction { Id = "f", Instrument = "purchaseOrder", Vendor = "vendorE", Site = "hubSite", Capability = "fuel", Quantity = 1000, DurationDays = 40, FundingLine = "om1" }).Accepted);
        AdvanceTo(s, 10);
        var line = s.Funding.Single(f => f.Id == "om1");
        var before = line.ObligatedCents;
        Assert.True(s.Apply(new TerminateAction { Arrangement = "f" }).Accepted);
        AdvanceTo(s, 12);
        Assert.True(line.ObligatedCents < before, "unperformed days should return to the line");
        var f = s.Arrangements.Single(a => a.Id == "f");
        Assert.Equal(f.ObligatedCents, f.DailyCostCents * (f.EndDay - f.StartDay + 1));
    }

    [Fact]
    public void Definitization_obligates_the_full_price_while_the_money_is_current()
    {
        var s = NewSpokeB();
        Assert.True(s.Apply(new AwardAction { Id = "u", Instrument = "uca", Vendor = "vendorB", Site = "spokeBSite", Capability = "power", Lines = ["generation", "fuelSupply", "operator", "maintenance"], Quantity = 470, DurationDays = 60, FundingLine = "om1" }).Accepted);
        while (!s.Arrangements.Any(a => a.Id == "u" && a.Uca!.ProposalReceived)) s.AdvanceDay();
        Assert.True(s.Apply(new DefinitizeAction { Arrangement = "u", FundingLine = "om1" }).Accepted);
        while (!s.Arrangements.Single(a => a.Id == "u").Uca!.Definitized) s.AdvanceDay();
        var u = s.Arrangements.Single(a => a.Id == "u");
        Assert.Equal(u.Uca!.QuotedTotalCents, u.ObligatedCents);
    }

    [Fact]
    public void Vendor_capacity_counts_only_overlapping_periods_and_incumbents_do_not_remobilize()
    {
        var s = NewSpokeB();
        Assert.True(s.Apply(new AwardAction { Id = "a", Instrument = "bpaCall", Vendor = "vendorC", Vehicle = "bosBpa", Site = "spokeBSite", Capability = "power", Lines = ["generation"], Quantity = 500, StartDay = 7, DurationDays = 10, FundingLine = "om1" }).Accepted);
        // Same vendor, 500 more, but starting after "a" ends: no overlap, so capacity allows it.
        Assert.True(s.Apply(new AwardAction { Id = "b", Instrument = "bpaCall", Vendor = "vendorC", Vehicle = "bosBpa", Site = "spokeBSite", Capability = "power", Lines = ["generation"], Quantity = 500, StartDay = 20, DurationDays = 10, FundingLine = "om1" }).Accepted);
        var t = NewSpokeB();
        Assert.True(t.Apply(new AwardAction { Id = "u", Instrument = "uca", Vendor = "vendorB", Site = "spokeBSite", Capability = "power", Lines = ["generation", "fuelSupply", "operator", "maintenance"], Quantity = 300, DurationDays = 20, FundingLine = "om1" }).Accepted);
        while (!t.Arrangements.Any(a => a.Id == "u" && a.Status == ArrangementStatus.Active)) t.AdvanceDay();
        Assert.True(t.Apply(new AwardAction { Id = "n", Instrument = "purchaseOrder", Vendor = "vendorB", Site = "spokeBSite", Capability = "power", Lines = ["generation", "fuelSupply", "operator", "maintenance"], Quantity = 300, StartDay = t.Arrangements.Single(a => a.Id == "u").EndDay + 1, DurationDays = 10, FundingLine = "om1" }).Accepted);
        Assert.Contains(t.Records, r => r.Subject == "n" && r.Text.Contains("quoted $45,000"));
    }
}
