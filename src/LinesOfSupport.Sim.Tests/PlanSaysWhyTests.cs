using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Policies;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>
/// DESK-60 (docs/design/desk-redesign.md D18 and D19): the plan and the quotes say why. From the 16 Nov Claude play
/// (docs/reviews/2026-11-16-claude-play.md), notes 5, 10, 19, 20, 22 and 24.
/// </summary>
public class PlanSaysWhyTests
{
    private static readonly string[] Prep = ["gpcAccounts", "cashAndPayingAgent", "systemAccess"];

    private static Session ForkliftOrderOnDay4()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        while (s.Day < 4) s.AdvanceDay();
        Assert.True(s.Apply(new AwardAction
        {
            Id = "lifts", Instrument = "purchaseOrder", Vendor = "coastalRentals", Card = "tutForklift", Quantity = 3,
            StartDay = 8, DurationDays = 13, FundingLine = "om1", Assignee = "avery",
        }).Accepted);
        return s;
    }

    [Fact]
    public void The_plan_says_why_a_job_waits()
    {
        var s = ForkliftOrderOnDay4();
        var avery = s.View.DayPlan().People.Single(p => p.MemberId == "avery");
        Assert.EndsWith("Written purchase order with Coastal Rentals for material handling equipment at Hub waits for finance to certify the funds on day 5.", avery.Sentence, StringComparison.Ordinal);
        Assert.DoesNotContain("not ready until a later day", avery.Sentence, StringComparison.Ordinal);
    }

    [Fact]
    public void A_buy_waiting_for_finance_counts_as_in_work()
    {
        var s = ForkliftOrderOnDay4();
        while (s.Day < 6) s.AdvanceDay();
        Assert.DoesNotContain(s.View.DayPlan().Open, o => o.StartsWith("Material handling equipment for Hub", StringComparison.Ordinal));
    }

    [Fact]
    public void A_late_start_says_the_stock_covers_the_days_between()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        Assert.True(s.Apply(new AwardAction
        {
            Id = "water", Instrument = "gpc", Vendor = "islandMart", Card = "tutWater", Quantity = 4500, StartDay = 3, DurationDays = 6,
        }).Accepted);
        var water = s.View.WhereThingsStand().Single(x => x.Title == "Water for Hub");
        Assert.Equal(RequestStanding.Arranged, water.State);
        Assert.Contains("It was wanted from day 2. The water stock on hand should cover day 2.", water.Sentence, StringComparison.Ordinal);
    }

    [Fact]
    public void A_late_start_with_nothing_between_says_so()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        Assert.True(s.Apply(new AwardAction
        {
            Id = "water", Instrument = "gpc", Vendor = "islandMart", Card = "tutWater", Quantity = 4500, StartDay = 6, DurationDays = 3,
        }).Accepted);
        var water = s.View.WhereThingsStand().Single(x => x.Title == "Water for Hub");
        Assert.Matches(@"should cover days? 2( and 3)?\. Nothing covers days? [3-5]", water.Sentence);
    }

    [Fact]
    public void A_price_under_a_dollar_reads_in_cents()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        Assert.True(s.Apply(new MeetVendorAction { Vendor = "harborBottling" }).Accepted);
        s.AdvanceDay();
        var met = s.Records.Last(r => r.Kind == RecordKind.Visit).Text;
        Assert.Contains("at $0.25 a gallon", met, StringComparison.Ordinal);
        Assert.DoesNotContain("$0 each", met, StringComparison.Ordinal);
    }

    [Fact]
    public void A_shortfall_count_matches_its_percent()
    {
        var greedy = Replay.Run(Repo.Load("tutorial"), 1, PolicyCatalog.Create("greedy"));
        Assert.Contains(greedy.Records, r => r.Text == "Material handling equipment at Hub degraded to 50%: supply 1.5 forklifts against demand 3.");
        Assert.DoesNotContain(greedy.Records, r => r.Text.Contains(" 1 forklifts", StringComparison.Ordinal));
    }

    [Fact]
    public void A_vendor_failure_says_there_is_nothing_for_you_to_do()
    {
        // The greedy tutorial: on the first day Coastal Rentals' forklifts are down, the vendor puts it right.
        var bundle = Repo.Load("tutorial");
        var played = Replay.Run(bundle, 1, PolicyCatalog.Create("greedy"));
        var day = played.Records.First(r => r.Kind == RecordKind.VendorFailure && r.Text.StartsWith("Coastal Rentals", StringComparison.Ordinal)).Day;
        var greedy = Replay.Run(bundle, played.ToSave() with { SavedDay = null }, stopAfterDay: day);
        var why = greedy.View.WhyShort("hubSite", "mhe");
        // DESK-78: the forklifts were bought without drivers, so the failure is the buy's to put right, not the vendor's.
        Assert.Equal("Coastal Rentals' equipment was down (no operators); it was bought without operators, so it will keep failing more often than it should: that is yours to put right, with a buy that brings it.", why);
    }

    [Fact]
    public void Mills_does_not_invite_questions_there_is_no_way_to_ask()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        Assert.DoesNotContain("ask me", s.View.Turnover()!.Greeting, StringComparison.OrdinalIgnoreCase);
    }
}
