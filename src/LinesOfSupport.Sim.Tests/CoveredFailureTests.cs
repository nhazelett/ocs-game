using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-184 (D50; Claude play 21 Nov, ninth play, notes P1-29, P1-35). On day 15 of the first log Today said 0
/// alerts and "met so far" beside "Main Island Sanitation waste removal at Hub had equipment down", and the debrief never
/// said what happened. The failed buy was the two-day SF 44 bridge; the purchase order from the same vendor, running the
/// same days, kept Hub fully supplied. The warning and the record now say so in one sentence.</summary>
public class CoveredFailureTests
{
    private const string Covered =
        "The SF 44 from Main Island Sanitation had equipment down on waste removal at Hub; the purchase order from Main Island Sanitation covered the day, so nothing was short.";

    private static InputLog FirstLog()
        => InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-21-9", "input-log-1.json"));

    [Fact]
    public void The_day_15_warning_says_the_purchase_order_covered_the_bridge()
    {
        var s = Replay.Run(Repo.Load("tutorial"), FirstLog(), stopAfterDay: 14);
        Assert.Equal(15, s.Day);
        var board = s.View.MorningBoard();
        var failure = Assert.Single(board.Items, i => i.Category == BoardCategory.Failure);
        Assert.Equal(BoardSeverity.Warning, failure.Severity);
        Assert.Equal(Covered, failure.Text);
        Assert.DoesNotContain(board.Items, i => i.Severity == BoardSeverity.Alert);
        // The failure link still opens the bridge that failed.
        Assert.Equal(BoardLinkKind.Arrangement, failure.LinkKind);
        Assert.Contains(s.Records, r => r.Id == failure.RecordId && r.Kind == RecordKind.VendorFailure && r.Subject == failure.LinkId);
    }

    [Fact]
    public void The_debriefs_record_keeps_the_failure_and_names_the_cover()
    {
        var s = Replay.Run(Repo.Load("tutorial"), FirstLog());
        Assert.True(s.Finished);
        var report = SessionReport.ToText(s);
        Assert.Contains($"FAIL      {Covered}", report, StringComparison.Ordinal);
        // A failure nothing covered keeps its words: the tutorial's, spoke-b-90's and sustainment's other FAIL lines in the
        // golden reports are unchanged.
    }
}
