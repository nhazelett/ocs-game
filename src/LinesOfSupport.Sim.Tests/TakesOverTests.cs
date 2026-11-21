using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-188 (Claude play 21 Nov, tenth play, note P1-15). On days 8 and 9 of the first log Requests said
/// "Coastal Rentals is delivering 3 forklifts through day 9" with the purchase order from day 10 already signed, and the
/// player thought the forklifts were about to stop. The running line now names what takes over.</summary>
public class TakesOverTests
{
    private static InputLog FirstLog()
        => InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-21-10", "input-log-1.json"));

    [Fact]
    public void Day_8_names_the_order_that_takes_over()
    {
        var s = Replay.Run(Repo.Load("tutorial"), FirstLog(), stopAfterDay: 7);
        Assert.Equal(8, s.Day);
        var lines = s.View.WhereThingsStand().Select(r => r.Sentence).ToList();
        Assert.Contains(lines, l => l.StartsWith("Coastal Rentals is delivering 3 forklifts through day 9; the purchase order from Coastal Rentals takes over on day 10.", StringComparison.Ordinal));
    }

    [Fact]
    public void Lines_without_a_follow_on_are_unchanged()
    {
        var s = Replay.Run(Repo.Load("tutorial"), FirstLog(), stopAfterDay: 7);
        Assert.DoesNotContain(s.View.WhereThingsStand(), r => r.Sentence.Contains("takes over", StringComparison.Ordinal) && !r.Sentence.Contains("forklift", StringComparison.Ordinal));
    }
}
