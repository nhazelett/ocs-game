using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-225 (Claude play 22 Nov seventh, note P2-25). The second player handed the waste order from Novak to
/// Avery on day 8; the request showed 6.5 hours for her, and the full record said "7 hours left". The record now says the
/// hours to the half hour, as every screen does (DESK-14).</summary>
public class HandOverHoursTests
{
    [Fact]
    public void The_hand_over_record_says_the_half_hour_the_request_showed()
    {
        var log = InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-22-7", "input-log-2.json"));
        var s = Replay.Run(Repo.Load("tutorial"), log, stopAfterDay: 8);
        var line = s.Records.Single(r => r.Text.Contains("took over", StringComparison.Ordinal)).Text;
        Assert.Equal("MSgt Joan Avery took over written purchase order with Main Island Sanitation for waste removal at Hub from SrA Eli Novak: 6.5 hours left.", line);
    }
}
