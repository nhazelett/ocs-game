using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>M2R-04: going to people (design doc 6.2 and 6.2a). Finance, the staff meeting, the SCO's staff, a vendor and
/// the J4 are actions with a shop-hour cost that return facts.</summary>
public class GoingToPeopleTests
{
    private static ScenarioBundle Modified(string id, Action<JsonNode> edit)
    {
        var root = JsonNode.Parse(File.ReadAllText(Path.Combine(Repo.Scenario(id), "scenario.json")))!;
        edit(root);
        var dir = Path.Combine(Path.GetTempPath(), $"los-people-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "scenario.json"), root.ToJsonString());
        return ScenarioBundle.Load(dir, Path.Combine(Repo.Root, "scenarios", "rules"));
    }

    private static List<CauseRecord> Visits(Session s) => s.Records.Where(r => r.Kind == RecordKind.Visit).ToList();

    private static void Days(Session s, int n)
    {
        for (var i = 0; i < n && !s.Finished; i++) s.AdvanceDay();
    }

    // Identifier-shaped scenario ids (a digit or an inner capital), as M2R-07 defines them; plain-word person ids are not.
    private static IEnumerable<string> IdShaped(Session s)
        => s.Scenario.Sites.Select(x => x.Id).Concat(s.Scenario.Vendors.Select(x => x.Id)).Concat(s.Scenario.RequirementCards.Select(x => x.Id))
            .Concat(s.Scenario.Islands.Select(x => x.Id)).Concat(s.Scenario.FundingLines.Select(x => x.Id)).Concat(s.Scenario.People.Select(x => x.Id))
            .Where(id => Regex.IsMatch(id, "[0-9]|[a-z][A-Z]"));

    [Fact]
    public void Finding_finance_sets_up_the_paying_agent_and_says_so_in_the_comptrollers_voice()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1);
        var preview = s.View.PreviewVisit(new FindFinanceAction());
        Assert.True(preview.Accepted);
        Assert.Equal(s.Rules.GoingToPeople.FindFinance.ShopHours, preview.ShopHoursMin);
        Assert.True(s.Apply(new FindFinanceAction()).Accepted);
        Assert.False(s.Apply(new FindFinanceAction()).Accepted);
        s.AdvanceDay();
        Assert.Equal(1, s.View.FinanceFoundDay);
        Assert.True(s.PayingAgentDay <= 1 + s.Rules.GoingToPeople.FindFinance.PayingAgentWithinDays);
        var said = Assert.Single(Visits(s));
        Assert.StartsWith("Capt Priya Nair, Comptroller: \"", said.Text);
        Assert.Contains("on hand", said.Text);
        var item = s.View.MorningBoard().Items.Single(i => i.RecordId == said.Id);
        Assert.Equal(BoardLinkKind.Person, item.LinkKind);
        Assert.Equal("Capt Priya Nair", item.Person!.DisplayName);
        Assert.False(s.Apply(new FindFinanceAction()).Accepted);
    }

    [Fact]
    public void The_staff_meeting_shows_requirements_before_they_are_submitted_and_recurs_weekly()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1);
        Assert.False(s.Apply(new JoinStaffMeetingAction { Attend = false }).Accepted);
        Assert.True(s.Apply(new JoinStaffMeetingAction()).Accepted);
        Assert.True(s.View.StaffMeeting.Joining);
        s.AdvanceDay();
        var first = Assert.Single(Visits(s));
        // MSgt Brandt's food service card is submitted on day 3; the meeting on day 1 hears about it first.
        Assert.Contains("MSgt Lena Brandt, Spoke C camp, said they will be asking for food service for Spoke C, wanted from day 12.", first.Text);
        var meeting = s.View.StaffMeeting;
        Assert.True(meeting.Attending);
        Assert.Equal(8, meeting.NextDay);
        Assert.Contains("req0014", meeting.PreviewedCards);
        Days(s, 7);
        Assert.Equal([1, 8], s.View.StaffMeeting.MeetingDays);
        Assert.Contains("Nobody raised anything new", Visits(s)[1].Text);
        // Each meeting after the first costs the weekly hours.
        Assert.Contains(s.Work, w => w.Kind == WorkKind.Visit && w.Label == "Staff meeting" && w.CreatedDay == 8
                                     && w.HoursTotalMilli == s.Rules.GoingToPeople.StaffMeeting.WeeklyShopHours * 1000L);
        Assert.True(s.Apply(new JoinStaffMeetingAction { Attend = false }).Accepted);
        Days(s, 8);
        Assert.Equal([1, 8], s.View.StaffMeeting.MeetingDays);
        Assert.False(s.View.StaffMeeting.Attending);
    }

    [Fact]
    public void The_priority_line_names_the_commanders_order()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1);
        s.Apply(new JoinStaffMeetingAction());
        s.AdvanceDay();
        var names = s.Scenario.Actors.Commander.PriorityOrder.Select(id => s.Scenario.Sites.First(x => x.Id == id).Name);
        Assert.Contains($"If we run short, it's {string.Join(", then ", names)}.", Visits(s)[0].Text);
        Assert.Contains("Lt Col Daniel Park, Hub commander, ran it.", Visits(s)[0].Text);
    }

    [Theory]
    [InlineData("fuel", "Bulk fuel normally comes through the J4")]
    [InlineData("routes", "The barge from")]
    [InlineData("vehicles", "contract vehicle")]
    [InlineData("thresholds", "The micro-purchase threshold for this operation is $40,000")]
    public void The_SCO_answers_with_facts_after_its_lag_and_no_directions(string question, string fact)
    {
        var s = Session.Create(Repo.Load("spoke-b"), 3);
        Assert.Equal(4, s.View.ScoQuestions.Count);
        var preview = s.View.PreviewVisit(new AskScoAction { Question = question });
        Assert.True(preview.Accepted);
        Assert.True(s.Apply(new AskScoAction { Question = question }).Accepted);
        Assert.False(s.Apply(new AskScoAction { Question = question }).Accepted);
        Assert.True(s.View.ScoQuestions.Single(q => q.Id == question).Pending);
        var lag = s.Rules.Sco.ResponseLagDaysByResponsiveness.Medium;
        Days(s, lag.Max + 1);
        var answer = Assert.Single(Visits(s));
        Assert.InRange(answer.Day, 1 + lag.Min, 1 + lag.Max);
        Assert.InRange(answer.Day, preview.EarliestStartMin, preview.EarliestStartMax);
        Assert.StartsWith("Maj Erin Cole, SCO staff, reachback, answered your question", answer.Text);
        Assert.Contains(fact, answer.Text);
        var direction = s.Rules.GoingToPeople.AskSco.Questions.Single(q => q.Id == question).TutorialDirection!;
        Assert.DoesNotContain(direction, answer.Text);
        Assert.False(s.View.ScoQuestions.Single(q => q.Id == question).Pending);
        Assert.True(s.View.ScoQuestions.Single(q => q.Id == question).Asked);
        Assert.False(s.Apply(new AskScoAction { Question = "whatShouldIDo" }).Accepted);
    }

    [Fact]
    public void In_a_tutorial_the_SCO_answer_carries_a_direction()
    {
        var s = Session.Create(Modified("spoke-b", r => r["tutorial"] = true), 1);
        s.Apply(new AskScoAction { Question = "fuel" });
        Days(s, 8);
        var direction = s.Rules.GoingToPeople.AskSco.Questions.Single(q => q.Id == "fuel").TutorialDirection!;
        Assert.Contains(direction, Assert.Single(Visits(s)).Text);
    }

    [Fact]
    public void The_route_answer_lists_the_next_runs_from_the_schedule()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1);
        s.Apply(new AskScoAction { Question = "routes" });
        Days(s, 8);
        var text = Assert.Single(Visits(s)).Text;
        Assert.Contains("The aircraft from", text);
        Assert.Contains("runs every day", text);
        Assert.Contains("2 days a week; the next runs are days", text);
        Assert.Contains("surveyed the island at the far end", text);
    }

    [Fact]
    public void Meeting_a_vendor_firms_up_the_directory_entry()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1);
        Assert.Equal("assumed", s.View.VendorConfidence("vendorD"));
        Assert.True(s.Apply(new MeetVendorAction { Vendor = "vendorD" }).Accepted);
        Assert.False(s.Apply(new MeetVendorAction { Vendor = "vendorD" }).Accepted);
        Assert.False(s.Apply(new MeetVendorAction { Vendor = "nobody" }).Accepted);
        s.AdvanceDay();
        Assert.Equal("estimated", s.View.VendorConfidence("vendorD"));
        Assert.Equal(1, s.View.VendorMetDay("vendorD"));
        var said = Assert.Single(Visits(s));
        Assert.StartsWith("Met Second Island Fuel. They offer fuel, up to", said.Text);
        Assert.Contains("now estimated, not assumed", said.Text);
        // A rep in the cast is who you sit down with; the board links the item to them.
        Assert.True(s.Apply(new MeetVendorAction { Vendor = "vendorE" }).Accepted);
        s.AdvanceDay();
        var rep = Visits(s).Last();
        Assert.StartsWith("Met Mr. Kofi Mensah of Port Fuel Haulers. They offer fuel, up to 4000 gal a day at $5 each, on a day's notice.", rep.Text);
        Assert.Equal("Mr. Kofi Mensah", s.View.RecordPerson(rep)!.DisplayName);
        Assert.Contains(s.View.VendorReps, p => p.Name == "Kofi Mensah");
    }

    [Fact]
    public void A_vendor_raises_an_undefinitized_letter_contract_the_handover_left_out()
    {
        var s = Session.Create(Repo.Load("sustainment"), 1);
        var row = s.Arrangements.Single(a => a.Id == "cLodging");
        Assert.True(s.View.ProblemHidden(row));
        Assert.True(s.Apply(new MeetVendorAction { Vendor = "thirdShelter" }).Accepted);
        s.AdvanceDay();
        Assert.False(s.View.ProblemHidden(row));
        var said = Assert.Single(Visits(s));
        Assert.Contains("They raised an old contract", said.Text);
        Assert.Contains("Lodging at Spoke C was started on a letter contract 173 days before you arrived and never definitized", said.Text);
        // A vendor with nothing hidden raises nothing.
        var t = Session.Create(Repo.Load("sustainment"), 1);
        t.Apply(new MeetVendorAction { Vendor = "northPower" });
        t.AdvanceDay();
        Assert.DoesNotContain("old contract", Assert.Single(Visits(t)).Text);
    }

    [Fact]
    public void The_J4_adds_a_site_to_the_ACSA_fuel_order_with_the_partner_stock_as_ceiling()
    {
        var s = Session.Create(Repo.Load("sustainment"), 1);
        var site = s.Scenario.Sites.First(x => !x.Id.StartsWith("north", StringComparison.Ordinal)).Id;
        var stock = (long)s.Scenario.Actors.AcsaPartner!.Stock["fuel"];
        var j4 = new EngageJ4Action { Id = "j4Fuel", Site = site, Capability = "fuel", Quantity = stock };
        var preview = s.View.PreviewVisit(j4);
        Assert.True(preview.Accepted);
        Assert.Equal(0, preview.TotalCents);
        Assert.Equal(1 + 1 + s.Rules.GoingToPeople.EngageJ4.LagDays, preview.EarliestStartMin);
        Assert.Contains(preview.SoftGaps, g => g.Contains("not a contract"));
        Assert.True(s.Apply(j4).Accepted);
        Assert.False(s.Apply(j4 with { Id = "more", Quantity = 1 }).Accepted);   // stock spoken for
        Assert.False(s.Apply(j4).Accepted);                                       // id in use
        s.AdvanceDay();
        var arr = s.Arrangements.Single(a => a.Id == "j4Fuel");
        Assert.Equal("acsa", arr.Instrument);
        Assert.Equal(0, arr.DailyCostCents);
        Assert.Null(arr.FundingLineId);
        Assert.Equal(preview.EarliestStartMin, arr.StartDay);
        Assert.Equal(s.LastDay, arr.EndDay);
        Assert.Contains("The reimbursement is the J4's, not the shop's.", Visits(s).Single().Text);
        Days(s, 6);
        Assert.Equal(ArrangementStatus.Active, arr.Status);
        while (!s.Finished) s.AdvanceDay();
        Assert.Equal(0, arr.IncurredCents);
    }

    [Fact]
    public void Without_a_partner_the_J4_has_nothing_to_add_the_site_to()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1);
        var r = s.Apply(new EngageJ4Action { Id = "j4", Site = "spokeBSite", Capability = "fuel", Quantity = 100 });
        Assert.False(r.Accepted);
        Assert.Contains("no ACSA partner", r.HardStops[0]);
    }

    [Fact]
    public void Visits_are_deterministic_and_speak_in_names_not_ids()
    {
        string Play(long seed, out Session s)
        {
            s = Session.Create(Repo.Load("spoke-b"), seed);
            s.Apply(new FindFinanceAction());
            s.Apply(new JoinStaffMeetingAction());
            foreach (var q in s.View.ScoQuestions) s.Apply(new AskScoAction { Question = q.Id });
            foreach (var v in s.Scenario.Vendors) s.Apply(new MeetVendorAction { Vendor = v.Id });
            while (!s.Finished) s.AdvanceDay();
            return s.StateHash();
        }
        Assert.Equal(Play(5, out var a), Play(5, out _));
        var ids = IdShaped(a).ToList();
        Assert.NotEmpty(ids);
        foreach (var r in Visits(a))
            foreach (var id in ids)
                Assert.False(Regex.IsMatch(r.Text, $@"\b{Regex.Escape(id)}\b"), $"'{id}' in: {r.Text}");
        Assert.Equal(4 + 1 + 5 + a.Scenario.Vendors.Count, Visits(a).Count);
        // Visits go through the input log like any action: a replay lands on the same state.
        Assert.Equal(a.StateHash(), Replay.Run(Repo.Load("spoke-b"), a.ToInputLog()).StateHash());
    }
}
