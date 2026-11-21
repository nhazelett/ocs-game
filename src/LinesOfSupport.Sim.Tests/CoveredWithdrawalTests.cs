using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-198 (Claude play 21 Nov, eleventh play, note P1-16). On the morning of day 9 of the first log the honey
/// trucks' withdrawal was "1 alert" though the purchase order from day 13 was already awarded. A withdrawal covered from
/// the day after its last day is news, not an alert, and names what takes over.</summary>
public class CoveredWithdrawalTests
{
    private static InputLog FirstLog()
        => InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-21-11", "input-log-1.json"));

    private const string Said = "The host nation withdrew inherited host nation support for waste removal at Hub; last day of support 12; the purchase order from Main Island Sanitation takes over on day 13.";

    [Fact]
    public void Day_9_the_covered_withdrawal_is_news_naming_the_order()
    {
        var s = Replay.Run(Repo.Load("tutorial"), FirstLog(), stopAfterDay: 8);
        var board = s.View.MorningBoard();
        var item = Assert.Single(board.Items, i => i.Text.StartsWith("The host nation withdrew", StringComparison.Ordinal));
        Assert.StartsWith(Said, item.Text);
        Assert.Equal(BoardSeverity.Info, item.Severity);
        Assert.DoesNotContain(board.Items, i => i.Severity == BoardSeverity.Alert);
        Assert.Contains(board.New, n => n.Text.StartsWith(Said, StringComparison.Ordinal));
        // The record itself is unchanged.
        Assert.Contains(s.Records, r => r.Text.StartsWith("The host nation withdrew inherited host nation support for waste removal at Hub; last day of support 12. Host", StringComparison.Ordinal));
    }

    [Fact]
    public void With_nothing_behind_it_the_withdrawal_stays_an_alert()
    {
        var log = FirstLog();
        var withoutWaste = log with { Actions = [.. log.Actions.Where(a => !(a is AwardAction { Card: "tutWaste" }))] };
        var s = Replay.Run(Repo.Load("tutorial"), withoutWaste, stopAfterDay: 8);
        var item = Assert.Single(s.View.MorningBoard().Items, i => i.Text.StartsWith("The host nation withdrew", StringComparison.Ordinal));
        Assert.Equal(BoardSeverity.Alert, item.Severity);
        Assert.DoesNotContain("takes over", item.Text);
    }
}
