using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-197 (Claude play 21 Nov, eleventh play). On day 9 of the second log Lane replaced Pratt on the waste order;
/// the picker and Payments said "about 40% of problems on this contract", the record "about 100% of problems". The record
/// now gives the rate on that contract; since DESK-227 it leads with the fit words, as the picker does.</summary>
public class CorDesignationWordsTests
{
    [Fact]
    public void The_day_9_record_gives_Lanes_rate_on_the_waste_order()
    {
        var log = InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-21-11", "input-log-2.json"));
        var s = Replay.Run(Repo.Load("tutorial"), log, stopAfterDay: 9);
        var record = Assert.Single(s.Records, r => r.Text.StartsWith("SSgt Lane designated COR", StringComparison.Ordinal));
        Assert.EndsWith(", replacing TSgt Pratt (does not work with waste removal: about 40% of problems on this contract, reported the same day).", record.Text);
    }
}
