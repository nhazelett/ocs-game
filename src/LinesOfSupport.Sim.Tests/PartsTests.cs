using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>
/// DESK-77 (docs/design/desk-redesign.md D34): a request can be met in parts. Checked on the 17 Nov review's Spoke B play,
/// where the player added operators and maintenance to the power request on day 2 and every cheap supplier went greyed.
/// </summary>
public class PartsTests
{
    private static InputLog Log()
        => InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-17-review", "spoke-b-input-log.json")) with { SavedDay = null };

    /// <summary>The review log through day 2 (operators and maintenance added to the power request), on the morning of day 3.</summary>
    private static Session Day3()
    {
        var log = Log();
        return Replay.Run(Repo.Load("spoke-b"), log with { Actions = [.. log.Actions.Where(a => a.Day <= 2)] }, stopAfterDay: 2);
    }

    private static AwardAction HarborFromTheRequest(int day) => new()
    {
        Id = "harborSets", Instrument = "bpaCall", Vendor = "vendorC", Vehicle = "bosBpa", Card = "req0007", Quantity = 468,
        StartDay = 7, DurationDays = 60, FundingLine = "om1", Day = day,
    };

    [Fact]
    public void A_vendor_who_brings_part_of_the_request_stays_selectable_and_says_what_it_leaves_out()
    {
        var s = Day3();
        var card = s.Cards.Single(c => c.Id == "req0007");
        Assert.True(card.Lines["operator"] && card.Lines["maintenance"]);
        var choices = s.View.AwardChoices("spokeBSite", "power", "req0007")!;
        Assert.True(choices.Vendors.Single(v => v.Id == "vendorC").Available);
        Assert.True(choices.Vendors.Single(v => v.Id == "vendorA").Available);

        var preview = s.View.Preview(HarborFromTheRequest(s.Day));
        Assert.True(preview.Accepted, string.Join(" ", preview.HardStops));
        Assert.Equal("Harbor Equipment Co brings power without operators; Capt Ana Ruiz said it is needed. The request stays partly covered until it is found.",
            preview.SoftGaps[0]);
    }

    [Fact]
    public void Bought_from_the_request_it_stands_as_partly_covered_and_names_what_is_still_to_find()
    {
        var s = Day3();
        Assert.True(s.Apply(HarborFromTheRequest(s.Day)).Accepted);
        var standing = s.View.WhereThingsStand().Single(r => r.CardId == "req0007");
        Assert.Equal(["operators"], standing.StillToFind);
        Assert.EndsWith("It leaves out operators, still to find.", standing.Sentence);
        Assert.Null(standing.OrderBy);

        while (s.Day <= 7) s.AdvanceDay();
        var arranged = s.Arrangements.Single(a => a.Id == "harborSets");
        Assert.DoesNotContain("operator", arranged.Lines);
        Assert.Contains("maintenance", arranged.Lines);
        Assert.Equal(["operators"], s.View.WhereThingsStand().Single(r => r.CardId == "req0007").StillToFind);
    }

    [Fact]
    public void Before_a_learned_part_is_added_the_request_says_who_could_still_meet_all_of_it()
    {
        var log = Log();
        var s = Replay.Run(Repo.Load("spoke-b"), log with { Actions = [.. log.Actions.Where(a => a.Day <= 1)] }, stopAfterDay: 1);
        var lines = s.View.RequirementEditor("req0007")!.Lines;
        var op = lines.Single(l => l.Id == "operator");
        Assert.True(op.CanAdd);
        Assert.Equal("With operators, only Meridian Site Services can meet all of it; others can still meet it in part.", op.IfAdded);
        Assert.Null(lines.Single(l => l.Id == "generation").IfAdded);
    }

    [Fact]
    public void An_own_initiative_buy_for_the_requests_site_counts_for_the_request()
    {
        // The review play bought Harbor's sets on its own initiative on day 3. On day 4 the lead said nothing covered the
        // power request and the request said no decisions were committed.
        var s = Replay.Run(Repo.Load("spoke-b"), Log(), stopAfterDay: 3);
        Assert.Equal(4, s.Day);
        var lead = s.View.MorningBoard().Lead;
        Assert.False(lead is not null && lead.LinkId == "req0007" && lead.Text.Contains("nothing covers", StringComparison.Ordinal),
            lead?.Text);
        Assert.Contains(s.View.Decisions("req0007"), d => d.Sentence.Contains("Harbor Equipment Co", StringComparison.Ordinal));
        Assert.NotEqual(RequestStanding.Nothing, s.View.WhereThingsStand().Single(r => r.CardId == "req0007").State);
    }

    [Fact]
    public void A_buy_that_lists_its_own_parts_is_checked_as_written()
    {
        var s = Day3();
        var listed = HarborFromTheRequest(s.Day) with { Lines = ["generation", "operator", "maintenance"] };
        var preview = s.View.Preview(listed);
        Assert.False(preview.Accepted);
        Assert.Contains(preview.HardStops, x => x.Contains("does not offer Operators", StringComparison.OrdinalIgnoreCase));
    }
}
