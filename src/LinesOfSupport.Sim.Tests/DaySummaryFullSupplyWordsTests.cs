using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

public class DaySummaryFullSupplyWordsTests
{
    [Fact]
    public void The_cold_zero_supply_day_uses_the_umbrella_count_without_changing_its_out_day()
    {
        var input = InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-23", "input-log-1.json"));
        var session = Replay.Run(Repo.Load("tutorial"), input, stopAfterDay: 8);
        var hash = session.StateHash();
        var summary = session.View.DaySummary()!;
        Assert.Equal("Day 8: nothing finished and one thing was not supplied in full.", summary.Sentence);
        Assert.Single(summary.Short);
        Assert.Contains("was at 0%", summary.Short[0]);
        Assert.Equal(1, session.Metrics["site.hubSite.mhe.downDays"]);
        Assert.Equal(hash, session.StateHash());
    }
    [Fact]
    public void Half_supply_uses_the_same_umbrella_words_without_counting_an_out_day()
    {
        var input = InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-22-9", "input-log-1.json"));
        var session = Replay.Run(Repo.Load("tutorial"), input, stopAfterDay: 3);
        var hash = session.StateHash();
        var summary = session.View.DaySummary()!;
        Assert.Equal(2, summary.Short.Count);
        Assert.All(summary.Short, line => Assert.Contains("was at 50%", line));
        Assert.EndsWith("2 things were not supplied in full.", summary.Sentence);
        Assert.Equal(0, session.Metrics["site.hubSite.water.downDays"]);
        Assert.Equal(hash, session.StateHash());
    }
}
