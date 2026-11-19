using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>
/// DESK-112 (Claude play 19 Nov 2026, notes 7, 22, 28): three sentences the player read as wrong. The debrief said the
/// camp vendor's lodging "failed with full operator and maintenance coverage", and lodging has no operator part; naming
/// TSgt Pratt on the waste order lowered his watch on the forklift order with nothing said; Mills promised a note each
/// morning and the first one came with the day's results.
/// </summary>
public class PlaySentencesTests
{
    private static InputLog Play()
        => InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-19", "input-log.json"));

    [Fact]
    public void A_failure_names_only_the_plant_parts_its_capability_has()
    {
        var debrief = Replay.Run(Repo.Load("tutorial"), Play()).View.Debrief();
        var lodging = debrief.Gaps.Where(g => g.CapabilityId == "lodging").ToList();
        Assert.Equal(2, lodging.Count);
        var texts = lodging.SelectMany(g => g.Factors).Select(f => f.Text).ToList();
        Assert.True(texts.Any(t => t.StartsWith("Island Camp Services lodging at Hub failed on day 4 with full maintenance coverage.", StringComparison.Ordinal)), string.Join(" | ", texts));
        Assert.DoesNotContain(texts, t => t.Contains("operator", StringComparison.Ordinal));
    }

    [Fact]
    public void Naming_a_COR_who_watches_another_contract_says_what_it_does_to_that_one()
    {
        // The morning of day 8: the purchase order with Coastal Rentals (Pratt its COR) was signed on day 7.
        var s = Replay.Run(Repo.Load("tutorial"), Play(), stopAfterDay: 7);
        var pratt = Assert.Single(s.View.CorChoices("waste"), c => c.Id == "corPratt");
        Assert.Equal(1, pratt.Watching);
        Assert.True(pratt.Why.EndsWith("Naming TSgt Pratt here also lowers the watch on Coastal Rentals material handling equipment at Hub to about 65%.", StringComparison.Ordinal), pratt.Why);

        // SSgt Lane watches the two inherited camp services calls; naming him lowers both, counted.
        var lane = Assert.Single(s.View.CorChoices("waste"), c => c.Id == "corLane").Why;
        Assert.True(lane.EndsWith("Naming SSgt Lane here also lowers the watch on the 2 other contracts.", StringComparison.Ordinal), lane);

        // On day 4 nothing is signed, so Pratt's line has no such sentence.
        var early = Replay.Run(Repo.Load("tutorial"), Play(), stopAfterDay: 3);
        Assert.DoesNotContain("also lowers the watch", Assert.Single(early.View.CorChoices("mhe"), c => c.Id == "corPratt").Why, StringComparison.Ordinal);
    }

    [Fact]
    public void Mills_promises_a_note_each_day()
    {
        var json = File.ReadAllText(Path.Combine(Repo.Scenario("tutorial"), "scenario.json"));
        Assert.Contains("I'll leave you a note each day while I'm still around.", json, StringComparison.Ordinal);
        Assert.DoesNotContain("each morning", json, StringComparison.Ordinal);
    }
}
