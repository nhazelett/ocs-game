using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

/// <summary>
/// DESK-110 (Fable's review of PR 330's day 5 capture, 18 Nov 2026; the 18 Nov Claude play, note 18): on the morning of
/// tutorial day 5 Today says the BPA call Avery is writing comes without the drivers Dunn said the request needs
/// (DESK-103), and the buy summary for the purchase order that brings them said, in amber, "It is on top of the BPA call
/// being written with Harbor Equipment Co. Every day it would run is already covered." The two views disagreed, and the
/// summary warned the player off the buy the tutorial is teaching. A buy that brings a part the request needs is not on
/// top of something that leaves it out as if it added nothing.
/// </summary>
public class OnTopOfTests
{
    private static InputLog Play()
        => InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-18", "input-log.json"));

    /// <summary>The morning of day 5 of the 18 Nov play: the call is with Avery and Dunn has said drivers are needed.</summary>
    private static Session Day5() => Replay.Run(Repo.Load("tutorial"), Play(), stopAfterDay: 4);

    /// <summary>The play's day 5 purchase order with Coastal Rentals, which brings forklifts, operators and maintenance.</summary>
    private static AwardAction CoastalOrder() => (AwardAction)Play().Actions.Single(a => a.Day == 5 && a is AwardAction);

    private const string OnTopWithout = "It is on top of the BPA call being written with Harbor Equipment Co, which comes without operators.";

    [Fact]
    public void The_order_with_drivers_is_not_a_double_buy_of_the_call_without_them()
    {
        var s = Day5();
        Assert.True(s.Apply(Play().Actions.Single(a => a.Day == 5 && a is AddLineAction)).Accepted);
        var summary = s.View.AwardSummary(CoastalOrder())!;
        Assert.Contains("It brings forklifts, operators, and maintenance.", summary.Sentence);
        Assert.EndsWith(OnTopWithout, summary.Sentence, StringComparison.Ordinal);
        Assert.DoesNotContain("already covered", summary.Sentence, StringComparison.Ordinal);
        Assert.False(summary.Attention);
        Assert.Null(summary.Why);
    }

    [Fact]
    public void An_order_that_leaves_out_the_drivers_too_says_to_add_them_first()
    {
        // Dunn has said it, but the player has not put drivers on the request, so the order as written brings none either
        // (the quote says so) and adds nothing the call leaves out. D40 (DESK-119, Claude play 20 Nov): the summary says
        // what is missing and who said so, in place of "Every day it would run is already covered.", which sent the 20 Nov
        // player away from the vendor that brings drivers.
        var s = Day5();
        var summary = s.View.AwardSummary(CoastalOrder())!;
        Assert.DoesNotContain("which comes without", summary.Sentence, StringComparison.Ordinal);
        Assert.DoesNotContain("already covered", summary.Sentence, StringComparison.Ordinal);
        Assert.True(summary.Attention);
        Assert.Equal("It leaves out operators, which SMSgt Ray Dunn said the request needs: add what you learned first.", summary.Why);
    }

    [Fact]
    public void The_quote_says_what_the_call_in_work_leaves_out()
    {
        var s = Day5();
        Assert.True(s.Apply(Play().Actions.Single(a => a.Day == 5 && a is AddLineAction)).Accepted);
        var gaps = s.View.Preview(CoastalOrder()).SoftGaps;
        Assert.Contains(gaps, g => g.StartsWith("MSgt Joan Avery is already writing the BPA call with Harbor Equipment Co for material handling equipment at Hub, 3 forklifts", StringComparison.Ordinal)
                                   && g.EndsWith(" from day 8, without operators; this would be on top of it.", StringComparison.Ordinal));
    }

    [Fact]
    public void A_second_order_with_drivers_is_still_a_double_buy()
    {
        // Once the order with drivers is in work, another forklift from the same shop brings nothing the request still lacks
        // (Coastal Rentals has one left on those days).
        var s = Day5();
        Assert.True(s.Apply(Play().Actions.Single(a => a.Day == 5 && a is AddLineAction)).Accepted);
        Assert.True(s.Apply(CoastalOrder()).Accepted);
        var summary = s.View.AwardSummary(CoastalOrder() with { Id = "again", Quantity = 1 })!;
        Assert.Contains("It is on top of the BPA call being written with Harbor Equipment Co and the written purchase order being written with Coastal Rentals.", summary.Sentence);
        Assert.Contains("The BPA call being written with Harbor Equipment Co comes without operators.", summary.Sentence);
        Assert.True(summary.Attention);
        Assert.Equal("Every day it would run is already covered.", summary.Why);
    }

    [Fact]
    public void Before_anyone_says_drivers_are_needed_the_order_is_a_double_buy_as_before()
    {
        // Day 4, the call in, Dunn not yet answered: the request does not ask for drivers, so the order adds nothing it needs.
        var log = Play();
        var s = Replay.Run(Repo.Load("tutorial"), log, stopAfterDay: 3);
        foreach (var a in log.Actions.Where(a => a.Day == 4)) Assert.True(s.Apply(a).Accepted);
        var summary = s.View.AwardSummary(CoastalOrder() with { Day = 4 })!;
        Assert.Contains("It is on top of the BPA call being written with Harbor Equipment Co.", summary.Sentence);
        Assert.True(summary.Attention);
    }

    [Fact]
    public void Asking_changes_nothing()
    {
        var a = Day5();
        var b = Day5();
        _ = a.View.AwardSummary(CoastalOrder()); _ = a.View.Preview(CoastalOrder());
        a.AdvanceDay(); b.AdvanceDay();
        Assert.Equal(b.StateHash(), a.StateHash());
    }
}
