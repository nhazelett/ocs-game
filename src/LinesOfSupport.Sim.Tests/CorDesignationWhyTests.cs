using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-227 (Claude play 22 Nov seventh, note P2-22). On the morning of day 14 the second player swapped the waste
/// contract's COR from Pratt to Lane in Payments. The picker read Lane "good: reports the same day, about 40% of problems on
/// this contract" beside Pratt "fair: ... about 65%", and nothing said that naming Lane lowered her watch on food service,
/// the contract under a cure notice. A COR on a running contract now reads as the order form does.</summary>
public class CorDesignationWhyTests
{
    private static Session Day14()
    {
        var s = Replay.Run(Repo.Load("tutorial"), InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-22-7", "input-log-2.json")), stopAfterDay: 13);
        Assert.Equal(14, s.Day);
        return s;
    }

    private static Arrangement Waste(Session s) => s.Arrangements.Single(a => a.CapabilityId == "waste" && a.CreatedDay > 0);

    [Fact]
    public void The_fit_words_lead_not_the_grade()
    {
        var s = Day14();
        var waste = Waste(s);
        Assert.Equal("does not work with waste removal: about 40% of problems on this contract, reported the same day",
                     s.View.CorSummary(s.Cors.Single(c => c.Id == "corLane"), waste));
        Assert.Equal("knows waste removal: about 65% of problems on this contract, reported 2 days late",
                     s.View.CorSummary(s.Cors.Single(c => c.Id == "corPratt"), waste));
    }

    [Fact]
    public void Naming_Lane_says_what_it_lowers()
    {
        var s = Day14();
        var why = s.View.CorDesignationWhy("corLane", Waste(s).Id);
        Assert.NotNull(why);
        Assert.StartsWith("SSgt Lane (3 FSS) does not work with waste removal and already watches 2 other contracts", why, StringComparison.Ordinal);
        Assert.Contains("Naming SSgt Lane here also lowers the watch on", why, StringComparison.Ordinal);
    }

    [Fact]
    public void Nothing_for_the_COR_already_on_it()
    {
        var s = Day14();
        Assert.Null(s.View.CorDesignationWhy("corPratt", Waste(s).Id));
    }
}
