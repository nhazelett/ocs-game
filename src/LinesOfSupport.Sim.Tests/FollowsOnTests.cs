using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-223 (Claude play 22 Nov sixth, note P2-17). The second player bridged day 8 with a one-day card buy from
/// Coastal Rentals and ordered the rest in writing from day 9. On day 6 the request said only "Coastal Rentals starts on
/// day 8: 3 forklifts through day 8." and on day 7 "(and one other arrangement)"; the player read forklifts for one day.
/// The line now names what takes over, while it is being written and once it is signed.</summary>
public class FollowsOnTests
{
    private static InputLog SecondLog()
        => InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-22-6", "input-log-2.json"));

    private static string Forklifts(int morning)
    {
        var s = Replay.Run(Repo.Load("tutorial"), SecondLog(), stopAfterDay: morning - 1);
        Assert.Equal(morning, s.Day);
        return s.View.WhereThingsStand().Select(r => r.Sentence).Single(l => l.Contains("forklift", StringComparison.Ordinal));
    }

    [Fact]
    public void Day_6_names_the_order_being_written()
        => Assert.StartsWith("Coastal Rentals starts on day 8: 3 forklifts through day 8; the purchase order with Coastal Rentals that MSgt Joan Avery is writing takes over on day 9.", Forklifts(6), StringComparison.Ordinal);

    [Fact]
    public void Day_7_names_the_signed_order_in_place_of_one_other_arrangement()
    {
        var line = Forklifts(7);
        Assert.StartsWith("Coastal Rentals starts on day 8: 3 forklifts through day 8; the purchase order from Coastal Rentals takes over on day 9.", line, StringComparison.Ordinal);
        Assert.DoesNotContain("other arrangement", line, StringComparison.Ordinal);
    }
}
