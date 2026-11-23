using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

/// <summary>D48 (DESK-172; Claude play 21 Nov, sixth play, notes 23, 24). The sixth play asked SMSgt Dunn about the
/// forklifts on day 6 and, in the same hour, bought three Coastal forklifts on the card; the pinned summary said only price
/// and start. When a buy leaves out a part at risk (DESK-59) and the person the request came from has not answered, the
/// summary's last sentence names the part and where the ask stands. Once they have answered, D40 says what they told.</summary>
public class NotYetConfirmedTests
{
    private static InputLog Sixth()
        => InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-21-6", "input-log.json"));

    private static AwardAction CoastalCard(InputLog log) => log.Actions.OfType<AwardAction>().Single(a => a.Id == "player-award-3");

    [Fact]
    public void The_sixth_plays_card_buy_on_day_6_names_operators_and_where_the_ask_of_Dunn_stands()
    {
        var log = Sixth();
        var s = Replay.Run(Repo.Load("tutorial"), log, stopAfterDay: 5);
        Assert.Equal(6, s.Day);
        var buy = CoastalCard(log);

        var before = s.View.AwardSummary(buy)!;
        Assert.Equal("It leaves out operators; SMSgt Ray Dunn has not been asked.", before.Why);
        Assert.True(before.Attention);
        Assert.EndsWith(before.Why, before.Sentence);
        Assert.Equal(3, SentenceCount(before.Sentence));   // D46: still at most three
        Assert.True(s.View.Preview(buy).Accepted);          // it says so; it does not stop the buy

        var ask = log.Actions.OfType<AskAboutAction>().Single(a => a.Day == 6);
        Assert.True(s.Apply(ask).Accepted);
        var asking = s.View.AwardSummary(buy)!;
        // D57 (a), DESK-218: when the answer comes and what waiting for it costs, in the same sentence.
        Assert.Equal("It leaves out operators; MSgt Joan Avery is asking SMSgt Ray Dunn, whose answer comes in tonight; ordered tomorrow, after the answer, it starts on day 8 or 9: material handling equipment at Hub is out 1 day in all: all the commander allows.", asking.Why);
        Assert.EndsWith(asking.Why, asking.Sentence);
        Assert.Equal(3, SentenceCount(asking.Sentence));
        // The answer-due sentence has been said, so it is not in the quote's notes (D57 a); the view keeps it for the game.
        Assert.NotNull(asking.AnswerDue);
        Assert.DoesNotContain(s.View.Preview(buy).SoftGaps, g => g == asking.AnswerDue);

        // Not bought this time: the next morning Dunn has answered, D40 names what he said (DESK-156), and D48 no longer applies.
        s.AdvanceDay();
        var told = s.View.AwardSummary(buy with { Day = 7 })!;
        Assert.Equal("It leaves out operators, which SMSgt Ray Dunn said the request needs: add what you learned first; Coastal Rentals brings them at the same price.", told.Why);
    }

    [Fact]
    public void An_ask_the_player_makes_is_said_as_yours()
    {
        var log = Sixth();
        var s = Replay.Run(Repo.Load("tutorial"), log, stopAfterDay: 5);
        var ask = log.Actions.OfType<AskAboutAction>().Single(a => a.Day == 6) with { Assignee = null };
        Assert.True(s.Apply(ask).Accepted);
        var asking = s.View.AwardSummary(CoastalCard(log))!;
        Assert.StartsWith("It leaves out operators; you are asking SMSgt Ray Dunn, whose answer comes in ", asking.Why);
    }

    [Fact]
    public void Goods_and_a_buy_with_every_part_say_nothing_of_it()
    {
        var log = Sixth();
        // Island Mart's water on day 1: water from a vendor whose offer leaves the plant out is goods (DESK-59).
        var s = Replay.Run(Repo.Load("tutorial"), log, stopAfterDay: 0);
        var water = log.Actions.OfType<AwardAction>().Single(a => a.Id == "player-award-1");
        Assert.Null(s.View.AwardSummary(water)!.Why);

        // Day 8's second Coastal buy, after operators were added: nothing at risk is left out.
        var t = Replay.Run(Repo.Load("tutorial"), log, stopAfterDay: 7);
        var second = log.Actions.OfType<AwardAction>().Single(a => a.Id == "player-award-5");
        Assert.DoesNotContain("leaves out", t.View.AwardSummary(second)?.Sentence ?? "");
    }

    private static int SentenceCount(string s) => s.Split(". ").Length;

    /// <summary>DESK-218 (D57 a; Claude play 22 Nov, fourth play, notes P1-18, P2-13): on day 4 the second player asked Dunn
    /// and put Avery on Harbor's BPA call in the same minute. The quote's third sentence said "answer due day 4" and D53's
    /// "still starts on day 8" sat below the fold; the player ordered blind and stopped the call the next morning.</summary>
    [Fact]
    public void Fourth_play_day_4_harbor_call_says_the_wait_costs_nothing_where_the_answer_is_named()
    {
        var log = InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-22-4", "input-log-2.json"));
        var s = Replay.Run(Repo.Load("tutorial"), log, stopAfterDay: 3);
        Assert.Equal(4, s.Day);
        Assert.True(s.Apply(log.Actions.OfType<AskAboutAction>().First(x => x.Day == 4)).Accepted);
        var buy = log.Actions.OfType<AwardAction>().First(x => x.Day == 4);
        var summary = s.View.AwardSummary(buy)!;
        Assert.Equal("It leaves out operators; you are asking SMSgt Ray Dunn, whose answer comes in tonight; a BPA call tomorrow still starts on day 8 if the supplier accepts.", summary.Why);
        Assert.EndsWith(summary.Why, summary.Sentence);
        Assert.Equal(3, SentenceCount(summary.Sentence));
        Assert.DoesNotContain(s.View.Preview(buy).SoftGaps, g => g.Contains("Ordered tomorrow") || g.Contains("A BPA call tomorrow"));
    }
}
