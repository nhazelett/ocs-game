using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

public class TutorialNewsTimingTests
{
    [Theory]
    [InlineData(1,.994)]
    [InlineData(2,.979)]
    public void The_first_notice_does_not_claim_a_second_day_that_has_not_happened(int player,double mission)
    {
        var log=InputLog.Load(Path.Combine(Repo.Root,"docs","reviews","plays","2026-11-22-8",$"input-log-{player}.json"));
        var bundle=Repo.Load("tutorial");
        var before=Replay.Run(bundle,log,stopAfterDay:10);
        Assert.DoesNotContain(before.Records,r=>r.Kind==RecordKind.Event&&r.Subject=="diningSlips");
        var first=Replay.Run(bundle,log,stopAfterDay:11);
        var news=first.Records.Single(r=>r.Kind==RecordKind.Event&&r.Subject=="diningSlips");
        Assert.Equal(11,news.Day);Assert.True(first.View.Known(news));
        Assert.StartsWith("Island Camp Services is short-staffed at the dining hall.",news.Text);
        Assert.DoesNotContain("two days running",news.Text);
        var final=Replay.Run(bundle,log);
        Assert.Equal(mission,final.Metrics["mission.output"],precision:6);
        foreach(var capability in new[]{"water","mhe","waste"})Assert.Equal(0,final.Metrics[$"site.hubSite.{capability}.downDays"]);
        Assert.Equal(2,final.Records.Count(r=>r.Kind==RecordKind.VendorFailure&&r.Subject=="hubDining"&&r.Day is 11 or 12));
    }

    [Theory]
    [InlineData(1, .994, 0)]
    [InlineData(2, .984, 1)]
    public void One_report_is_information_and_repeated_reported_misses_open_the_existing_decision(int player, double mission, int waterDaysOut)
    {
        var log = InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-22-9", $"input-log-{player}.json"));
        var bundle = Repo.Load("tutorial");
        var first = Replay.Run(bundle, log, stopAfterDay: 11);
        Assert.Equal(12, first.Day);
        var news = first.Records.Single(r => r.Kind == RecordKind.Event && r.Subject == "diningSlips");
        Assert.Equal(11, news.Day);
        Assert.EndsWith("One reported miss is information. Repeated reported misses can bring a decision on what to do with the vendor to your desk.", news.Text);
        Assert.Single(first.Records.Where(r => r.Kind == RecordKind.CorReport && r.Subject == "hubDining"));
        Assert.DoesNotContain(first.PerformanceIssues, i => i.ArrangementId == "hubDining");
        var second = Replay.Run(bundle, log, stopAfterDay: 12);
        Assert.Equal(13, second.Day);
        Assert.Equal(2, second.Records.Count(r => r.Kind == RecordKind.CorReport && r.Subject == "hubDining"));
        var issue = Assert.Single(second.PerformanceIssues.Where(i => i.ArrangementId == "hubDining"));
        Assert.Equal(12, issue.OpenedDay);
        Assert.Equal(new[] { 11, 12 }, issue.FailureDays);
        Assert.Null(issue.CureNoticeDay);
        var final = Replay.Run(bundle, log);
        Assert.Equal(mission, final.Metrics["mission.output"], precision: 6);
        Assert.Equal(waterDaysOut, final.Metrics["site.hubSite.water.downDays"]);
        Assert.Equal(0, final.Metrics["site.hubSite.mhe.downDays"]);
        Assert.Equal(0, final.Metrics["site.hubSite.waste.downDays"]);
        Assert.Equal(13, Assert.Single(final.PerformanceIssues.Where(i => i.ArrangementId == "hubDining")).CureNoticeDay);
    }

}
