using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-185 (Claude play 21 Nov, ninth play, note P2-21). On the morning of day 5 of the second log the office
/// vehicle bought on day 3 started, and the day's plan said "No deliveries or support starts or ends are due today". The
/// plan now names the office's own support as it does a site's, for the contracting office.</summary>
public class ShopSupportPlanTests
{
    private static InputLog SecondLog()
        => InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-21-9", "input-log-2.json"));

    [Fact]
    public void The_day_5_plan_says_the_office_vehicle_starts()
    {
        var s = Replay.Run(Repo.Load("tutorial"), SecondLog(), stopAfterDay: 4);
        Assert.Equal(5, s.Day);
        var plan = s.View.DayPlan();
        Assert.Contains("Coastal Rentals should start the vehicle for the contracting office today.", plan.Due);
        Assert.DoesNotContain(plan.Due, d => d.StartsWith("No deliveries", StringComparison.Ordinal));
        Assert.DoesNotContain(plan.Due, d => d.Contains("at Hub today", StringComparison.Ordinal) && d.Contains("vehicle", StringComparison.Ordinal));
    }

    [Fact]
    public void The_plan_says_the_office_vehicle_ends_on_its_last_day()
    {
        var s = Replay.Run(Repo.Load("tutorial"), SecondLog(), stopAfterDay: 4);
        Assert.True(s.Apply(new AwardAction
        {
            Id = "short-vehicle", Instrument = "gpc", Vendor = "coastalRentals", Site = "hubSite", Capability = "shopVehicle",
            Quantity = 1, StartDay = 6, DurationDays = 2, Day = 5,
        }).Accepted);
        var said = new List<string>();
        while (!s.Finished && s.Day <= 12) { said.AddRange(s.View.DayPlan().Due); s.AdvanceDay(); }
        Assert.Contains("Today is the last day of the vehicle for the contracting office from Coastal Rentals.", said);
        Assert.DoesNotContain(said, d => d.Contains("contracting office at Hub", StringComparison.Ordinal));
    }
}
