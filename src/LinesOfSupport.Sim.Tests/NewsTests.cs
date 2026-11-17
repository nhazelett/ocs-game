using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>
/// DESK-73 (docs/design/desk-redesign.md D29): what people told you and the day's news reach you. Checked on the three
/// plays of the 17 Nov gameplay review (docs/reviews/2026-11-17-gameplay-review.md section 3), each at a day where the
/// player paid for an answer or news arrived and no screen they read showed it.
/// </summary>
public class NewsTests
{
    private static Session Play(string log, string scenario, int throughDay)
        => Replay.Run(Repo.Load(scenario),
            InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-17-review", log + "-input-log.json")),
            stopAfterDay: throughDay);

    private static Session SpokeB(int throughDay) => Play("spoke-b", "spoke-b", throughDay);

    /// <summary>The hasty play without the vendor list it brought (DESK-76: the list fills in the listed vendors' cards,
    /// so the fastest route the shop knows changes). Information only, so the replay is otherwise the same.</summary>
    private static Session HastyWithoutVendorList(int throughDay)
    {
        var log = InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-17-review", "tutorial-hasty-input-log.json"));
        return Replay.Run(Repo.Load("tutorial"), log with { PrepChoices = [.. log.PrepChoices.Where(p => p != "vendorLists")] }, stopAfterDay: throughDay);
    }

    /// <summary>The day's results page and the next morning both carry it.</summary>
    private static void Heard(Session s, NewKind kind, string words)
    {
        var summary = s.View.DaySummary()!;
        Assert.Contains(kind == NewKind.Answer ? summary.Said : summary.News, n => n.Text.Contains(words, StringComparison.Ordinal));
        Assert.Contains(s.View.MorningBoard().New, n => n.Kind == kind && n.Text.Contains(words, StringComparison.Ordinal));
    }

    [Fact]
    public void Spoke_B_day_1_the_J4s_answer_reaches_the_player_with_who_asked()
    {
        var s = SpokeB(1);
        Heard(s, NewKind.Answer, "The J4's ACSA manager is adding Spoke B to the fuel order");
        var j4 = s.View.DaySummary()!.Said.Single(n => n.Text.Contains("J4", StringComparison.Ordinal));
        Assert.Equal("you", j4.AskedBy);
        Assert.Contains("can cut it back when its own needs rise", j4.Text);
    }

    [Fact]
    public void Spoke_B_days_4_and_6_both_SCO_answers_reach_the_player_in_full()
    {
        var s = SpokeB(4);
        Heard(s, NewKind.Answer, "The micro-purchase threshold for this operation is $40,000");
        Assert.Equal("cole", s.View.DaySummary()!.Said.Single(n => n.Text.Contains("thresholds", StringComparison.Ordinal)).Person!.Id);
        Heard(SpokeB(6), NewKind.Answer, "We don't know of a contract vehicle that covers your area.");
    }

    [Fact]
    public void Spoke_B_days_12_and_14_the_people_arriving_are_news()
    {
        Heard(SpokeB(12), NewKind.News, "Spoke C: 64 more personnel arrive day 15");
        Heard(SpokeB(14), NewKind.News, "Second increment of 80 personnel lands day 20");
    }

    [Fact]
    public void Spoke_B_day_16_both_weather_closures_are_news()
    {
        var s = SpokeB(16);
        Heard(s, NewKind.News, "Weather stops the barge.");
        Heard(s, NewKind.News, "Weather closes the Third Island crossing.");
    }

    [Fact]
    public void The_contract_writing_system_coming_up_is_said_on_the_morning_it_is_up()
    {
        var s = SpokeB(11);
        Assert.Equal(12, s.SystemOnlineDay);
        Heard(s, NewKind.News, "The contract-writing system is up from day 12");
        Assert.DoesNotContain(SpokeB(12).View.MorningBoard().New, n => n.Text.Contains("contract-writing system", StringComparison.Ordinal));
    }

    [Fact]
    public void Tutorial_morning_5_Dunns_answer_and_the_vendor_running_short_are_on_the_morning()
    {
        var s = Play("tutorial-careful", "tutorial", 4);
        Assert.Equal(5, s.Day);
        Heard(s, NewKind.Answer, "I've got nobody licensed on forklifts this rotation.");
        Heard(s, NewKind.News, "Harbor Equipment Co is short of forklifts this week.");
        var dunn = s.View.MorningBoard().New.Single(n => n.Kind == NewKind.Answer);
        Assert.Equal("dunn", dunn.Person!.Id);
        Assert.Equal("you", dunn.AskedBy);
        // The answer comes before the news.
        Assert.Equal([NewKind.Answer, NewKind.News], s.View.MorningBoard().New.Select(n => n.Kind));
    }

    [Fact]
    public void Tutorial_morning_7_the_new_request_leads_the_list()
    {
        var s = Play("tutorial-careful", "tutorial", 6);
        var first = s.View.MorningBoard().New[0];
        Assert.Equal(NewKind.Request, first.Kind);
        Assert.Equal(BoardLinkKind.Card, first.LinkKind);
        Assert.Equal("tutWaste", first.LinkId);
        Assert.Equal("Capt Ana Ruiz asks for waste removal for Hub, wanted from day 13 for 8 days.", first.Text);
    }

    [Fact]
    public void A_staff_meeting_with_nothing_for_contracting_is_not_news()
    {
        // The careful player's day 5 had only the meeting, which was ops talk.
        var s = Play("tutorial-careful", "tutorial", 5);
        Assert.Contains(s.Records, r => r.Day == 5 && r.Kind == RecordKind.Visit && r.Text.EndsWith("Nothing for contracting.", StringComparison.Ordinal));
        Assert.Empty(s.View.MorningBoard().New);
        Assert.Empty(s.View.DaySummary()!.Said);
        // The first meeting, which gives the commander's priorities, is.
        Assert.Contains(Play("tutorial-careful", "tutorial", 1).View.MorningBoard().New,
            n => n.Text.Contains("If we run short, Hub comes first.", StringComparison.Ordinal));
    }

    [Fact]
    public void News_that_caused_other_news_the_same_night_is_told_once()
    {
        var s = Play("tutorial-careful", "tutorial", 3);
        var news = s.View.MorningBoard().New.Where(n => n.Kind == NewKind.News).Select(n => n.Text).ToList();
        Assert.Contains(news, t => t.StartsWith("The host nation withdrew inherited host nation support for material handling equipment at Hub", StringComparison.Ordinal));
        Assert.DoesNotContain(news, t => t.StartsWith("The host nation takes its forklift back.", StringComparison.Ordinal));
    }

    [Fact]
    public void An_open_request_gives_its_last_day_to_order_and_the_route()
    {
        // The hasty player: forklifts wanted day 8; the shop knows a cash buy from Harbor Equipment on two days' notice.
        var s = HastyWithoutVendorList(3);
        var forklift = s.View.WhereThingsStand().Single(r => r.CardId == "tutForklift");
        var ob = forklift.OrderBy!;
        Assert.Equal(6, ob.Day);
        Assert.Equal("Order by day 6: a cash buy from Harbor Equipment Co starts 2 days after it goes in.", ob.Sentence);

        // Placed on that day by that route, it starts on the day wanted.
        var later = HastyWithoutVendorList(5);
        var today = later.View.WhereThingsStand().Single(r => r.CardId == "tutForklift").OrderBy!;
        Assert.Equal("Order today to start on day 8: a cash buy from Harbor Equipment Co starts 2 days after it goes in.", today.Sentence);
        var preview = later.View.Preview(new AwardAction
        {
            Id = "forecastCheck", Instrument = today.InstrumentId, Vendor = today.VendorId, Card = "tutForklift", Quantity = 1,
            DurationDays = 13, Day = later.Day,
        });
        Assert.True(preview.Accepted, string.Join(" ", preview.HardStops));
        Assert.Equal(8, preview.EarliestStartMin);

        // A day later no route makes day 8, and it says the soonest start instead.
        var late = HastyWithoutVendorList(6).View.WhereThingsStand().Single(r => r.CardId == "tutForklift").OrderBy!;
        Assert.True(late.Day < 7);
        Assert.Equal("Too late for day 8 by any route the shop knows: a cash buy from Harbor Equipment Co starts 2 days after it goes in, so the soonest start is day 9.", late.Sentence);
    }

    [Fact]
    public void The_vendor_list_widens_the_routes_the_shop_knows()
    {
        // The hasty player brought the vendor list: Coastal Rentals' card is filled in from day 1, and its card buy is faster.
        var ob = Play("tutorial-hasty", "tutorial", 3).View.WhereThingsStand().Single(r => r.CardId == "tutForklift").OrderBy!;
        Assert.Equal("Order by day 7: a card buy from Coastal Rentals starts 1 day after it goes in.", ob.Sentence);
    }

    [Fact]
    public void A_request_with_something_bought_or_in_work_has_no_order_by()
    {
        var s = Play("tutorial-careful", "tutorial", 12);
        Assert.All(s.View.WhereThingsStand().Where(r => r.State is not (RequestStanding.Nothing or RequestStanding.Asking)),
            r => Assert.Null(r.OrderBy));
        Assert.Contains(s.View.WhereThingsStand(), r => r.State == RequestStanding.Running);
    }

    [Fact]
    public void Reading_the_news_and_the_forecast_changes_nothing()
    {
        var a = SpokeB(5);
        var b = SpokeB(5);
        _ = a.View.MorningBoard();
        _ = a.View.WhereThingsStand();
        _ = a.View.DaySummary();
        Assert.Equal(b.StateHash(), a.StateHash());
        while (!a.Finished) { _ = a.View.WhereThingsStand(); a.AdvanceDay(); }
        while (!b.Finished) b.AdvanceDay();
        Assert.Equal(b.StateHash(), a.StateHash());
    }
}
