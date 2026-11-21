namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-192 (Claude play 21 Nov, tenth play, notes P2-7, P2-16). Dunn's request said "through the end of the month"
/// for thirteen days, and the waste line's "not one day missed" read against "A day at 50% counts as short, not out" as if a
/// half-pumped day were missed. The words now match the request's days and the pass line's count.</summary>
public class TutorialWordsTests
{
    [Fact]
    public void Dunn_asks_for_the_days_his_request_has()
    {
        var card = Repo.Load("tutorial").Scenario.RequirementCards.Single(c => c.Id == "tutForklift");
        Assert.Equal(13, card.DurationDays);
        Assert.EndsWith("Three forklifts from day 8, for thirteen days.", card.Message);
    }

    [Fact]
    public void The_waste_line_says_what_counts_as_missed()
    {
        var line = Repo.Load("tutorial").Scenario.PassCriteria.Single(p => p.Id == "wasteUp");
        Assert.Contains("a day under half pumped counts as missed", line.Says);
    }
}
