using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>
/// D39, DESK-111 (Claude play 19 Nov 2026, notes 16 and 17): what the shop has been told reaches the advice. On the
/// morning of day 5 SMSgt Dunn had said the forklifts need drivers and the news said Harbor Equipment is short of
/// forklifts this week, and the order-by line still said "a BPA call with Harbor Equipment Co"; Harbor's card still
/// showed five forklifts uncommitted with nothing about the shortage.
/// </summary>
public class HeardTests
{
    private static InputLog Play()
        => InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-19", "input-log.json"));

    private static Session Morning(int day) => Replay.Run(Repo.Load("tutorial"), Play(), stopAfterDay: day - 1);

    private static OrderByView? ForkliftOrderBy(Session s)
        => Assert.Single(s.View.WhereThingsStand(), e => e.CardId == "tutForklift").OrderBy;

    private const string Shortage = "Heard on day 4: Harbor Equipment Co is short of forklifts this week.";

    [Fact]
    public void The_order_by_line_names_a_route_that_brings_what_Dunn_said_and_not_the_vendor_in_the_news()
    {
        var s = Morning(5);
        Assert.Equal(5, s.View.Day);
        var ob = ForkliftOrderBy(s);
        Assert.NotNull(ob);
        Assert.Equal("coastalRentals", ob!.VendorId);
        Assert.DoesNotContain("Harbor", ob.Sentence, StringComparison.Ordinal);
    }

    [Fact]
    public void A_part_someone_said_is_needed_counts_before_the_player_adds_it()
    {
        // On the morning of day 5 the request does not yet carry operators (the player adds them later that day); the
        // route the line names brings them.
        var s = Morning(5);
        var card = Assert.Single(s.View.WhereThingsStand(), e => e.CardId == "tutForklift");
        var editor = s.View.ActionEditor("hubSite", "mhe", "tutForklift")!;
        Assert.DoesNotContain("operator", editor.SelectedLines);
        var named = Assert.Single(editor.Vendors, v => v.Id == card.OrderBy!.VendorId);
        Assert.Contains("operator", named.Lines);
    }

    [Fact]
    public void Before_the_news_the_line_still_names_the_call()
    {
        // The morning of day 4: Dunn has not answered and Harbor has said nothing.
        var ob = ForkliftOrderBy(Morning(4));
        Assert.NotNull(ob);
        Assert.Equal("harborEquipment", ob!.VendorId);
        Assert.Equal("bpaCall", ob.InstrumentId);
    }

    [Fact]
    public void The_vendor_card_and_the_editor_say_what_was_heard_while_it_holds()
    {
        var s = Morning(5);
        var harbor = Assert.Single(s.View.VendorCards(), c => c.Id == "harborEquipment");
        Assert.Equal(Shortage, harbor.Heard);
        Assert.All(s.View.VendorCards().Where(c => c.Id != "harborEquipment"), c => Assert.Null(c.Heard));
        var option = Assert.Single(s.View.ActionEditor("hubSite", "mhe", "tutForklift")!.Vendors, v => v.Id == "harborEquipment");
        Assert.Equal(Shortage, option.Heard);

        Assert.Null(Assert.Single(Morning(4).View.VendorCards(), c => c.Id == "harborEquipment").Heard);
        // The effect runs days 4 to 8; on the morning of day 9 it is over and the card says nothing.
        Assert.Equal(Shortage, Assert.Single(Morning(8).View.VendorCards(), c => c.Id == "harborEquipment").Heard);
        Assert.Null(Assert.Single(Morning(9).View.VendorCards(), c => c.Id == "harborEquipment").Heard);
    }
}
