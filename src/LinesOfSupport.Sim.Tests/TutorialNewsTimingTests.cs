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
}
