using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-170 (Claude play 21 Nov, sixth play, note 34): on the morning of day 16 the map said "Hub · Fuel: supplier
/// failure reported" and Today said nothing new. A host nation failure the stock on hand covered is news, in one sentence
/// that says whose it is, and not an alert.</summary>
public class TheirFailureNewsTests
{
    private const string Said = "Main Island Utilities, the host nation, missed a fuel delivery at Hub on day 15; the stock on hand covered it, and the host nation puts it right.";

    private static Session SixthPlay(int throughDay)
        => Replay.Run(Repo.Load("tutorial"), InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-21-6", "input-log.json")), stopAfterDay: throughDay);

    [Fact]
    public void The_sixth_play_hears_on_day_16_that_the_host_nation_missed_fuel_and_the_stock_covered_it()
    {
        var s = SixthPlay(15);
        Assert.Equal(16, s.Day);
        var board = s.View.MorningBoard();
        var item = Assert.Single(board.New, n => n.Text == Said);
        Assert.Equal(NewKind.News, item.Kind);
        Assert.NotNull(item.RecordId);
        Assert.Equal(RecordKind.VendorFailure, s.Records.Single(r => r.Id == item.RecordId).Kind);
        Assert.DoesNotContain(board.Items, i => i.Severity == BoardSeverity.Alert && i.RecordId == item.RecordId);
        Assert.Contains(s.View.DaySummary()!.News, n => n.Text == Said);
        // The map's line for the same night has its match on Today.
        Assert.Contains(s.View.MapDay().Changes, c => c.Kind == "vendorFailure" && c.RecordId == item.RecordId && c.Known);
    }

    [Fact]
    public void The_office_s_own_vendors_and_failures_that_left_a_site_short_are_not_this_news()
    {
        var s = SixthPlay(16);
        foreach (var d in Enumerable.Range(1, 16))
        {
            var day = SixthPlay(d);
            Assert.All(day.View.MorningBoard().New.Where(n => n.Text.Contains(", the host nation, ", StringComparison.Ordinal)
                                                              || n.Text.Contains(", under the ACSA, ", StringComparison.Ordinal)),
                n => Assert.Contains(day.Records.Single(r => r.Id == n.RecordId).Subject,
                                     day.Arrangements.Where(a => a.Instrument is "hostNation" or "acsa").Select(a => a.Id)));
        }
        Assert.DoesNotContain(s.View.MorningBoard().New, n => n.Text.StartsWith("Island Camp Services", StringComparison.Ordinal));
    }
}
