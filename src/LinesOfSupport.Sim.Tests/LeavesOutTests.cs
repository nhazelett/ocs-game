using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>
/// DESK-103 (Claude play 18 Nov 2026, note 18): a buy in work that leaves out a part says so on Today. On day 4 the player
/// asked SMSgt Dunn about the forklifts and called the Harbor Equipment BPA without drivers in the same hour; Dunn's
/// answer that night said the rental has to come with drivers. On the morning of day 5 Today said "0 alerts" and Where
/// things stand said only who was writing the call.
/// </summary>
public class LeavesOutTests
{
    private static InputLog Play()
        => InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-18", "input-log.json"));

    /// <summary>The morning of day 5 of the 18 Nov play: the call is with Avery, Dunn has answered, nothing else bought.</summary>
    private static Session Day5() => Replay.Run(Repo.Load("tutorial"), Play(), stopAfterDay: 4);

    private static RequestStanding Forklifts(Session s) => Assert.Single(s.View.WhereThingsStand(), e => e.CardId == "tutForklift");

    [Fact]
    public void The_standing_names_the_call_and_who_said_drivers_are_needed()
    {
        var s = Day5();
        Assert.Equal(5, s.View.Day);
        var f = Forklifts(s);
        Assert.Equal(RequestStanding.Buying, f.State);
        Assert.Equal("The BPA call MSgt Joan Avery is writing with Harbor Equipment Co comes without operators, and SMSgt Ray Dunn said the request needs them.", f.LeavesOut);
        Assert.EndsWith(f.LeavesOut!, f.Sentence, StringComparison.Ordinal);
        Assert.StartsWith("MSgt Joan Avery is writing the BPA call with Harbor Equipment Co", f.Sentence, StringComparison.Ordinal);
    }

    [Fact]
    public void The_request_counts_in_the_mornings_alerts()
    {
        var board = Day5().View.MorningBoard();
        var alert = Assert.Single(board.Items, i => i.Severity == BoardSeverity.Alert);
        Assert.Equal(BoardLinkKind.Card, alert.LinkKind);
        Assert.Equal("tutForklift", alert.LinkId);
        Assert.Equal(BoardCategory.Request, alert.Category);
        Assert.Equal("dunn", alert.Person?.Id);
        Assert.Equal("Material handling equipment for Hub: the BPA call MSgt Joan Avery is writing with Harbor Equipment Co comes without operators, and SMSgt Ray Dunn said the request needs them.", alert.Text);
        Assert.DoesNotContain("tutForklift", alert.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void Adding_the_part_keeps_the_alert_until_a_buy_brings_it()
    {
        var s = Day5();
        var log = Play();
        var add = log.Actions.Single(a => a.Day == 5 && a is AddLineAction);
        Assert.True(s.Apply(add).Accepted);
        // The part is on the request now; the call still leaves it out, and the standing says so once.
        var f = Forklifts(s);
        Assert.NotNull(f.LeavesOut);
        Assert.DoesNotContain("still to find", f.Sentence, StringComparison.Ordinal);
        Assert.Contains(s.View.MorningBoard().Items, i => i.Severity == BoardSeverity.Alert && i.LinkId == "tutForklift");

        // The purchase order with Coastal Rentals brings drivers: the request is no longer short of them.
        Assert.True(s.Apply(log.Actions.Single(a => a.Day == 5 && a is AwardAction)).Accepted);
        Assert.Null(Forklifts(s).LeavesOut);
        Assert.DoesNotContain(s.View.MorningBoard().Items, i => i.Severity == BoardSeverity.Alert && i.LinkId == "tutForklift");
    }

    [Fact]
    public void Nothing_is_flagged_before_anyone_says_drivers_are_needed()
    {
        // Day 4 of the same play, after the call went in but before Dunn answered: the request never asked for drivers.
        var log = Play();
        var s = Replay.Run(Repo.Load("tutorial"), log, stopAfterDay: 3);
        foreach (var a in log.Actions.Where(a => a.Day == 4)) Assert.True(s.Apply(a).Accepted);
        Assert.Null(Forklifts(s).LeavesOut);
        Assert.DoesNotContain(s.View.MorningBoard().Items, i => i.LinkId == "tutForklift" && i.Severity == BoardSeverity.Alert);
    }

    [Fact]
    public void A_quiet_morning_is_not_quiet_while_a_buy_leaves_out_a_part()
    {
        var s = Day5();
        Assert.False(s.View.QuietDay());
    }

    [Fact]
    public void Asking_for_it_changes_nothing()
    {
        var a = Day5();
        var b = Day5();
        _ = a.View.MorningBoard(); _ = a.View.WhereThingsStand();
        a.AdvanceDay(); b.AdvanceDay();
        Assert.Equal(b.StateHash(), a.StateHash());
    }

    [Fact]
    public void A_buy_made_after_the_answer_is_bought_around_on_purpose_and_raises_no_alert()
    {
        // Dunn's answer is in before the call goes in (D34): the request names the drivers as still to find, no alert.
        var log = Play();
        // DESK-178: a call made after the shop heard of Harbor's short week (days 4 to 8) is refused, so the late call goes in on day 9 (the play's own later
        // buys are left out).
        var s = Replay.Run(Repo.Load("tutorial"), log with { Actions = [.. log.Actions.Where(a => a.Day <= 4 && !(a.Day == 4 && a is AwardAction or AssignAction))] }, stopAfterDay: 8);
        var call = (AwardAction)log.Actions.Single(a => a.Day == 4 && a is AwardAction);
        Assert.True(s.Apply(call with { Day = 9, StartDay = 10 }).Accepted);
        var f = Forklifts(s);
        Assert.Equal(RequestStanding.Buying, f.State);
        Assert.Null(f.LeavesOut);
        Assert.DoesNotContain(s.View.MorningBoard().Items, i => i.LinkId == "tutForklift" && i.Severity == BoardSeverity.Alert);
    }

    [Fact]
    public void An_arrangement_not_yet_started_says_so_too()
    {
        // The same day 4 as the play, but a purchase order with Coastal Rentals in place of the call: it is signed that day
        // and starts on day 8, without the drivers Dunn names that night.
        var log = Play();
        var s = Replay.Run(Repo.Load("tutorial"), log, stopAfterDay: 3);
        Assert.True(s.Apply(log.Actions.Single(a => a.Day == 4 && a is AskAboutAction)).Accepted);
        var po = new AwardAction
        {
            Id = "po", Instrument = "gpc", Vendor = "coastalRentals", Card = "tutForklift", Quantity = 3, StartDay = 8,
            DurationDays = 13, Day = 4,
        };
        var r = s.Apply(po);
        Assert.True(r.Accepted, string.Join(" ", r.HardStops));
        s.AdvanceDay();
        var f = Forklifts(s);
        Assert.Equal(RequestStanding.Arranged, f.State);
        Assert.NotNull(f.LeavesOut);
        Assert.Equal("Coastal Rentals, starting on day 8, comes without operators, and SMSgt Ray Dunn said the request needs them.", f.LeavesOut);
        Assert.Contains(s.View.MorningBoard().Items, i => i.LinkId == "tutForklift" && i.Severity == BoardSeverity.Alert);
    }
}
