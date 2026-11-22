using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-226 (Claude play 22 Nov seventh, note P2-26; second play P2-12). Day 11's summary began "nothing finished
/// and everything was supplied in full" over a COR report that Island Camp Services missed its food service delivery that
/// day. On a day nothing was short and the office heard of a miss, the first line says so.</summary>
public class ReportedMissSummaryTests
{
    private static Session Seventh(int throughDay)
        => Replay.Run(Repo.Load("tutorial"), InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-22-7", "input-log-2.json")), stopAfterDay: throughDay);

    [Fact]
    public void Day_11_says_nothing_was_short_though_a_miss_was_reported()
        => Assert.Equal("Day 11: nothing finished and nothing was short, though one missed delivery was reported.", Seventh(11).View.DaySummary()!.Sentence);

    [Fact]
    public void A_day_with_no_miss_is_still_supplied_in_full()
        => Assert.Equal("Day 1: one job finished and everything was supplied in full.", Seventh(1).View.DaySummary()!.Sentence);

    [Fact]
    public void A_host_nation_miss_the_news_names_counts_too()
    {
        var s = Replay.Run(Repo.Load("tutorial"), InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-21-6", "input-log.json")), stopAfterDay: 15);
        Assert.EndsWith(", though one missed delivery was reported.", s.View.DaySummary()!.Sentence, StringComparison.Ordinal);
    }
}
