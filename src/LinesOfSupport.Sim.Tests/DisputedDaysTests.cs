using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-205 (Claude play 22 Nov, notes P1-6, P2-10). On day 4 of the second log the Island Mart water page read
/// "cost of support received $1,390.50" after a day 3 that was disputed and half delivered, with no word on why the whole
/// day was paid. The money line now adds one sentence; no money moves.</summary>
public class DisputedDaysTests
{
    private static InputLog SecondLog()
        => InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-22", "input-log-2.json"));

    private static Arrangement Water(Session s) => s.Arrangements.Single(a => a.VendorId == "islandMart" && a.Instrument == "gpc");

    [Fact]
    public void Day_four_water_page_says_what_the_disputed_day_cost()
    {
        var s = Replay.Run(Repo.Load("tutorial"), SecondLog(), stopAfterDay: 3);
        Assert.Equal(4, s.Day);
        var water = Water(s);
        Assert.Equal("Day 3 was disputed and half arrived; the card had already paid for the whole day, and with nothing written there is no claim.",
                     s.View.DisputedDaysLine(water.Id));
        // No money moves: the page still reads $1,390.50 received, day 3 charged whole, as in the play.
        Assert.Equal(139050, water.IncurredCents);
        Assert.Equal(water.DailyCostCents, water.IncurredCents);
    }

    [Fact]
    public void Day_two_and_a_written_buy_have_no_line()
    {
        var early = Replay.Run(Repo.Load("tutorial"), SecondLog(), stopAfterDay: 2);
        Assert.Null(early.View.DisputedDaysLine(Water(early).Id));

        var s = Replay.Run(Repo.Load("tutorial"), SecondLog());
        foreach (var a in s.Arrangements.Where(a => a.Instrument is "purchaseOrder" or "bpaCall"))
            Assert.Null(s.View.DisputedDaysLine(a.Id));
        Assert.Null(s.View.DisputedDaysLine("no-such-buy"));
    }

    [Fact]
    public void First_log_forklifts_read_both_days()
    {
        // The first log's forklift card buy was disputed on days 12 and 15 (the DESK-204 gaps).
        var s = Replay.Run(Repo.Load("tutorial"), InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-22", "input-log-1.json")));
        var lines = s.Arrangements.Select(a => s.View.DisputedDaysLine(a.Id)).OfType<string>().ToList();
        Assert.Contains("Days 12 and 15 were disputed and half arrived each time; the card had already paid for both whole days, and with nothing written there is no claim.", lines);
    }

    [Fact]
    public void Several_disputed_days_read_together()
    {
        var s = Replay.Run(Repo.Load("tutorial"), SecondLog());
        foreach (var a in s.Arrangements)
        {
            var days = s.Records.Where(r => r.Kind == RecordKind.ExecutionConfusion && r.Subject == a.Id).Select(r => r.Day).Distinct().ToList();
            var line = s.View.DisputedDaysLine(a.Id);
            if (days.Count == 0) { Assert.Null(line); continue; }
            Assert.NotNull(line);
            Assert.EndsWith("and with nothing written there is no claim.", line);
            foreach (var d in days) Assert.Contains(d.ToString(), line);
            if (days.Count > 1) Assert.StartsWith("Days ", line);
        }
    }
}
