using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-166 (Claude play 21 Nov, fifth play, note 13): a vendor whose terms are already known says what meeting them
/// brings, true to CompleteMeetVendor: nothing new on the card, and the vendor raises any contract of theirs the office left
/// unfinished. The words are the same for every vendor, so they never hint at a contract the handover left out.</summary>
public class MeetingBringsTests
{
    private static readonly string[] Prep = ["gpcAccounts", "cashAndPayingAgent", "systemAccess"];
    private static readonly string[] Scenarios = ["tutorial", "spoke-b", "spoke-b-90", "sustainment"];

    private static Session Start(string sc) => sc == "tutorial" ? Session.Create(Repo.Load(sc), 1, Prep) : Session.Create(Repo.Load(sc), 1);

    private static string Expected(VendorCard c)
        => $"Their terms are on file already, so meeting {c.Rep!.DisplayName} adds nothing to their card; a vendor you meet does raise any contract of theirs this office has left unfinished.";

    [Fact]
    public void Only_a_vendor_with_nothing_left_to_learn_and_someone_to_meet_says_it()
    {
        var said = 0;
        foreach (var sc in Scenarios)
            foreach (var c in Start(sc).View.VendorCards())
            {
                if (c.StillToLearn is null && c.Rep is not null && c.MetDay is null) { Assert.Equal(Expected(c), c.MeetingBrings); said++; }
                else Assert.Null(c.MeetingBrings);
            }
        Assert.True(said > 0);
    }

    [Fact]
    public void Meeting_a_vendor_whose_terms_are_known_changes_nothing_on_the_card_and_then_says_nothing()
    {
        foreach (var sc in Scenarios)
        {
            var s = Start(sc);
            foreach (var before in s.View.VendorCards().Where(c => c.MeetingBrings is not null).ToList())
            {
                if (!s.Apply(new MeetVendorAction { Vendor = before.Id }).Accepted) continue;
                var day = s.Day;
                while (s.View.VendorCards().Single(c => c.Id == before.Id).MetDay is null && s.Day < day + 5) s.AdvanceDay();
                var after = s.View.VendorCards().Single(c => c.Id == before.Id);
                Assert.NotNull(after.MetDay);
                Assert.Null(after.MeetingBrings);
                Assert.Equal(before.Sells, after.Sells);
                Assert.Equal((before.AcceptsCash, before.AcceptsGpc), (after.AcceptsCash, after.AcceptsGpc));
                Assert.Equal(before.IslandsReached, after.IslandsReached);
                return;
            }
        }
        Assert.Fail("no vendor with known terms could be met");
    }

    [Fact]
    public void The_fifth_plays_harbor_equipment_card_says_what_meeting_Tomas_Reyna_brings()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, ["cashAndPayingAgent", "systemAccess", "pullVehicles", "vendorLists", "templatesAndKit"]);
        var c = s.View.VendorCards().Single(x => x.Id == "harborEquipment");
        Assert.Null(c.StillToLearn);
        Assert.Equal("Their terms are on file already, so meeting Mr. Tomas Reyna adds nothing to their card; a vendor you meet does raise any contract of theirs this office has left unfinished.", c.MeetingBrings);
    }
}
