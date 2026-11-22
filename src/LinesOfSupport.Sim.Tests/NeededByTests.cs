using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-208 (Claude play 22 Nov, notes P1-5, P2-9). Both players saw food service go to half with water on day 3;
/// nothing had said food service needs water. A request's dependency checklist now names what at the site needs it.</summary>
public class NeededByTests
{
    [Fact]
    public void The_tutorial_water_request_names_food_service()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1);
        var water = s.View.RequirementEditor("tutWater")!;
        Assert.Equal("Food service at Hub needs water: when water runs short, food service runs short with it.", water.NeededBy);
    }

    [Fact]
    public void Other_requests_name_their_own_site_or_nothing()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1);
        var waste = s.Cards.Single(c => c.Id == "tutWaste");
        while (s.Day < waste.SubmittedDay) s.AdvanceDay();
        // Nothing at Hub depends on material handling or waste removal.
        Assert.Null(s.View.RequirementEditor("tutForklift")!.NeededBy);
        Assert.Null(s.View.RequirementEditor("tutWaste")!.NeededBy);

        // Spoke B: the power request names lodging there; Spoke C's water request names food service there.
        var b = Session.Create(Repo.Load("spoke-b"), 1);
        var power = b.Cards.Single(c => c.CapabilityId == "power");
        var water = b.Cards.Single(c => c.CapabilityId == "water");
        while (b.Day < Math.Max(power.SubmittedDay, water.SubmittedDay)) b.AdvanceDay();
        Assert.Equal("Lodging at Spoke B needs power: when power runs short, lodging runs short with it.", b.View.RequirementEditor(power.Id)!.NeededBy);
        Assert.Equal("Food service at Spoke C needs water: when water runs short, food service runs short with it.", b.View.RequirementEditor(water.Id)!.NeededBy);
    }
}
