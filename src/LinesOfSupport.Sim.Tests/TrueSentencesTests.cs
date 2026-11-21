using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>
/// DESK-78: shortfalls, the strip and quotes say the true thing (docs/reviews/2026-11-17-gameplay-review.md section 7).
/// Each on the review log that showed it.
/// </summary>
public class TrueSentencesTests
{
    private static InputLog Log(string name)
        => InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-17-review", name + "-input-log.json")) with { SavedDay = null };

    private static Session SpokeB(int throughDay) => Divided.Replay(Repo.Load("spoke-b"), Log("spoke-b"), throughDay);

    [Fact]
    public void Spoke_C_food_on_day_15_compares_supply_with_the_headcount_before_any_held_delivery()
    {
        var why = SpokeB(15).View.WhyShort("spokeCSite", "food")!;
        Assert.StartsWith("Coastal Provisions supplies 480 meals/day, and Spoke C needs 672 meals/day for its 224 people", why);
        Assert.DoesNotContain("waiting", why);
    }

    [Fact]
    public void The_strip_counts_the_hours_the_days_plan_counts()
    {
        // Spoke B day 3 in the review play: the staff meeting and work; the strip read 0 of 16. With the team (DESK-12)
        // the work SpokeB() leaves is yours, so your bar counts what your line of the day's plan counts.
        var s = SpokeB(2);
        var plan = s.View.DayPlan().People.Single(p => p.MemberId == "you");
        var strip = s.View.Team.Single(m => m.Id == "you");
        Assert.True(plan.HoursPlanned > 0);
        Assert.Equal(plan.HoursPlanned, strip.HoursSpokenForToday);
        Assert.Equal(strip.HoursPerDay - strip.HoursSpokenForToday, strip.FreeHoursToday);
    }

    [Fact]
    public void The_turnover_counts_the_shop_as_the_team_page_does()
        => Assert.Contains("You have 8 hours a day; Avery has 8, Novak has 8.",
            Session.Create(Repo.Load("spoke-b"), 1, []).View.Turnover().Text(TurnoverSectionKind.Shop));

    [Fact]
    public void Peak_demand_says_when_and_whose_headcount()
    {
        var s = SpokeB(2);
        Assert.Equal("Demand peaks at 468 kW on day 20, when 260 people are at Spoke B; on day 7 it is 324 kW, for 180.",
            s.View.RequirementEditor("req0007")!.PeakSaid);
        var preview = s.View.Preview(new AwardAction
        {
            Id = "q", Instrument = "gpc", Vendor = "vendorA", Site = "spokeBSite", Capability = "power", Quantity = 360,
            StartDay = 7, DurationDays = 30, Day = s.Day,
        });
        Assert.Contains(preview.SoftGaps, g => g.StartsWith("360 kW against peak demand of 468 kW at Spoke B on day 20, when 260 people are there;", StringComparison.Ordinal));
    }

    [Fact]
    public void A_start_range_says_what_decides_it()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, ["gpcAccounts", "cashAndPayingAgent", "systemAccess"]);
        var preview = s.View.Preview(new AwardAction
        {
            Id = "w", Instrument = "gpc", Vendor = "islandMart", Card = "tutWater", Quantity = 4500, DurationDays = 7, Day = 1,
        });
        Assert.True(preview.EarliestStartMax > preview.EarliestStartMin);
        Assert.Contains(preview.SoftGaps, g => g.StartsWith($"Earliest start is day {preview.EarliestStartMin} to {preview.EarliestStartMax}: the vendor can take up to", StringComparison.Ordinal));
    }

    [Fact]
    public void On_top_of_names_nothing_when_the_days_do_not_overlap()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, ["gpcAccounts", "cashAndPayingAgent", "systemAccess"]);
        Assert.True(s.Apply(new AwardAction
        {
            Id = "w1", Instrument = "gpc", Vendor = "islandMart", Card = "tutWater", Quantity = 4500, StartDay = 2, DurationDays = 3, Day = 1,
        }).Accepted);
        // A second buy that starts after the first one ends is not on top of it.
        var later = s.View.Preview(new AwardAction
        {
            Id = "w2", Instrument = "gpc", Vendor = "islandMart", Card = "tutWater", Quantity = 4500, StartDay = 6, DurationDays = 3, Day = 1,
        });
        Assert.DoesNotContain(later.SoftGaps, g => g.Contains("on top of", StringComparison.Ordinal));
        var same = s.View.Preview(new AwardAction
        {
            Id = "w3", Instrument = "gpc", Vendor = "islandMart", Card = "tutWater", Quantity = 4500, StartDay = 2, DurationDays = 3, Day = 1,
        });
        Assert.Contains(same.SoftGaps, g => g.Contains("on top of it", StringComparison.Ordinal));
    }

    [Fact]
    public void A_site_not_yet_open_carries_its_opening_day_and_the_heading_counts_open_requests()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1, []);
        var board = s.View.MorningBoard();
        var closed = board.Tiles.Where(t => t.Status == "notOpen").ToList();
        Assert.NotEmpty(closed);
        Assert.All(closed, t => Assert.NotNull(t.OpensDay));
        Assert.All(board.Tiles.Where(t => t.Status != "notOpen"), t => Assert.Null(t.OpensDay));
        Assert.Equal(2, board.OpenRequests);
    }

    [Fact]
    public void A_COR_reads_the_same_on_the_order_form_and_on_payments()
    {
        var bundle = Repo.Load("spoke-b-90");
        var log = InputLog.Load(Path.Combine(Repo.Scenario("spoke-b-90"), "golden", "approach1-as-written.json"));
        var award = log.Actions.OfType<AwardAction>().Single(a => a.Id == "gens");
        // The order form says the odds only when the COR misses some; a COR who catches everything has no warning there.
        var form = Session.Create(bundle, log.Seed, log.PrepChoices, log.Team).View.Preview(award).SoftGaps
                          .SingleOrDefault(g => g.Contains("% of problems", StringComparison.Ordinal));
        var formPercent = form is null ? "100" : System.Text.RegularExpressions.Regex.Match(form, @"about (\d+)% of problems").Groups[1].Value;

        var s = Replay.Run(bundle, log, stopAfterDay: 12);
        var a = s.Arrangements.Single(x => x.Id == "gens");
        var payments = s.View.CorSummary(s.View.Cors.Single(c => c.Id == a.CorId), a);
        Assert.Contains($"about {formPercent}% of problems", payments);
    }

    [Fact]
    public void Numbers_take_the_right_words()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, ["gpcAccounts", "cashAndPayingAgent", "systemAccess"]);
        var preview = s.View.Preview(new AwardAction
        {
            Id = "w", Instrument = "sf44", Vendor = "harborBottling", Card = "tutWater", Quantity = 4500, StartDay = 2, DurationDays = 8, Day = 1,
        });
        Assert.Contains(preview.SoftGaps, g => g.StartsWith("No written terms on an 8-day service", StringComparison.Ordinal));
        // Claude play 21 Nov, fifth play, note 21: the quote said "execution-confusion events", a report column's name.
        var noTerms = Assert.Single(preview.SoftGaps, g => g.StartsWith("No written terms", StringComparison.Ordinal));
        Assert.Equal("No written terms on an 8-day service: on a day the vendor and the unit disagree on how much or when, nothing on paper settles it and only half of that day's supply arrives.", noTerms);
        Assert.DoesNotContain("confusion", noTerms, StringComparison.Ordinal);
    }

    [Fact]
    public void What_a_vendors_rep_said_about_where_they_ship_from_reaches_their_card_and_the_quote()
    {
        // Coastal Provisions is on Third Island and ships its meals from Main Island; the review player met Ms. Carvalho on
        // day 1 and never saw that again.
        var s = SpokeB(1);
        var food = s.View.VendorCards().Single(v => v.Id == "vendorG").Sells.Single(o => o.CapabilityId == "food");
        Assert.Equal("Main Island", food.ShipsFrom);
        var preview = s.View.Preview(new AwardAction
        {
            Id = "f", Instrument = "gpc", Vendor = "vendorG", Site = "spokeCSite", Capability = "food", Quantity = 100,
            StartDay = 12, DurationDays = 5, Day = s.Day,
        });
        Assert.Contains(preview.SoftGaps, g => g.StartsWith("Coastal Provisions stocks this on Main Island (they said so when you met): deliveries depend on the transport links to", StringComparison.Ordinal));
    }

    [Fact]
    public void A_buy_that_starts_after_its_need_ended_says_so_in_the_plan()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, ["gpcAccounts", "cashAndPayingAgent", "systemAccess"]);
        Assert.True(s.Apply(new AwardAction
        {
            Id = "late", Instrument = "purchaseOrder", Vendor = "harborBottling", Card = "tutWater", Quantity = 4500, StartDay = 10,
            DurationDays = 7, FundingLine = "om1", Day = 1,
        }).Accepted);
        const string Late = "It starts on day 10, after water for Hub is no longer needed (its last day is day 8).";
        // Waiting for finance on day 1, then in work.
        Assert.Contains(s.View.DayPlan().People, p => p.Sentence.Contains(Late, StringComparison.Ordinal));
        s.AdvanceDay();
        var work = s.Work.Single(w => w.CompletedDay is null && w.Kind == WorkKind.Award);
        var item = s.View.DayPlan().People.SelectMany(p => p.Work).SingleOrDefault(w => w.WorkId == work.Id);
        Assert.True(item is null ? s.View.DayPlan().People.Any(p => p.Sentence.Contains(Late, StringComparison.Ordinal)) : item.Sentence.EndsWith(Late, StringComparison.Ordinal));
    }
}
