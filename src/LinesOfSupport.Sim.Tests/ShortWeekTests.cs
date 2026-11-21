using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-174 (Claude play 21 Nov, sixth play, note 22). Harbor declined the sixth play's BPA call as short of
/// forklifts this week, then the buy form offered the same forklifts on the card for day 8. The tutorial's shortage is now
/// the scenario effect vendor.short: the call is still declined at award, and any other buy with Harbor that would start on
/// those days is refused with the shortage.</summary>
public class ShortWeekTests
{
    private static Session SixthPlayDay6()
        => Replay.Run(Repo.Load("tutorial"), InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-21-6", "input-log.json")), stopAfterDay: 5);

    private static AwardAction Harbor(string instrument, int start) => new()
    {
        Id = "harbor", Instrument = instrument, Vendor = "harborEquipment", Card = "tutForklift", Quantity = 3, StartDay = start,
        DurationDays = 13, FundingLine = instrument == "gpc" ? null : "om1",
    };

    private const string Short = "Harbor Equipment Co has no material handling equipment to spare through day 8. Start it on day 9 or later, or buy from someone else.";

    [Theory]
    [InlineData("gpc")]
    [InlineData("purchaseOrder")]
    public void A_buy_with_Harbor_that_would_start_in_its_short_week_is_refused_with_the_shortage(string instrument)
    {
        var s = SixthPlayDay6();
        Assert.Equal(6, s.Day);
        var preview = s.View.Preview(Harbor(instrument, 8));
        Assert.False(preview.Accepted);
        Assert.Contains(Short, preview.HardStops);
        var applied = s.Apply(Harbor(instrument, 8));
        Assert.False(applied.Accepted);

        // From day 9 the shortage says nothing.
        Assert.DoesNotContain(Short, s.View.Preview(Harbor(instrument, 9)).HardStops);
    }

    [Fact]
    public void The_sixth_plays_call_is_still_declined_and_its_game_is_unchanged()
    {
        var s = Replay.Run(Repo.Load("tutorial"), InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-21-6", "input-log.json")));
        Assert.Contains(s.Records, r => r.Kind == RecordKind.Declined && r.Text.StartsWith("Harbor Equipment Co declined the BPA call", StringComparison.Ordinal));
        Assert.Contains(s.Arrangements, a => a.Id == "player-award-2" && a.Status == ArrangementStatus.Declined);
    }

    [Fact]
    public void Before_the_news_and_after_the_week_Harbor_sells_as_before()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, ["gpcAccounts", "cashAndPayingAgent", "systemAccess"]);
        while (s.Day < 3) s.AdvanceDay();
        Assert.DoesNotContain(Short, s.View.Preview(Harbor("gpc", 8)).HardStops);   // the shortage begins on day 4
        while (s.Day < 9) s.AdvanceDay();
        Assert.DoesNotContain(s.View.Preview(Harbor("gpc", 10)).HardStops, h => h.Contains("to spare", StringComparison.Ordinal));
    }

    // DESK-178 (Claude play 21 Nov, eighth play, note P2-10): on day 5 the second player saw the card refused with Harbor's
    // shortage while the BPA call's quote still promised day 8. Once the shop has heard, the call is refused as well.
    private const string CallRefused = "Harbor Equipment Co is short of material handling equipment through day 8 and is turning calls down. Call after day 8, or buy from someone else.";

    private static AwardAction HarborCall(int start) => new()
    {
        Id = "harborCall", Instrument = "bpaCall", Vendor = "harborEquipment", Vehicle = "equipBpa", Card = "tutForklift",
        Site = "hubSite", Capability = "mhe", Quantity = 3, StartDay = start, DurationDays = 13, FundingLine = "om1",
    };

    [Fact]
    public void After_the_news_a_call_to_Harbor_is_refused_with_the_shortage()
    {
        var s = Replay.Run(Repo.Load("tutorial"), InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-21-8", "input-log-2.json")), stopAfterDay: 4);
        Assert.Equal(5, s.Day);
        Assert.NotNull(s.View.VendorCards().Single(c => c.Id == "harborEquipment").Heard);
        var preview = s.View.Preview(HarborCall(8));
        Assert.False(preview.Accepted);
        Assert.Contains(CallRefused, preview.HardStops);
        Assert.False(s.Apply(HarborCall(8)).Accepted);
        // The card says the same shortage on the same day.
        Assert.Contains(Short, s.View.Preview(Harbor("gpc", 8)).HardStops);
    }

    [Fact]
    public void After_the_short_week_a_call_to_Harbor_goes_in_again()
    {
        var s = Replay.Run(Repo.Load("tutorial"), InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-21-8", "input-log-2.json")), stopAfterDay: 8);
        Assert.Equal(9, s.Day);
        Assert.DoesNotContain(s.View.Preview(HarborCall(10)).HardStops, h => h.Contains("turning calls down", StringComparison.Ordinal));
    }

    [Fact]
    public void A_call_made_before_the_news_goes_in_and_is_declined_at_award()
    {
        // The day-4 call of the eighth play's first player is quoted without the shortage (it stopped the call on day 5);
        // the sixth play's day-4 call is placed and declined at award, as the tutorial teaches.
        var first = Replay.Run(Repo.Load("tutorial"), InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-21-8", "input-log-1.json")), stopAfterDay: 3);
        Assert.Equal(4, first.Day);
        Assert.DoesNotContain(first.View.Preview(HarborCall(8)).HardStops, h => h.Contains("turning calls down", StringComparison.Ordinal));

        var sixth = Replay.Run(Repo.Load("tutorial"), InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-21-6", "input-log.json")));
        Assert.Contains(sixth.Records, r => r.Kind == RecordKind.Declined && r.Text.StartsWith("Harbor Equipment Co declined the BPA call", StringComparison.Ordinal));
    }
}
