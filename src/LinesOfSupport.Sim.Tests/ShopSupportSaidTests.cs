using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-189 (Claude play 21 Nov, tenth play, notes P1-10, P2-9). The buy summary for the office vehicle said
/// "vehicle for the contracting office for Hub", and the day's results never said it started or ended. The quote leaves out
/// the site, and the day's results list the office's own support as the plan does (DESK-185).</summary>
public class ShopSupportSaidTests
{
    private static InputLog FirstLog()
        => InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-21-10", "input-log-1.json"));

    private static AwardAction Vehicle(int day, int start, int days) => new()
    {
        Id = "vehicle", Instrument = "gpc", Vendor = "coastalRentals", Site = "hubSite", Capability = "shopVehicle",
        Quantity = 1, StartDay = start, DurationDays = days, Day = day,
    };

    [Fact]
    public void The_day_3_quote_leaves_out_the_site()
    {
        var s = Replay.Run(Repo.Load("tutorial"), FirstLog(), stopAfterDay: 2);
        Assert.Equal(3, s.Day);
        var summary = s.View.AwardSummary(Vehicle(3, 3, 12))!;
        Assert.StartsWith("Coastal Rentals, vehicle for the contracting office: $", summary.Sentence);
        Assert.DoesNotContain("for Hub", summary.Sentence);
    }

    [Fact]
    public void The_day_5_results_say_the_office_vehicle_started()
    {
        var s = Replay.Run(Repo.Load("tutorial"), FirstLog(), stopAfterDay: 5);
        Assert.Equal(6, s.Day);
        var summary = s.View.DaySummary()!;
        Assert.Equal(5, summary.Day);
        Assert.Contains("Coastal Rentals started the vehicle for the contracting office.", summary.Started);
        Assert.DoesNotContain(summary.Started, x => x.Contains("contracting office at Hub", StringComparison.Ordinal));
    }

    [Fact]
    public void The_results_say_the_office_vehicle_ended()
    {
        var s = Replay.Run(Repo.Load("tutorial"), FirstLog(), stopAfterDay: 4);
        Assert.True(s.Apply(Vehicle(5, 6, 2) with { Id = "short-vehicle" }).Accepted);
        var ended = new List<string>();
        while (!s.Finished && s.Day <= 12) { s.AdvanceDay(); if (s.View.DaySummary() is { } d) ended.AddRange(d.Ended); }
        Assert.Contains(ended, x => x.StartsWith("The vehicle for the contracting office from Coastal Rentals ended", StringComparison.Ordinal));
    }
}
