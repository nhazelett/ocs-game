using System.Text.Json.Nodes;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;

namespace LinesOfSupport.Sim.Tests;

/// <summary>
/// DESK-08 (D4): staff meetings that sometimes carry nothing for contracting and sometimes put a requirement on the
/// desk. A per-meeting draw decides whether there is anything for the shop; a requirement tagged heardAtStaffMeeting is
/// raised in the room whatever the draw, and a shop that is not there gets it later.
/// </summary>
public class StaffMeetingTests
{
    private static ScenarioBundle Bundle(string id, Action<JsonNode>? scenario = null, Action<JsonNode>? meetingRules = null)
    {
        var dir = Path.Combine(Path.GetTempPath(), $"los-meeting-{Guid.NewGuid():N}");
        var scenarioDir = Path.Combine(dir, id);
        var rulesDir = Path.Combine(dir, "rules");
        Directory.CreateDirectory(scenarioDir);
        Directory.CreateDirectory(rulesDir);
        var sc = JsonNode.Parse(File.ReadAllText(Path.Combine(Repo.Scenario(id), "scenario.json")))!;
        scenario?.Invoke(sc);
        File.WriteAllText(Path.Combine(scenarioDir, "scenario.json"), sc.ToJsonString());
        foreach (var f in Directory.GetFiles(Path.Combine(Repo.Root, "scenarios", "rules"), "*.json"))
        {
            var rules = JsonNode.Parse(File.ReadAllText(f))!;
            if (meetingRules is not null) meetingRules(rules["goingToPeople"]!["staffMeeting"]!);
            File.WriteAllText(Path.Combine(rulesDir, Path.GetFileName(f)), rules.ToJsonString());
        }
        return ScenarioBundle.Load(scenarioDir, rulesDir);
    }

    private static List<CauseRecord> Meetings(Session s)
        => s.Records.Where(r => r.Kind == RecordKind.Visit && r.Subject == $"person {s.Scenario.Actors.Commander.Person}").ToList();

    private static void To(Session s, int day)
    {
        while (s.Day < day && !s.Finished) s.AdvanceDay();
    }

    [Fact]
    public void A_meeting_with_nothing_for_contracting_is_ops_talk()
    {
        var s = Session.Create(Bundle("spoke-b", meetingRules: m => m["contractingPermille"] = 0), 1);
        s.Apply(new JoinStaffMeetingAction());
        To(s, 6);
        var talk = s.Rules.GoingToPeople.StaffMeeting.OpsTalk;
        Assert.NotEmpty(talk);
        foreach (var m in Meetings(s))
        {
            Assert.Contains(talk, line => m.Text.EndsWith(line, StringComparison.Ordinal));
            Assert.DoesNotContain("will be asking for", m.Text);
        }
        Assert.Empty(s.View.StaffMeeting.PreviewedCards);
        // Never the same ops talk two meetings running.
        var m2 = Meetings(s);
        for (var i = 1; i < m2.Count; i++) Assert.NotEqual(m2[i - 1].Text.Split("ran it. ")[^1], m2[i].Text.Split("ran it. ")[^1]);
    }

    [Fact]
    public void A_meeting_with_something_for_contracting_mentions_requests_before_they_arrive()
    {
        var s = Session.Create(Bundle("spoke-b", meetingRules: m => m["contractingPermille"] = 1000), 1);
        s.Apply(new JoinStaffMeetingAction());
        s.AdvanceDay();
        // MSgt Brandt's food service card is submitted on day 3; the meeting on day 1 hears about it first.
        Assert.Contains("MSgt Lena Brandt, Spoke C camp, said they will be asking for food service for Spoke C, wanted from day 12.", Meetings(s)[0].Text);
        Assert.Contains("req0014", s.View.StaffMeeting.PreviewedCards);
        s.AdvanceDay();
        // Heard once; the next day's meeting does not repeat it.
        Assert.DoesNotContain("Brandt", Meetings(s)[1].Text);
    }

    [Fact]
    public void Whether_the_first_meeting_has_anything_for_the_shop_is_a_draw()
    {
        var heard = 0;
        for (var seed = 1; seed <= 20; seed++)
        {
            var s = Session.Create(Repo.Load("spoke-b"), seed);
            s.Apply(new JoinStaffMeetingAction());
            s.AdvanceDay();
            var first = Meetings(s).Single();
            Assert.Contains("If we run short", first.Text);
            if (first.Text.Contains("will be asking for", StringComparison.Ordinal)) heard++;
            else Assert.Contains(s.Rules.GoingToPeople.StaffMeeting.OpsTalk, line => first.Text.EndsWith(line, StringComparison.Ordinal));
            // The priorities are said once, at the first meeting.
            s.AdvanceDay();
            Assert.DoesNotContain("If we run short", Meetings(s)[1].Text);
        }
        Assert.InRange(heard, 1, 19);
    }

    [Fact]
    public void In_the_room_the_requirement_is_on_the_desk_the_next_morning()
    {
        // The draw does not matter: a tagged requirement is raised even at a meeting with nothing else for the shop.
        var s = Session.Create(Bundle("tutorial", meetingRules: m => m["contractingPermille"] = 0), 1);
        s.Apply(new JoinStaffMeetingAction());
        To(s, 6);
        Assert.Null(s.View.RequirementEditor("tutWaste"));
        s.AdvanceDay();
        var raised = Meetings(s).Last();
        Assert.Equal(6, raised.Day);
        Assert.Contains("Col Helen Ward, Hub commander, ran it. Then the commander turned to the shop: \"The mayor's office tells me", raised.Text);
        Assert.Contains("That is waste removal for Hub, wanted from day 13. Capt Ana Ruiz, 3 CES, will bring it to you tomorrow.", raised.Text);
        var card = s.View.RequirementEditor("tutWaste");
        Assert.NotNull(card);
        Assert.Equal(7, card.SubmittedDay);
        Assert.Equal("Col Helen Ward, Hub commander, raised it at the staff meeting on day 6, and the shop was in the room.", card.HeardAt);
        // Raised once.
        s.AdvanceDay();
        Assert.DoesNotContain("turned to the shop", Meetings(s).Last().Text);
    }

    [Fact]
    public void Out_of_the_room_it_arrives_later_and_the_card_and_the_debrief_say_why()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1);
        To(s, 8);
        Assert.Null(s.View.RequirementEditor("tutWaste"));
        s.AdvanceDay();
        var card = s.View.RequirementEditor("tutWaste")!;
        Assert.Equal(9, card.SubmittedDay);
        Assert.Equal("Col Helen Ward, Hub commander, raised it at the staff meeting on day 6. The shop was not in the room, so it reached you on day 9.", card.HeardAt);
        To(s, 21);
        Assert.Contains(s.View.Debrief().Gaps.SelectMany(g => g.Factors),
            f => f.Text.Contains("It was raised at the commander's staff meeting on day 6, when the shop was not in the room."));
    }

    [Fact]
    public void Joining_after_it_first_came_up_still_hears_it_before_it_would_arrive()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1);
        To(s, 7);
        s.Apply(new JoinStaffMeetingAction());
        s.AdvanceDay();
        Assert.Contains("turned to the shop", Meetings(s).Single().Text);
        Assert.Equal(8, s.View.RequirementEditor("tutWaste")!.SubmittedDay);
    }

    [Fact]
    public void The_teammate_who_got_the_shop_in_goes_to_every_meeting()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1);
        Assert.True(s.Apply(new JoinStaffMeetingAction { Assignee = "novak" }).Accepted);
        To(s, 4);
        var m = Meetings(s);
        Assert.StartsWith("SrA Eli Novak got the shop into the commander's staff meeting.", m[0].Text);
        Assert.StartsWith("Staff meeting, with SrA Eli Novak there for the shop.", m[1].Text);
        Assert.All(s.Work.Where(w => w.Label == "Staff meeting"), w => Assert.Equal("novak", w.Assignee));
    }

    [Fact]
    public void A_game_that_never_attends_replays_as_before()
    {
        string Hash(ScenarioBundle b)
        {
            var s = Session.Create(b, 4);
            while (!s.Finished) s.AdvanceDay();
            return s.StateHash();
        }
        // Changing the meeting's draw odds changes nothing for a shop that is never in the room.
        Assert.Equal(Hash(Repo.Load("spoke-b")), Hash(Bundle("spoke-b", meetingRules: m => m["contractingPermille"] = 0)));
    }

    [Theory]
    [InlineData(9)]
    [InlineData(0)]
    public void The_hearing_day_must_come_before_the_card_would_arrive(int fromDay)
    {
        var ex = Assert.ThrowsAny<Exception>(() => Bundle("tutorial", sc =>
        {
            var card = sc["requirementCards"]!.AsArray().First(c => (string?)c!["id"] == "tutWaste")!;
            card["heardAtStaffMeeting"]!["fromDay"] = fromDay;
        }));
        Assert.Contains("heardAtStaffMeeting.fromDay", ex.Message);
    }
}
