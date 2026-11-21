using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

/// <summary>
/// DESK-82 (D33; Codex's Q26 for DESK-71): one sentence above Arrange support sums the buy up, and the Sim says when it
/// needs attention, so the Game never reads warning prose to choose a colour.
/// </summary>
public class AwardSummaryTests
{
    private static readonly string[] Prep = ["gpcAccounts", "cashAndPayingAgent", "systemAccess"];

    private static Session Tutorial() => Session.Create(Repo.Load("tutorial"), 1, Prep);

    private static AwardAction Water(string id, int start, int days, long quantity = 4500, string instrument = "sf44") => new()
    {
        Id = id, Instrument = instrument, Vendor = "harborBottling", Card = "tutWater", Quantity = quantity, StartDay = start,
        DurationDays = days, FundingLine = "om1", Day = 1,
    };

    [Fact]
    public void The_sentence_gives_cost_hours_and_whose_start_against_wanted_and_what_it_is_on_top_of()
    {
        var s = Tutorial();
        var buy = Water("w", 2, 7);
        var preview = s.View.Preview(buy);
        var summary = s.View.AwardSummary(buy)!;
        Assert.StartsWith($"Harbor Bottling, water for Hub: {(preview.TotalCents / 100m).ToString("$#,##0", System.Globalization.CultureInfo.InvariantCulture)} and ", summary.Sentence);
        // An SF 44 written on day 1 starts on day 3 or 4 at the earliest; the stock covers day 2.
        Assert.Contains("and no shop hours. It starts on day 3 or 4; it was wanted from day 2. The water stock on hand should cover day 2.", summary.Sentence);
        // The host nation's water point opens on day 9, inside these days, so the buy is on top of it; days 2 to 8 are not
        // covered, so it is not only adding to covered days.
        Assert.Contains("It is on top of Main Island Utilities (host nation).", summary.Sentence);
        Assert.False(summary.Attention);
        Assert.Null(summary.Why);
    }

    [Fact]
    public void A_late_start_says_what_covers_the_days_between()
    {
        var s = Tutorial();
        var summary = s.View.AwardSummary(Water("w", 5, 4))!;
        Assert.Contains("It starts on day 5; it was wanted from day 2.", summary.Sentence);
        Assert.Contains("The water stock on hand should cover day 2. Nothing covers days 3 and 4.", summary.Sentence);
        Assert.False(summary.Attention);
    }

    [Fact]
    public void A_buy_that_starts_after_the_need_ends_needs_attention()
    {
        var s = Tutorial();
        var summary = s.View.AwardSummary(Water("w", 10, 3))!;
        Assert.True(summary.Attention);
        Assert.Equal("It starts after the need ends on day 8.", summary.Why);
        Assert.EndsWith(summary.Why!, summary.Sentence);
    }

    [Fact]
    public void A_second_buy_for_days_already_covered_needs_attention()
    {
        var s = Tutorial();
        Assert.True(s.Apply(Water("first", 2, 7)).Accepted);
        var again = Water("second", 2, 7, 500);
        Assert.True(s.View.Preview(again).Accepted);
        var summary = s.View.AwardSummary(again)!;
        Assert.True(summary.Attention);
        Assert.Equal("Every day it would run is already covered.", summary.Why);
        Assert.Contains("It is on top of Harbor Bottling and Main Island Utilities (host nation).", summary.Sentence);
    }

    [Fact]
    public void A_buy_that_tops_up_a_short_day_does_not()
    {
        var s = Tutorial();
        Assert.True(s.Apply(Water("first", 2, 7, 1000)).Accepted);
        var summary = s.View.AwardSummary(Water("second", 2, 7, 3500))!;
        Assert.False(summary.Attention);
        Assert.Contains("It is on top of", summary.Sentence);
    }

    [Fact]
    public void A_buy_still_being_written_counts_and_is_named_as_work_in_hand()
    {
        var s = Tutorial();
        Assert.True(s.Apply(Water("po", 6, 3, instrument: "purchaseOrder") with { Assignee = "avery" }).Accepted);
        var summary = s.View.AwardSummary(Water("again", 6, 3, 500))!;
        Assert.Contains("being written with Harbor Bottling", summary.Sentence);
        Assert.True(summary.Attention);
        Assert.Equal("Every day it would run is already covered.", summary.Why);
    }

    [Fact]
    public void A_refused_buy_has_no_summary()
    {
        var s = Tutorial();
        Assert.Null(s.View.AwardSummary(Water("w", 2, 7, 6000)));
    }

    [Fact]
    public void Hours_name_whose_time()
    {
        var s = Tutorial();
        var buy = Water("w", 6, 3, instrument: "purchaseOrder") with { Assignee = "avery" };
        var preview = s.View.Preview(buy);
        Assert.True(preview.Accepted, string.Join(" ", preview.HardStops));
        Assert.True(preview.ShopHoursMax > 0);
        // Q28 (DESK-142): Avery's own hours to the half hour, as the picker says them ("6.5 to 13 hours").
        static string H(long milli) => ((milli + 499) / 500 / 2.0).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
        var (lo, hi) = (H(preview.HoursMinMilli), H(preview.HoursMaxMilli));
        var hours = lo == hi ? $"{lo} hours" : $"{lo} to {hi} hours";
        Assert.Contains($"and {hours} of MSgt Joan Avery's time.", s.View.AwardSummary(buy)!.Sentence);
        Assert.Contains(".5", hours);
        Assert.Contains("and no shop hours.", s.View.AwardSummary(Water("x", 2, 7))!.Sentence);
    }

    [Fact]
    public void The_summary_changes_nothing()
    {
        var s = Tutorial();
        var before = s.View.MorningBoard();
        _ = s.View.AwardSummary(Water("w", 2, 7));
        Assert.Equal(before.Items.Count, s.View.MorningBoard().Items.Count);
        Assert.Equal(1, s.Day);
    }

    // ------------------------------------------------------------------ DESK-86 (Claude play 17 Nov, notes 9, 24, 25)

    private static InputLog TodaysPlay()
        => InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-17", "input-log.json"));

    /// <summary>The morning of day 5 of the 17 Nov play, with the operators SMSgt Dunn named added to the forklift request.</summary>
    private static Session OperatorsAdded()
    {
        var log = TodaysPlay();
        var s = Replay.Run(Repo.Load("tutorial"), log, stopAfterDay: 4);
        foreach (var add in log.Actions.Where(a => a.Day == 5 && a is AddLineAction)) Assert.True(s.Apply(add).Accepted);
        return s;
    }

    [Fact]
    public void A_card_buy_names_the_card_surcharge_in_its_price()
    {
        var s = Tutorial();
        var buy = Water("w", 2, 7, instrument: "gpc") with { Vendor = "islandMart", FundingLine = null };
        var summary = s.View.AwardSummary(buy)!;
        Assert.StartsWith("Island Mart, water for Hub: $9,734 with Island Mart's 3% card surcharge and no shop hours.", summary.Sentence);
        // Water has one part, so the summary does not list parts.
        Assert.DoesNotContain("It brings", summary.Sentence, StringComparison.Ordinal);
    }

    [Fact]
    public void A_written_buy_says_what_it_brings_and_why_it_starts_when_it_does()
    {
        // Day 5 of the 17 Nov play, after the operators were added: the purchase order with Coastal Rentals the player wrote.
        var s = OperatorsAdded();
        var buy = new AwardAction
        {
            Id = "po", Instrument = "purchaseOrder", Vendor = "coastalRentals", Card = "tutForklift", Quantity = 3, StartDay = 8,
            DurationDays = 13, FundingLine = "om1", Day = 5, Priorities = ["fast", "good"],
        };
        var preview = s.View.Preview(buy);
        Assert.True(preview.Accepted, string.Join(" ", preview.HardStops));
        var summary = s.View.AwardSummary(buy)!.Sentence;
        Assert.True(summary.Contains("It brings forklifts, operators, and maintenance.", StringComparison.Ordinal), summary);
        // D43 (DESK-129): the late end of the range says what it costs against the commander's allowance.
        Assert.Contains("It starts on day 8 or 9; it is wanted from day 8. If it starts on day 9, material handling equipment at Hub is out 1 day in all: all the commander allows. Finance certifies the funds on day 6.", summary);
    }

    [Fact]
    public void A_buy_that_leaves_a_part_out_says_which()
    {
        // The same request with the operators added, bought from a vendor whose offer lacks them, says what it leaves out.
        var s = OperatorsAdded();
        var tried = new[] { "harborEquipment", "coastalRentals" }
            .Select(v => s.View.AwardSummary(new AwardAction
            {
                Id = "c", Instrument = "gpc", Vendor = v, Card = "tutForklift", Quantity = 3, StartDay = 8, DurationDays = 13, Day = 5,
            }))
            .Where(x => x is not null).Select(x => x!.Sentence).ToList();
        Assert.NotEmpty(tried);
        Assert.All(tried, t => Assert.Matches("It brings [a-z ,]+(, not [a-z ,]+)?\\.", t));
    }
}
