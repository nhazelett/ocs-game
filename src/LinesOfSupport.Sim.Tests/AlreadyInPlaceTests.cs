using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-32 (docs/design/desk-redesign.md D15, D16): the quote says first what is already in place or being bought,
/// and a vendor-capacity refusal says what is left and what to do. Nick at the DESK gate tried to buy water from Harbor
/// Bottling a second time and could not remember whether he had already bought it.</summary>
public class AlreadyInPlaceTests
{
    private static readonly string[] Prep = ["gpcAccounts", "cashAndPayingAgent", "systemAccess"];

    private static AwardAction Water(string id, long quantity) => new()
    {
        Id = id, Instrument = "sf44", Vendor = "harborBottling", Card = "tutWater", Quantity = quantity, StartDay = 2,
        DurationDays = 7, FundingLine = "om1",
    };

    [Fact]
    public void Buying_the_water_twice_says_so_first_and_says_what_is_left()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        // The host nation's water point opens on day 9; the quote says so, and nothing from Harbor Bottling is in place yet.
        Assert.Equal("Main Island Utilities (host nation) is already set to supply water at Hub, 8000 gal/day from day 9 on; this would be on top of it.",
            s.View.Preview(Water("first", 4500)).SoftGaps[0]);
        Assert.True(s.Apply(Water("first", 4500)).Accepted);

        var again = s.View.Preview(Water("second", 4500));
        Assert.False(again.Accepted);
        Assert.Equal("Harbor Bottling is already set to supply water at Hub, 4500 gal/day from day 3 to day 9; this would be on top of it.", again.SoftGaps[0]);
        Assert.Equal("Harbor Bottling can supply 5000 gal/day and 4500 is already committed on those days, so 500 is left. Ask for 500 or less, or buy the rest from someone else.",
            Assert.Single(again.HardStops));
        Assert.True(s.View.Preview(Water("second", 500)).Accepted);
    }

    [Fact]
    public void A_vendor_with_nothing_left_says_to_buy_elsewhere()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        Assert.True(s.Apply(Water("first", 5000)).Accepted);
        Assert.Equal("Harbor Bottling can supply 5000 gal/day and all of it is already committed on those days. Buy from someone else, or pick days when they are free.",
            Assert.Single(s.View.Preview(Water("second", 100)).HardStops));
    }

    [Fact]
    public void Support_already_running_reads_through_its_last_day()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        Assert.True(s.Apply(Water("first", 4500)).Accepted);
        s.AdvanceDay();
        s.AdvanceDay();
        var more = s.View.Preview(Water("second", 500) with { Vendor = "islandMart", StartDay = 4, DurationDays = 5 });
        Assert.Equal("Harbor Bottling already supplies water at Hub, 4500 gal/day through day 9; this would be on top of it.", more.SoftGaps[0]);
    }

    [Fact]
    public void A_contract_still_being_written_names_who_is_writing_it()
    {
        var log = InputLog.Load(Path.Combine(Repo.Scenario("tutorial"), "golden", "approach1-taught.json"));
        var s = Replay.Run(Repo.Load("tutorial"), log, stopAfterDay: 3);
        foreach (var a in log.Actions.Where(a => a.Day == 4)) s.Apply(a);
        var preview = s.View.Preview(new AwardAction
        {
            Id = "more", Instrument = "purchaseOrder", Vendor = "coastalRentals", Card = "tutForklift", Lines = ["equipment", "operator", "maintenance"],
            Quantity = 3, StartDay = 8, DurationDays = 13, FundingLine = "om1",
        });
        Assert.Equal("MSgt Joan Avery is already writing the BPA call with Harbor Equipment Co for material handling equipment at Hub, 3 forklifts from day 8; this would be on top of it.",
            preview.SoftGaps[0]);
    }

    [Fact]
    public void Asking_for_a_quote_changes_nothing()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        Assert.True(s.Apply(Water("first", 4500)).Accepted);
        var hash = s.StateHash();
        _ = s.View.Preview(Water("second", 4500));
        Assert.Equal(hash, s.StateHash());
    }
}
