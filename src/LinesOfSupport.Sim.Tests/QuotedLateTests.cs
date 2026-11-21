using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-130 (D43; Claude play 20 Nov, third play, note 2). The player took a written purchase order quoted to start
/// "day 8 or 9" for forklifts wanted from day 8; it started on day 9 and the debrief put the gap on the host nation
/// withdrawal. The gap's reason now names the quote and the day it started.</summary>
public class QuotedLateTests
{
    private static Session ThirdPlay()
    {
        var log = InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-20-3", "input-log.json"));
        return Replay.Run(Repo.Load("tutorial"), log);
    }

    [Fact]
    public void The_forklift_gap_says_the_purchase_order_was_quoted_to_start_on_day_8_or_9_and_started_on_day_9()
    {
        var s = ThirdPlay();
        var fork = s.Arrangements.Single(a => a.VendorId == "coastalRentals" && a.CapabilityId == "mhe");
        Assert.Equal((8, 9, 9), (fork.QuotedStartMin, fork.QuotedStartMax, fork.StartDay));
        var d = s.View.Debrief();
        var gap = d.Gaps.Single(g => g.SiteId == "hubSite" && g.CapabilityId == "mhe");
        Assert.Equal(GapTag.Avoidable, gap.Tag);
        var uncovered = gap.Factors.Single(f => f.Kind == GapFactorKind.Uncovered);
        Assert.EndsWith("The purchase order with Coastal Rentals was quoted to start on day 8 or 9 and started on day 9.", uncovered.Text);
        Assert.DoesNotContain("did not start until", uncovered.Text);
        Assert.Contains("was quoted to start on day 8 or 9 and started on day 9.", d.Summary!.Text);
    }

    [Fact]
    public void A_buy_whose_whole_quote_was_late_keeps_the_plain_sentence()
    {
        // Spoke B's enduring-only approach: the letter contract for power was quoted to start between day 16 and day 20 for
        // power wanted from day 7. The player chose a late buy knowingly; the gap keeps "did not start until day 18".
        var log = InputLog.Load(Path.Combine(Repo.Scenario("spoke-b"), "golden", "approach4-enduring-only.json"));
        var s = Replay.Run(Repo.Load("spoke-b"), log);
        var meridian = s.Arrangements.Single(a => a.VendorId == "vendorB" && a.CapabilityId == "power");
        Assert.True(meridian.QuotedStartMin > 7);
        var factors = s.View.Debrief().Gaps.SelectMany(g => g.Factors).ToList();
        Assert.DoesNotContain(factors, f => f.Text.Contains("was quoted to start"));
        Assert.Contains(factors, f => f.Text.Contains("Meridian Site Services power at Spoke B did not start until day 18."));
    }
}
