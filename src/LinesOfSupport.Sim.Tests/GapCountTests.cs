using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-222 (D58; Claude play 22 Nov, fifth play, notes P1-23, P2-22). Both players read "2 gaps: 2 avoidable"
/// above two buttons that each said "a risk worth taking" and could not tell whether they had made a mistake. The count
/// keeps its tags and numbers and says how many avoidable gaps were risks worth taking.</summary>
public class GapCountTests
{
    private static InputLog Log(string dir, int n)
        => InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", dir, $"input-log-{n}.json"));

    [Fact]
    public void Two_risks_worth_taking_read_as_both()
    {
        var d = Replay.Run(Repo.Load("tutorial"), Log("2026-11-22-5", 1)).View.Debrief();
        Assert.Equal(2, d.Scorecard.AvoidableGaps);
        Assert.Equal(2, d.Scorecard.WorthTakingGaps);
        Assert.Equal("2 gaps: 2 avoidable, both risks worth taking · 0 discoverable · 0 unforeseeable", DebriefWords.GapCounts(d.Scorecard));
    }

    [Fact]
    public void Some_worth_taking_among_avoidable_gaps_are_counted_apart()
    {
        // The first 22 Nov play: water and food service on day 3 were risks worth taking; the forklift days were not, since
        // a purchase order with Coastal on day 5 could have started in time (D54 keeps its line there).
        var d = Replay.Run(Repo.Load("tutorial"), Log("2026-11-22", 1)).View.Debrief();
        var worth = d.Gaps.Count(g => g.TagLine?.Contains("a risk worth taking", StringComparison.OrdinalIgnoreCase) == true);
        Assert.Equal(2, worth);
        Assert.Equal(worth, d.Scorecard.WorthTakingGaps);
        Assert.InRange(d.Scorecard.WorthTakingGaps, 1, d.Scorecard.AvoidableGaps - 1);
        Assert.Contains($"{d.Scorecard.AvoidableGaps} avoidable, {d.Scorecard.WorthTakingGaps} of them risk", DebriefWords.GapCounts(d.Scorecard));
    }

    [Theory]
    [InlineData(1, 1, 1, "1 gap: 1 avoidable, a risk worth taking · 0 discoverable · 0 unforeseeable")]
    [InlineData(3, 3, 1, "3 gaps: 3 avoidable, 1 of them a risk worth taking · 0 discoverable · 0 unforeseeable")]
    [InlineData(4, 3, 3, "4 gaps: 3 avoidable, all risks worth taking · 1 discoverable · 0 unforeseeable")]
    [InlineData(2, 2, 0, "2 gaps: 2 avoidable · 0 discoverable · 0 unforeseeable")]
    public void The_count_line_reads_as_a_sentence(int gaps, int avoidable, int worth, string expected)
    {
        var s = new ScorecardView(1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, gaps, avoidable, gaps - avoidable, 0, WorthTakingGaps: worth);
        Assert.Equal(expected, DebriefWords.GapCounts(s));
    }
}
