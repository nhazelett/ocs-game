using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-145 (Claude play 21 Nov, notes 2, 3, 5, 6, 11, 16), on that play's own log: waste removal at Hub ran at
/// 50% on day 15 with its report still on the way, and mission output stayed at 100%.</summary>
public class ShortDayAndReportTests
{
    private static Session Play(int? stopAfterDay = null)
    {
        var log = InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-21", "input-log.json"));
        return Replay.Run(Repo.Load("tutorial"), log, stopAfterDay);
    }

    [Fact]
    public void The_morning_after_the_short_day_says_it_is_not_an_out_day_and_when_the_report_comes()
    {
        var s = Play(15);
        Assert.Equal(16, s.Day);
        var waste = s.View.Bar().Lines.Single(l => l.Text.StartsWith("Waste removal at Hub", StringComparison.Ordinal));
        Assert.EndsWith("out 0 days so far; none allowed. A day at 50% counts as short, not out.", waste.Text);
        Assert.Equal(BarState.Met, waste.State);
        var water = s.View.Bar().Lines.Single(l => l.Text.StartsWith("Water at Hub", StringComparison.Ordinal));
        Assert.DoesNotContain("counts as short", water.Text);
        Assert.Matches(@"^The site reports it short\. Nobody has told you why yet; TSgt Pratt's report is due on day \d+\.$",
            s.View.WhyShort("hubSite", "waste"));
    }

    [Fact]
    public void The_debrief_does_not_say_mission_output_dipped_when_it_stayed_at_100_percent()
    {
        var s = Play();
        Assert.Equal(1.0, s.Metrics["mission.output"]);
        var mission = s.View.Debrief().Carried.Single(c => c.Sentence.StartsWith("Carried by the lines above", StringComparison.Ordinal));
        Assert.Equal("Carried by the lines above: mission output never dipped, though waste removal at Hub ran short on day 15.", mission.Sentence);
    }

    [Fact]
    public void The_cure_notice_reads_cure_notice_on()
    {
        var s = Play();
        var cure = s.Work.Single(w => w.Kind == WorkKind.CureNotice);
        Assert.StartsWith("Cure notice on the performance issue", cure.Label);
    }

    [Fact]
    public void A_known_notice_says_it_counts_from_the_signed_order()
    {
        var s = Play(1);
        var mart = s.View.VendorCards().Single(v => v.Id == "islandMart").Sells.Single();
        Assert.Equal(0, mart.LeadTimeDays);
        Assert.Equal("Counted from the signed order, which comes after the buy is written; the quote gives the first day it can start.", mart.NoticeSays);
        var fresh = Session.Create(Repo.Load("tutorial"), 1);
        Assert.All(fresh.View.VendorCards().SelectMany(v => v.Sells).Where(o => o.LeadTimeDays is null), o => Assert.Null(o.NoticeSays));
    }

    [Fact]
    public void The_dining_hall_news_says_what_comes_next()
    {
        var s = Play();
        var news = s.Records.Single(r => r.Kind == RecordKind.Event && r.Subject == "diningSlips");
        Assert.EndsWith("they come to your desk as a decision on what to do with the vendor.", news.Text);
    }
}
