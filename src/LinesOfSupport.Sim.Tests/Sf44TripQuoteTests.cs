using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-183 (Claude play 21 Nov, ninth play, notes P1-26, P1-15). On day 8 the first player's two-day SF 44 bridge
/// for waste removal quoted "$192 and no shop hours"; each delivery then took 2 of the player's hours, as the buy's record
/// said once it was made. The quote now counts the trips. On day 3 the meeting sentence on a vendor card read "a vendor you
/// meet does raise any contract", which does not parse.</summary>
public class Sf44TripQuoteTests
{
    private static InputLog FirstLog()
        => InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-21-9", "input-log-1.json"));

    private static AwardAction Bridge(InputLog log) => (AwardAction)log.Actions.Single(a => a.Day == 8 && a is AwardAction { Instrument: "sf44" });

    [Fact]
    public void The_day_8_bridge_quotes_the_trip_each_delivery_takes()
    {
        var log = FirstLog();
        var s = Replay.Run(Repo.Load("tutorial"), log, stopAfterDay: 7);
        Assert.Equal(8, s.Day);
        var summary = s.View.AwardSummary(Bridge(log))!;
        Assert.StartsWith("Main Island Sanitation, waste removal for Hub: $192 and 2 shop hours of your time on each delivery day.", summary.Sentence, StringComparison.Ordinal);
        Assert.DoesNotContain("no shop hours", summary.Sentence, StringComparison.Ordinal);
        // The record made by the buy says the same trip.
        Assert.True(s.Apply(Bridge(log)).Accepted);
        Assert.Contains(s.Records, r => r.Text.Contains("Each delivery is a trip for you and the paying agent: 2 shop hours.", StringComparison.Ordinal));
    }

    [Fact]
    public void The_trip_is_said_in_the_hours_of_whoever_makes_it()
    {
        var log = FirstLog();
        var s = Replay.Run(Repo.Load("tutorial"), log, stopAfterDay: 7);
        var summary = s.View.AwardSummary(Bridge(log) with { Assignee = "avery" })!;
        Assert.Matches(@": \$192 and [0-9.]+ shop hours? of MSgt Joan Avery's time on each delivery day\.", summary.Sentence);
    }

    [Fact]
    public void A_one_day_sf44_is_paid_on_the_spot_and_still_takes_no_shop_hours()
    {
        var log = FirstLog();
        var s = Replay.Run(Repo.Load("tutorial"), log, stopAfterDay: 7);
        var summary = s.View.AwardSummary(Bridge(log) with { DurationDays = 1 })!;
        Assert.Contains("$96 and no shop hours.", summary.Sentence, StringComparison.Ordinal);
    }

    [Fact]
    public void The_day_3_meeting_sentence_reads_plainly()
    {
        var s = Replay.Run(Repo.Load("tutorial"), FirstLog(), stopAfterDay: 2);
        Assert.Equal(3, s.Day);
        var said = s.View.VendorCards().Select(c => c.MeetingBrings).OfType<string>().ToList();
        Assert.NotEmpty(said);
        Assert.All(said, m => Assert.EndsWith("; a vendor you meet will bring up any contract of theirs this office has left unfinished.", m, StringComparison.Ordinal));
        Assert.DoesNotContain(said, m => m.Contains("does raise", StringComparison.Ordinal));
    }
}
