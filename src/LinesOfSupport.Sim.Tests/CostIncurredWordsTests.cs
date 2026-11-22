using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

public class CostIncurredWordsTests
{
    [Theory]
    [InlineData(2,false)]
    [InlineData(3,true)]
    [InlineData(10,true)]
    public void The_known_half_day_explains_cost_without_changing_the_charge(int after,bool explained)
    {
        var log=InputLog.Load(Path.Combine(Repo.Root,"docs","reviews","plays","2026-11-22-8","input-log-1.json"));
        var s=Replay.Run(Repo.Load("tutorial"),log,stopAfterDay:after);
        var water=s.Arrangements.Single(a=>a.CapabilityId=="water"&&a.Instrument=="gpc");
        var hash=s.StateHash();var incurred=water.IncurredCents;
        Assert.Equal(explained,s.View.DisputedDaysLine(water.Id) is not null);
        if(after==3)Assert.Equal(water.DailyCostCents,water.IncurredCents);
        if(after==10)Assert.Equal(water.ObligatedCents,water.IncurredCents);
        Assert.Null(s.View.DisputedDaysLine("not-an-arrangement"));
        Assert.Equal(incurred,water.IncurredCents);Assert.Equal(hash,s.StateHash());
    }
}
