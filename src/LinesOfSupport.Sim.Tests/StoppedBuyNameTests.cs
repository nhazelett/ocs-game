using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

/// <summary>
/// DESK-117 (Claude play 20 Nov 2026, note 20). The player called Harbor Equipment for forklifts on day 4
/// (player-award-2), bought Coastal Rentals on the card on day 5 (player-award-3) and stopped the call before it was
/// signed. The buy form named the next buy by skipping ids in an arrangement or in work still in progress, so it reused
/// player-award-2, which the award check refuses because the stopped call's work still carries it. Every buy for the
/// rest of the run was refused with "Harbor Equipment Co material handling equipment at Hub already exists".
/// </summary>
public class StoppedBuyNameTests
{
    private static InputLog Play()
        => InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-20", "input-log.json"));

    /// <summary>The morning of day 8, where the player stopped: the waste order is due today.</summary>
    private static Session Day8() => Replay.Run(Repo.Load("tutorial"), Play(), stopAfterDay: 7);

    private static AwardAction Waste(Session s, string id) => new()
    {
        Day = s.Day, Id = id, Card = "tutWaste", Site = "hubSite", Capability = "waste",
        Instrument = "purchaseOrder", Vendor = "sanitation", FundingLine = "om1",
        Quantity = 3600, StartDay = 13, DurationDays = 8,
    };

    [Fact]
    public void A_stopped_buy_keeps_its_id_taken_though_it_is_no_longer_in_work()
    {
        var s = Day8();
        var stopped = Assert.Single(s.Work, w => w.SubjectId == "player-award-2");
        Assert.NotNull(stopped.StoppedDay);

        Assert.False(s.View.InWork("player-award-2"));
        Assert.True(s.View.AwardIdTaken("player-award-2"));
        Assert.True(s.View.AwardIdTaken("player-award-1"));   // Island Mart's water, an arrangement
        Assert.True(s.View.AwardIdTaken("player-award-3"));   // Coastal Rentals' forklifts, an arrangement
        Assert.False(s.View.AwardIdTaken("player-award-4"));
    }

    [Fact]
    public void The_award_check_and_the_view_agree_on_every_id()
    {
        var s = Day8();
        for (var n = 1; n <= 5; n++)
        {
            var id = $"player-award-{n}";
            var refusedAsTaken = s.View.Preview(Waste(s, id)).HardStops.Any(x => x.EndsWith("already exists", StringComparison.Ordinal));
            Assert.Equal(s.View.AwardIdTaken(id), refusedAsTaken);
        }
    }

    [Fact]
    public void The_waste_order_under_the_first_free_id_goes_through()
    {
        var s = Day8();
        // The buy form's picker as DESK-118 makes it: skip every taken id.
        var n = 1;
        while (s.View.AwardIdTaken($"player-award-{n}")) n++;
        Assert.Equal(4, n);

        var order = Waste(s, $"player-award-{n}");
        var preview = s.View.Preview(order);
        Assert.True(preview.Accepted, string.Join(" | ", preview.HardStops));
        Assert.True(s.Apply(order).Accepted);
        Assert.True(s.View.AwardIdTaken("player-award-4"));

        // Under the id the old picker chose, the same order is refused, with the sentence the player saw.
        var reused = s.View.Preview(Waste(s, "player-award-2"));
        Assert.False(reused.Accepted);
        Assert.Contains("Harbor Equipment Co material handling equipment at Hub already exists", reused.HardStops);
    }
}
