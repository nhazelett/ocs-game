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
        // D46 (DESK-156): two sentences here, and the cover before it starts and what it is on top of are in the notes.
        // DESK-183: each delivery of a multi-day SF 44 is a trip for the buyer and the paying agent.
        Assert.Contains("and 2 shop hours of your time on each delivery day. It starts on day 3 or 4; it was wanted from day 2. If it starts on day 4,", summary.Sentence);
        Assert.Contains(preview.SoftGaps, g => g.EndsWith("The water stock on hand should cover day 2.", StringComparison.Ordinal));
        // The host nation's water point opens on day 9, inside these days, so the buy is on top of it; days 2 to 8 are not
        // covered, so it is not only adding to covered days.
        Assert.Contains(preview.SoftGaps, g => g.StartsWith("Main Island Utilities (host nation) is already set to supply", StringComparison.Ordinal));
        Assert.False(summary.Attention);
        Assert.Null(summary.Why);
    }

    [Fact]
    public void A_late_start_says_what_covers_the_days_between()
    {
        var s = Tutorial();
        var summary = s.View.AwardSummary(Water("w", 5, 4))!;
        Assert.Contains("It starts on day 5; it was wanted from day 2.", summary.Sentence);
        Assert.Contains(s.View.Preview(Water("w", 5, 4)).SoftGaps, g => g.EndsWith("The water stock on hand should cover day 2. Nothing covers days 3 and 4.", StringComparison.Ordinal));
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
        Assert.EndsWith("from day 2. Every day it would run is already covered.", summary.Sentence);
        Assert.Contains(s.View.Preview(again).SoftGaps, g => g.StartsWith("Harbor Bottling is already set to supply", StringComparison.Ordinal));
    }

    [Fact]
    public void A_buy_that_tops_up_a_short_day_does_not()
    {
        var s = Tutorial();
        Assert.True(s.Apply(Water("first", 2, 7, 1000)).Accepted);
        var summary = s.View.AwardSummary(Water("second", 2, 7, 3500))!;
        Assert.False(summary.Attention);
        Assert.Contains(s.View.Preview(Water("second", 2, 7, 3500)).SoftGaps, g => g.EndsWith("this would be on top of it.", StringComparison.Ordinal));
    }

    [Fact]
    public void A_buy_still_being_written_counts_and_is_named_as_work_in_hand()
    {
        var s = Tutorial();
        Assert.True(s.Apply(Water("po", 6, 3, instrument: "purchaseOrder") with { Assignee = "avery" }).Accepted);
        var summary = s.View.AwardSummary(Water("again", 6, 3, 500))!;
        Assert.Contains(s.View.Preview(Water("again", 6, 3, 500)).SoftGaps, g => g.Contains("already writing the written purchase order with Harbor Bottling", StringComparison.Ordinal));
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
        // A one-day SF 44 is paid on the spot; one that runs longer costs a trip each delivery day (DESK-183).
        Assert.Contains("and no shop hours.", s.View.AwardSummary(Water("x", 2, 1))!.Sentence);
        Assert.Contains("shop hours of your time on each delivery day.", s.View.AwardSummary(Water("x", 2, 7))!.Sentence);
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
        // D43 (DESK-129): the late end of the range says what it costs against the commander's allowance; D46 (DESK-156):
        // that is the third sentence, and what it brings and finance's day are in the notes.
        Assert.EndsWith("It starts on day 8 or 9, after finance certifies the funds on day 6 and the buy is written; it is wanted from day 8. If it starts on day 9, material handling equipment at Hub is out 1 day in all: all the commander allows.", summary);
        Assert.Contains("Coastal Rentals brings forklifts, operators, and maintenance.", preview.SoftGaps);
        Assert.DoesNotContain(preview.SoftGaps, g => g.Contains("certifies", StringComparison.Ordinal));   // D47: in the start, once
    }

    [Fact]
    public void A_buy_that_leaves_a_part_out_says_which()
    {
        // The same request with the operators added, bought from a vendor whose offer lacks them, says what it leaves out.
        var s = OperatorsAdded();
        // D46 (DESK-156): in the quote's notes now, not the pinned summary.
        var tried = new[] { "harborEquipment", "coastalRentals" }
            .Select(v => new AwardAction
            {
                Id = "c", Instrument = "gpc", Vendor = v, Card = "tutForklift", Quantity = 3, StartDay = 8, DurationDays = 13, Day = 5,
            })
            .Where(a => s.View.AwardSummary(a) is not null).Select(a => s.View.Preview(a).SoftGaps).ToList();
        Assert.NotEmpty(tried);
        Assert.All(tried, gaps => Assert.Contains(gaps, g => System.Text.RegularExpressions.Regex.IsMatch(g, "^[A-Z][A-Za-z ]+ brings [a-z ,]+( without [a-z ,]+;.*)?\\.")));
    }

    // ------------------------------------------------------------------ D46 (DESK-156, Claude play 21 Nov, third play, note 22)

    [Fact]
    public void The_pinned_summary_is_at_most_three_sentences_and_the_rest_is_in_the_notes_once()
    {
        // The third play's day 7 waste purchase order, cheap and good, with Ruiz asked: it ran to six lines and pushed pick
        // two out of reach.
        var log = InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-21-3", "input-log.json"));
        var s = Replay.Run(Repo.Load("tutorial"), log, stopAfterDay: 6);
        Assert.True(s.Apply(log.Actions.Single(a => a.Day == 7 && a is AskAboutAction)).Accepted);
        var order = (AwardAction)log.Actions.Single(a => a.Day == 7 && a is AwardAction) with { Priorities = ["cheap", "good"] };
        var summary = s.View.AwardSummary(order)!;
        var sentences = System.Text.RegularExpressions.Regex.Matches(summary.Sentence, @"[.!?](\s|$)").Count;
        Assert.InRange(sentences, 2, 3);
        Assert.StartsWith("Main Island Sanitation, waste removal for Hub: ", summary.Sentence);
        Assert.NotNull(summary.AnswerDue);   // the field keeps its value whether or not it is said
        var gaps = s.View.Preview(order).SoftGaps;
        Assert.Equal(gaps.Count, gaps.Distinct().Count());
        var third = new[] { summary.Why, summary.PassLineCost, summary.AnswerDue }.OfType<string>().ToList();
        Assert.EndsWith(third[0], summary.Sentence);
        foreach (var rest in third.Skip(1))
        {
            Assert.DoesNotContain(rest, summary.Sentence);
            Assert.Single(gaps, g => g == rest);
        }
        // D47 (DESK-158): what the start waits for is in the start sentence, not the notes.
        Assert.Contains(", after finance certifies the funds on day 8 and the buy is written; it is wanted from day 13.", summary.Sentence);
        Assert.DoesNotContain(gaps, g => g.Contains("certifies", StringComparison.Ordinal));
    }
}
