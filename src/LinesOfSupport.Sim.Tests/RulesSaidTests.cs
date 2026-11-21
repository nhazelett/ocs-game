using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-155 (Claude play 21 Nov, third play, notes 14, 21, 28): each rule is said where it applies, before it
/// matters. (a), the debrief's lag clause, is in FourSentencesTests.</summary>
public class RulesSaidTests
{
    private static readonly string[] Prep = ["gpcAccounts", "cashAndPayingAgent", "systemAccess"];

    [Fact]
    public void B_joining_the_staff_meeting_says_its_hour_every_day()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        Assert.Equal("Joins the staff meeting so requests reach you in the room; whoever goes then spends 1 hour at each meeting, every day.",
                     s.View.StaffMeeting.JoinSays);
    }

    [Fact]
    public void C_the_commander_says_once_what_counts_as_out()
    {
        const string said = "A day counts as out when less than half of it is supplied; at half or more it is short.";
        var s = Session.Create(Repo.Load("tutorial"), 1, Prep);
        var spoken = s.View.Bar().Spoken;
        Assert.Contains(said + " That's for the 16 days you're here.", spoken);
        Assert.Equal(spoken.IndexOf(said, StringComparison.Ordinal), spoken.LastIndexOf(said, StringComparison.Ordinal));
    }

    [Fact]
    public void D_before_a_pick_the_trade_names_all_three_pairs_with_each_picks_numbers()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1);
        var po = new AwardAction
        {
            Id = "po", Instrument = "purchaseOrder", Vendor = "vendorA", Site = "spokeBSite", Capability = "power",
            Lines = ["generation", "maintenance"], Quantity = 60, DurationDays = 10, FundingLine = "om1",
        };
        var none = s.View.Preview(po).Trade!;
        var paid = s.View.Preview(po with { Priorities = ["fast", "good"] }).Trade!;
        var premium = paid[(paid.IndexOf("about $", StringComparison.Ordinal) + 6)..].Split(' ')[0];
        Assert.Equal("Pick two of fast, cheap and good, and the third pays: cheap and good competes it among vendors, adding about 2 days of shop work; "
                     + $"fast and good pays a 15% premium, about {premium} of the price; "
                     + "fast and cheap takes the first quote, and the vendor is more likely to fall down on the job later.", none);
        Assert.Null(s.View.Preview(po with { Instrument = "sf44", Vendor = "vendorA" }).Trade);
    }
}
