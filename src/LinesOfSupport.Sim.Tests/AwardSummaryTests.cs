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
        var hours = preview.ShopHoursMin == preview.ShopHoursMax ? $"{preview.ShopHoursMin} hours" : $"{preview.ShopHoursMin} to {preview.ShopHoursMax} hours";
        Assert.Contains($"and {hours} of MSgt Joan Avery's time.", s.View.AwardSummary(buy)!.Sentence);
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
}
