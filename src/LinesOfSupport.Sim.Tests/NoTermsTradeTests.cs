using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-204 (D54; Claude play 22 Nov, notes P1-12, P2-20). Both players bought water and forklifts on the card,
/// each quote saying there were no written terms; the seed brought confusion days, and the debrief graded each
/// "Avoidable: what you needed to know was in front of you" and said "you could have avoided all of them". Such a gap now
/// reads as a trade taken knowingly, says what written terms would have cost, and is counted apart.</summary>
public class NoTermsTradeTests
{
    private static Session FirstPlay()
        => Replay.Run(Repo.Load("tutorial"), InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-22", "input-log-1.json")));

    [Fact]
    public void Confusion_gaps_on_card_buys_read_as_a_trade()
    {
        var d = FirstPlay().View.Debrief();
        var water = d.Gaps.Single(g => g.CapabilityId == "water" && g.StartDay == 3);
        var food = d.Gaps.Single(g => g.CapabilityId == "food" && g.StartDay == 3);
        var mhe = d.Gaps.Where(g => g.CapabilityId == "mhe").ToList();
        Assert.Equal([12, 15], mhe.Select(g => g.StartDay));

        // The tag is unchanged (SPEC 4); the line under the name says what it was.
        foreach (var g in mhe.Prepend(water))
        {
            Assert.Equal(GapTag.Avoidable, g.Tag);
            Assert.True(g.PriceOfNoTerms);
            // DESK-215 (D56): water's written route could not have started by day 3, so it reads as worth taking.
            Assert.Equal(g == water ? "A risk worth taking: no written order could have started by day 3." : DebriefWords.NoTermsTagLine, g.TagLine);
            // DESK-211 (D55 a): the closing sentence names the written route the shop knew that day (WrittenRouteTests).
            Assert.DoesNotContain("a purchase order or a BPA call", Assert.Single(g.Factors).Text, StringComparison.Ordinal);
        }
        // A knock-on is not counted as a trade of its own; DESK-211 (D55 c) says it went down with water.
        Assert.False(food.PriceOfNoTerms);
        Assert.Equal("It went down with water at Hub, a risk worth taking.", food.TagLine);

        var text = d.Summary!.Text;
        Assert.Contains("Food service at Hub went down with it. There were two more gaps, both the price of buys with no written terms.", text);
        Assert.DoesNotContain("you could have avoided", text);
    }

    [Fact]
    public void Other_avoidable_gaps_keep_their_words()
    {
        // The sixth play of 21 Nov cut a buy that would have covered day 8: that gap is the player's miss, not a trade.
        var s = Replay.Run(Repo.Load("tutorial"), InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-21-6", "input-log.json")));
        var d = s.View.Debrief();
        var cut = d.Gaps.Single(g => g.CapabilityId == "mhe" && g.StartDay == 8);
        Assert.False(cut.PriceOfNoTerms);
        Assert.Equal("Avoidable: what you needed to know was in front of you.", cut.TagLine);
    }
}
