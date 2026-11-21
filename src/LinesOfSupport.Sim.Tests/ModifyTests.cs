using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

/// <summary>D37 (DESK-97; FOR-NICK #20 (a)): a running contract takes the part it left out. Forklifts rented without
/// drivers in the tutorial, changed on day 9, stop failing for want of operators from day 10.</summary>
public class ModifyTests
{
    private static readonly string[] Prep = ["gpcAccounts", "cashAndPayingAgent", "systemAccess"];

    /// <summary>Coastal Rentals forklifts bought on day 4 as the request first read (no drivers), then the request
    /// learns it needs operators (SMSgt Dunn) and the line goes on it.</summary>
    private static Session ForkliftsWithoutDrivers(long seed)
    {
        var s = Session.Create(Repo.Load("tutorial"), seed, Prep);
        while (s.Day < 4) s.AdvanceDay();
        var award = s.Apply(new AwardAction
        {
            Id = "fork", Instrument = "purchaseOrder", Vendor = "coastalRentals", Card = "tutForklift", Quantity = 3, StartDay = 8,
            DurationDays = 13, FundingLine = "om1",
        });
        Assert.True(award.Accepted, award.Message);
        Assert.True(s.Apply(new AskAboutAction { Card = "tutForklift", Person = "dunn" }).Accepted);
        while (s.Day < 8) s.AdvanceDay();
        var added = s.Apply(new AddLineAction { Card = "tutForklift", Line = "operator" });
        Assert.True(added.Accepted, added.Message);
        Assert.DoesNotContain("operator", s.Arrangements.Single(a => a.Id == "fork").Lines);
        return s;
    }

    private static IEnumerable<CauseRecord> OperatorFailures(Session s, string id)
        => s.Records.Where(r => r.Kind == RecordKind.VendorFailure && r.Subject == id && r.Text.Contains("no operators"));

    [Fact]
    public void Forklifts_changed_on_day_9_stop_failing_for_want_of_drivers_from_day_10_over_200_seeds()
    {
        var before = 0;
        for (var seed = 1; seed <= 200; seed++)
        {
            var s = ForkliftsWithoutDrivers(seed);
            while (s.Day < 9) s.AdvanceDay();
            var change = s.Apply(new ModifyAction { Arrangement = "fork" });
            Assert.True(change.Accepted, change.Message);
            while (!s.View.Finished) s.AdvanceDay();
            Assert.DoesNotContain(OperatorFailures(s, "fork"), r => r.Day >= 10);
            before += OperatorFailures(s, "fork").Count();   // days 8 and 9 keep their reason as recorded
        }
        Assert.True(before > 0, "Some seeds fail on days 8 or 9 for want of drivers, and keep that record.");

        // Without the change the same forklifts keep failing for want of drivers after day 9.
        var after = 0;
        for (var seed = 1; seed <= 40; seed++)
        {
            var s = ForkliftsWithoutDrivers(seed);
            while (!s.View.Finished) s.AdvanceDay();
            after += OperatorFailures(s, "fork").Count(r => r.Day >= 10);
        }
        Assert.True(after > 0);
    }

    [Fact]
    public void The_change_says_its_hours_and_price_obligates_them_when_signed_and_reads_back_in_the_decisions()
    {
        var s = ForkliftsWithoutDrivers(1);
        while (s.Day < 9) s.AdvanceDay();
        var fork = s.Arrangements.Single(a => a.Id == "fork");
        var v = s.View.ModifyOptions("fork")!;
        Assert.True(v.Can, v.Why);
        Assert.Equal(["operator"], v.Parts);
        Assert.Equal("Add operators to this contract", v.Button);
        var daily = fork.DailyCostCents / 4;   // rules modificationPartPricePermille 250
        Assert.Equal(daily, v.DailyCents);
        Assert.Equal(daily * 11, v.PriceCents);   // days 10 to 20
        Assert.Equal(4, v.ShopHours);
        Assert.Equal($"4 hours to write the change; {Money(daily * 11)} for the days left, obligated when it is signed. Operators from day 10.", v.Says);
        Assert.Empty(v.Added);

        var obligated = fork.ObligatedCents;
        var line = s.Funding.Single(f => f.Id == "om1").ObligatedCents;
        Assert.True(s.Apply(new ModifyAction { Arrangement = "fork" }).Accepted);
        Assert.False(s.View.ModifyOptions("fork")!.Can);   // one change at a time
        while (s.Work.Last().CompletedDay is null) s.AdvanceDay();
        Assert.Equal(obligated + daily * 11, fork.ObligatedCents);
        Assert.Equal(line + daily * 11, s.Funding.Single(f => f.Id == "om1").ObligatedCents);
        Assert.Contains(s.Records, r => r.Text == $"Coastal Rentals material handling equipment at Hub changed: Coastal Rentals brings operators from day 10; {Money(daily * 11)} obligated from the O&M line.");
        var after = s.View.ModifyOptions("fork")!;
        Assert.False(after.Can);
        Assert.Equal(["Operators added from day 10."], after.Added);
        var d = s.View.Decisions().First();
        Assert.StartsWith("Day 9: Add operators to Coastal Rentals material handling equipment at Hub", d.Sentence);
        Assert.Equal("Signed on day 9: operators from day 10.", d.Now);

        // The parts are paid for as they are delivered.
        var incurred = fork.IncurredCents;
        s.AdvanceDay();
        Assert.Equal(incurred + fork.DailyCostCents + daily, fork.IncurredCents);
    }

    [Fact]
    public void A_part_the_request_does_not_ask_for_a_card_buy_and_a_vendor_without_it_are_refused_with_a_reason()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        while (s.Day < 4) s.AdvanceDay();
        Assert.True(s.Apply(new AwardAction
        {
            Id = "fork", Instrument = "purchaseOrder", Vendor = "coastalRentals", Card = "tutForklift", Quantity = 3, StartDay = 8,
            DurationDays = 13, FundingLine = "om1",
        }).Accepted);
        var call = s.Apply(new AwardAction
        {
            Id = "call", Instrument = "purchaseOrder", Vendor = "harborEquipment", Card = "tutForklift", Quantity = 1,
            StartDay = 8, DurationDays = 13, FundingLine = "om1",
        });
        Assert.True(call.Accepted, call.Message);
        Assert.True(s.Apply(new AwardAction
        {
            Id = "water", Instrument = "gpc", Vendor = "islandMart", Card = "tutWater", Quantity = 4500, DurationDays = 7,
        }).Accepted);
        while (s.Day < 8) s.AdvanceDay();

        var notAsked = s.View.ModifyOptions("fork")!;
        Assert.False(notAsked.Can);
        Assert.Equal("The request does not ask for operators. Find out what it needs first.", notAsked.Why);
        Assert.Equal("A card buy cannot be changed. Buy what it left out on its own, or cancel it and buy again.", s.View.ModifyOptions("water")!.Why);
        Assert.EndsWith("is not this office's contract, so it is not this office's to change.", s.View.ModifyOptions("hubForklift")!.Why);

        Assert.True(s.Apply(new AskAboutAction { Card = "tutForklift", Person = "dunn" }).Accepted);
        while (!s.Apply(new AddLineAction { Card = "tutForklift", Line = "operator" }).Accepted) s.AdvanceDay();   // once Dunn has answered
        var r = s.Apply(new ModifyAction { Arrangement = "call" });
        Assert.False(r.Accepted);
        Assert.NotEqual(ArrangementStatus.Declined, s.Arrangements.Single(a => a.Id == "call").Status);   // a written order from a vendor without drivers
        Assert.Equal("Harbor Equipment Co does not offer operators. Buy that from someone who does.", r.Message);
        Assert.False(s.Apply(new ModifyAction { Arrangement = "water" }).Accepted);
    }

    private static string Money(long cents) => (cents / 100m).ToString("$#,##0", System.Globalization.CultureInfo.InvariantCulture);
}
