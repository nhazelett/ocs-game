using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-206 (Claude play 22 Nov, note P2-16). On day 9 of the second log the morning heading said "2 requests"
/// while the Requests tab listed three: water's own days were over and its card buy was still delivering. The heading now
/// counts a request while its days are not over or a buy for it is still supplying.</summary>
public class OpenRequestsCountTests
{
    private static InputLog SecondLog()
        => InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-22", "input-log-2.json"));

    [Fact]
    public void Day_nine_heading_counts_the_water_request_its_buy_still_supplies()
    {
        var s = Replay.Run(Repo.Load("tutorial"), SecondLog(), stopAfterDay: 8);
        Assert.Equal(9, s.Day);
        var water = s.Cards.Single(c => c.Id == "tutWater");
        Assert.True(water.RequestedStartDay + water.DurationDays - 1 < 9, "water's own days are over by day 9");
        Assert.Contains(s.Arrangements, a => a.CardId == water.Id && a.Status == ArrangementStatus.Active && a.EndDay >= 9);
        Assert.Equal(3, s.View.MorningBoard().OpenRequests);
    }

    [Fact]
    public void A_request_drops_out_the_day_after_its_last_buy_ends()
    {
        // Water's card buy supplied days 3 to 9; on day 10 nothing supplies it and its days are over.
        var s = Replay.Run(Repo.Load("tutorial"), SecondLog(), stopAfterDay: 9);
        Assert.Equal(10, s.Day);
        Assert.DoesNotContain(s.Arrangements, a => a.CardId == "tutWater" && a.EndDay >= 10);
        Assert.Equal(2, s.View.MorningBoard().OpenRequests);
    }
}
