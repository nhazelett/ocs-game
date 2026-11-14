using LinesOfSupport.Sim.Core;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-50 (Q23, D17): every action an assignee picker offers has a preview, so each person's choice says when the
/// work would come out with them; handing work over keeps its place in the order it was given.</summary>
public class WorkPreviewTests
{
    private static Session Tutorial()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1);
        while (s.Day < 9) s.AdvanceDay();
        Assert.True(s.Apply(new AwardAction
        {
            Id = "waste", Instrument = "purchaseOrder", Vendor = "sanitation", Card = "tutWaste", Quantity = 3600,
            StartDay = 13, DurationDays = 8, FundingLine = "om1",
        }).Accepted);
        s.AdvanceDay();
        return s;
    }

    private static Session SpokeB() => Session.Create(Repo.Load("spoke-b"), 1);

    [Fact]
    public void Handing_work_over_says_when_it_would_be_done_with_each_person_and_marks_who_holds_it()
    {
        var s = Tutorial();
        var w = s.Work.Single(x => x.CompletedDay is null && x.Assignee == "you" && x.Label.Contains("purchase order", StringComparison.OrdinalIgnoreCase));
        var choices = s.View.AssigneeChoices(new AssignAction { Work = w.Id });
        Assert.All(choices, c => Assert.NotNull(c.ResultDayMin));
        var you = choices.Single(c => c.MemberId == "you");
        Assert.True(you.Current);
        Assert.Contains("You are on it now; it would be done day", you.Sentence);
        var avery = choices.Single(c => c.MemberId == "avery");
        Assert.False(avery.Current);
        Assert.True(avery.ResultDayMin < you.ResultDayMin, "the experienced CCO, free, finishes it sooner");
        Assert.True(avery.HoursMax < you.HoursMax, "and in fewer of her own hours");
        Assert.Contains("This takes them", avery.Sentence);

        var p = s.View.PreviewAny(new AssignAction { Work = w.Id, To = "you" });
        Assert.False(p.Accepted);
        Assert.Equal("You are already doing it", p.HardStops.Single());

        Assert.True(s.Apply(new AssignAction { Work = w.Id, To = "avery" }).Accepted);
        while (w.CompletedDay is null && s.Day < 40) s.AdvanceDay();
        Assert.Equal(avery.ResultDayMin, w.CompletedDay);
    }

    [Fact]
    public void Work_handed_over_keeps_its_place_ahead_of_what_was_given_to_them_later()
    {
        var s = Tutorial();
        var order = s.Work.Single(x => x.CompletedDay is null && x.Assignee == "you" && x.Label.Contains("purchase order", StringComparison.OrdinalIgnoreCase));
        // A visit given to the junior after the order was given: the order, handed over, still comes first.
        Assert.True(s.Apply(new MeetVendorAction { Vendor = "sanitation", Assignee = "novak" }).Accepted);
        var visit = s.Work.Last();
        var choice = s.View.AssigneeChoices(new AssignAction { Work = order.Id }).Single(c => c.MemberId == "novak");
        var hand = s.View.PreviewAny(new AssignAction { Work = order.Id, To = "novak" });
        Assert.Equal(0, hand.QueueAheadHours);
        Assert.Equal(choice.ResultDayMin, hand.EarliestStartMin);

        Assert.True(s.Apply(new AssignAction { Work = order.Id, To = "novak" }).Accepted);
        while ((order.CompletedDay is null || visit.CompletedDay is null) && s.Day < 60) s.AdvanceDay();
        Assert.Equal(choice.ResultDayMin, order.CompletedDay);
        Assert.True(visit.CompletedDay >= order.CompletedDay, "the visit waited behind the order");
    }

    [Fact]
    public void A_survey_is_quoted_for_the_day_its_findings_come_in()
    {
        var s = SpokeB();
        var survey = new SurveyAction { Island = "spokeB" };
        var choices = s.View.AssigneeChoices(survey);
        var you = Assert.Single(choices);
        Assert.NotNull(you.ResultDayMin);
        Assert.Contains("it would be done day", you.Sentence);
        Assert.True(s.Apply(survey).Accepted);
        var w = s.Work.Last();
        while (w.CompletedDay is null) s.AdvanceDay();
        Assert.Equal(you.ResultDayMin, w.CompletedDay + s.Rules.Model.Work.SurveyReturnDays);
        Assert.False(s.View.PreviewAny(survey).Accepted);
    }

    [Fact]
    public void A_termination_is_quoted_for_the_last_day_the_contract_runs()
    {
        var s = SpokeB();
        Assert.True(s.Apply(new AwardAction { Id = "f", Instrument = "purchaseOrder", Vendor = "vendorE", Site = "hubSite", Capability = "fuel", Quantity = 1000, DurationDays = 40, FundingLine = "om1" }).Accepted);
        while (s.Day < 10) s.AdvanceDay();
        var end = new TerminateAction { Arrangement = "f" };
        var p = s.View.PreviewAny(end);
        Assert.True(p.Accepted, string.Join("; ", p.HardStops));
        Assert.True(p.ShopHoursMax > 0);
        Assert.True(s.Apply(end).Accepted);
        var w = s.Work.Last();
        while (w.CompletedDay is null) s.AdvanceDay();
        Assert.Equal(p.EarliestStartMin, s.Arrangements.Single(a => a.Id == "f").EndDay);
    }

    [Fact]
    public void Definitizing_is_quoted_as_the_range_of_its_hours_and_the_work_lands_inside_it()
    {
        var s = SpokeB();
        Assert.True(s.Apply(new AwardAction { Id = "u", Instrument = "uca", Vendor = "vendorB", Site = "spokeBSite", Capability = "power", Lines = ["generation", "fuelSupply", "operator", "maintenance"], Quantity = 470, DurationDays = 60, FundingLine = "om1" }).Accepted);
        var d = new DefinitizeAction { Arrangement = "u", FundingLine = "om1" };
        Assert.False(s.View.PreviewAny(d).Accepted);
        while (!s.Arrangements.Any(a => a.Id == "u" && a.Uca!.ProposalReceived)) s.AdvanceDay();
        var p = s.View.PreviewAny(d);
        Assert.True(p.Accepted, string.Join("; ", p.HardStops));
        Assert.True(s.Apply(d).Accepted);
        var w = s.Work.Last();
        Assert.InRange(w.HoursTotalMilli, p.ShopHoursMin * 1000L - 999, p.ShopHoursMax * 1000L);
        while (w.CompletedDay is null) s.AdvanceDay();
        Assert.InRange(w.CompletedDay!.Value, p.EarliestStartMin, p.EarliestStartMax);
    }

    [Fact]
    public void Coordination_takes_nobody_s_hours_and_reads_as_when_it_could_start()
    {
        var s = SpokeB();
        var c = new CoordinateAction { Id = "c", Site = "spokeBSite", Capability = "fuel", Quantity = 900, FromSite = "hubSite" };
        var choice = Assert.Single(s.View.AssigneeChoices(c));
        Assert.Equal(0, choice.HoursMax);
        Assert.Contains("it could start day", choice.Sentence);
        Assert.True(s.Apply(c).Accepted);
        Assert.InRange(s.Arrangements.Single(a => a.Id == "c").StartDay, choice.ResultDayMin!.Value, choice.ResultDayMax!.Value);
    }

    [Fact]
    public void No_action_the_game_can_hand_out_falls_through_to_not_a_visit()
    {
        var s = SpokeB();
        PlayerAction[] actions =
        [
            new SurveyAction { Island = "nowhere" }, new CoordinateAction(), new DefinitizeAction { Arrangement = "x" },
            new TerminateAction { Arrangement = "x" }, new CureNoticeAction { Issue = "x" },
            new ResolveCommitmentAction { Commitment = "x", Choice = "ratify" }, new AssignAction { Work = "x", To = "you" },
        ];
        foreach (var a in actions)
        {
            var p = s.View.PreviewAny(a);
            Assert.False(p.Accepted);
            Assert.DoesNotContain(p.HardStops, h => h.Contains("not a visit", StringComparison.OrdinalIgnoreCase));
            // The preview refuses with the words Apply refuses with.
            var r = s.Apply(a);
            Assert.False(r.Accepted);
            Assert.Equal(r.HardStops, p.HardStops);
        }
    }
}
