using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-216 (Claude play 22 Nov, third play, note P2-17): on day 13 the dining hall's vendor problem offered
/// Re-source, which opened a buy form whose only supplier was Island Camp Services, the vendor being replaced.</summary>
public class ResourceReasonTests
{
    [Fact]
    public void Re_source_says_when_no_other_supplier_offers_the_capability()
    {
        var log = InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-22-3", "input-log-2.json"));
        var s = Replay.Run(Repo.Load("tutorial"), log, stopAfterDay: 12);
        var issue = Assert.Single(s.View.PerformanceIssues, i => i.CapabilityId == "food");
        Assert.Equal("No other supplier the shop knows offers food service at Hub.", s.View.ResourceReason(issue.Id));
        Assert.Null(s.View.ResourceReason("no-such-issue"));
    }
}
