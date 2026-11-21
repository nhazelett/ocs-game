using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

/// <summary>
/// DESK-119 (D40; Claude play 20 Nov 2026, notes 16 and 17). On the morning of day 5, after SMSgt Dunn said the rental
/// needs drivers, the summary above Arrange support for Coastal Rentals read "It is on top of the BPA call being written
/// with Harbor Equipment Co. Every day it would run is already covered." in the warning colour: it called the vendor
/// that brings drivers a double buy of the call without them, and said nothing of the drivers.
/// </summary>
public class ToldNotAddedTests
{
    private static InputLog Play()
        => InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-20", "input-log.json"));

    private static Session Morning(int day) => Replay.Run(Repo.Load("tutorial"), Play(), stopAfterDay: day - 1);

    private static AwardAction Coastal(Session s, string instrument) => new()
    {
        Day = s.Day, Id = "check", Card = "tutForklift", Site = "hubSite", Capability = "mhe", Instrument = instrument,
        Vendor = "coastalRentals", FundingLine = instrument == "gpc" ? null : "om1", Quantity = 3, StartDay = 8, DurationDays = 13,
        Priorities = instrument == "gpc" ? null : ["fast", "good"],
    };

    private const string Told = "It leaves out operators, which SMSgt Ray Dunn said the request needs: add what you learned first.";

    [Theory]
    [InlineData("gpc")]
    [InlineData("purchaseOrder")]
    public void Before_the_part_is_added_the_summary_says_who_said_it_is_needed(string instrument)
    {
        var s = Morning(5);
        var summary = s.View.AwardSummary(Coastal(s, instrument))!;
        Assert.EndsWith(Told, summary.Sentence);
        Assert.True(summary.Attention);
        Assert.Equal(Told, summary.Why);
        Assert.DoesNotContain("already covered", summary.Sentence, StringComparison.Ordinal);
    }

    [Fact]
    public void Once_the_part_is_in_the_request_the_sentence_goes()
    {
        var s = Morning(5);
        Assert.True(s.Apply(new AddLineAction { Day = 5, Card = "tutForklift", Line = "operator" }).Accepted);
        var summary = s.View.AwardSummary(Coastal(s, "gpc"))!;
        Assert.DoesNotContain("leaves out operators", summary.Sentence, StringComparison.Ordinal);
        Assert.Contains("Coastal Rentals brings forklifts, operators, and maintenance.", s.View.Preview(Coastal(s, "gpc")).SoftGaps);   // D46
        Assert.Null(summary.Why);
    }

    [Fact]
    public void Before_anyone_has_said_so_there_is_no_sentence()
    {
        // The morning of day 4: Dunn has been asked by nobody yet.
        var s = Morning(4);
        var summary = s.View.AwardSummary(Coastal(s, "gpc"))!;
        Assert.DoesNotContain("said the request needs", summary.Sentence, StringComparison.Ordinal);
        // D48 (DESK-172): what it leaves out is named while Dunn has not answered, without saying what he would say.
        Assert.EndsWith("It leaves out operators; SMSgt Ray Dunn has not been asked.", summary.Sentence, StringComparison.Ordinal);
    }
}
