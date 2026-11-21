using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-195 (Claude play 21 Nov, eleventh play, note P2-11). The second player's own-initiative BPA call with
/// Harbor had "Add to this contract" greyed with "The request does not ask for operators or maintenance." on days 4 and 5,
/// though it had no request and the forklift request on the desk asked for maintenance (and operators, once added). It is
/// now changed by the request on the desk for its site and capability.</summary>
public class OwnInitiativeModifyTests
{
    private static InputLog SecondLog()
        => InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-21-11", "input-log-2.json"));

    [Fact]
    public void Harbors_own_initiative_call_takes_the_maintenance_the_request_on_the_desk_asks_for()
    {
        var s = Replay.Run(Repo.Load("tutorial"), SecondLog(), stopAfterDay: 3);
        Assert.Equal(4, s.Day);
        var m = s.View.ModifyOptions("player-award-2")!;
        Assert.True(m.Can);
        Assert.Equal(["maintenance"], m.Parts);
    }

    [Fact]
    public void With_no_request_on_the_desk_it_says_so()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1);
        Assert.True(s.Apply(new AwardAction
        {
            Id = "early", Instrument = "purchaseOrder", Vendor = "harborEquipment", Site = "hubSite", Capability = "mhe",
            Quantity = 1, StartDay = 7, DurationDays = 1, FundingLine = "om1",
        }).Accepted);
        while (s.Arrangements.All(a => a.Id != "early") && s.Day < 7) s.AdvanceDay();
        Assert.True(s.Day <= 7);
        var m = s.View.ModifyOptions("early")!;
        Assert.False(m.Can);
        Assert.Equal("Nothing on your desk asks for operators or maintenance at Hub.", m.Why);
    }
}
