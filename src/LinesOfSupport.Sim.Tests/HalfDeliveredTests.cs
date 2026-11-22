using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-213 (Claude play 22 Nov, second play, note P2-16): on day 3 the day said Island Mart delivered half of
/// the water; the debrief's evidence trace said "water on hand ran out". The shortfall says what the day said.</summary>
public class HalfDeliveredTests
{
    [Fact]
    public void A_confusion_day_shortfall_says_the_vendor_delivered_half()
    {
        var log = InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-22-2", "input-log-1.json"));
        var s = Replay.Run(Repo.Load("tutorial"), log, stopAfterDay: 3);
        var shortfall = s.Records.Single(r => r.Kind == RecordKind.Shortfall && r.Subject == "hubSite.water" && r.Day == 3);
        Assert.Equal("Water at Hub degraded to 50%: Island Mart delivered half of today's water.", shortfall.Text);
        Assert.DoesNotContain("on hand ran out", shortfall.Text);
        Assert.Contains(s.Records, r => r.Kind == RecordKind.ExecutionConfusion && r.Day == 3 && shortfall.CausedBy.Contains(r.Id));

        // The morning after, the line beside the level says the same.
        Assert.StartsWith("Island Mart delivered half", s.View.WhyShort("hubSite", "water"));
    }
}
