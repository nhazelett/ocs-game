using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-37 (docs/design/desk-redesign.md D17): each person works their queue in the order it was given, so a
/// quote's earliest day counts the work already ahead of it, and the assignee picker says who is free.</summary>
public class QueueTimingTests
{
    private static AwardAction WastePo(string? who) => new()
    {
        Id = "waste", Instrument = "purchaseOrder", Vendor = "sanitation", Card = "tutWaste", Quantity = 3600,
        StartDay = 13, DurationDays = 8, FundingLine = "om1", Assignee = who,
    };

    private static Session LoadedOnYou()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1);
        while (s.Day < 9) s.AdvanceDay();
        Assert.True(s.Apply(WastePo(null)).Accepted);
        s.AdvanceDay();   // the order waits a day on finance's certification, then it is the first thing on your desk
        return s;
    }

    [Fact]
    public void A_visit_given_to_someone_with_a_queue_is_quoted_behind_it_and_comes_out_that_day()
    {
        var s = LoadedOnYou();
        var you = s.View.Team.Single(m => m.Role == "you");
        Assert.True(you.HoursQueued > you.HoursPerDay);
        var visit = new MeetVendorAction { Vendor = "sanitation", Assignee = "you" };
        var p = s.View.PreviewVisit(visit);
        Assert.True(p.Accepted, string.Join("; ", p.HardStops));
        Assert.True(p.EarliestStartMin > s.Day, "the visit waits behind the order already on you");
        Assert.True(p.EarliestStartMin >= you.QueueClearsDay, "nothing of yours is done after the work already on you");

        Assert.True(s.Apply(visit).Accepted);
        var w = s.Work.Last();
        while (w.CompletedDay is null && s.Day < 30) s.AdvanceDay();
        Assert.Equal(p.EarliestStartMin, w.CompletedDay);
    }

    [Fact]
    public void The_same_visit_given_to_a_free_teammate_is_quoted_for_today()
    {
        var s = LoadedOnYou();
        var p = s.View.PreviewVisit(new MeetVendorAction { Vendor = "sanitation", Assignee = "avery" });
        Assert.Equal(s.Day, p.EarliestStartMin);
        Assert.True(p.EarliestStartMin < s.View.PreviewVisit(new MeetVendorAction { Vendor = "sanitation" }).EarliestStartMin);
    }

    [Fact]
    public void A_buy_given_behind_a_queue_starts_later_than_the_same_buy_given_to_a_free_teammate()
    {
        var s = LoadedOnYou();
        var call = new AwardAction
        {
            Id = "call", Instrument = "bpaCall", Vendor = "harborEquipment", Vehicle = "equipBpa", Card = "tutForklift",
            Quantity = 3, DurationDays = 8, FundingLine = "om1",
        };
        var mine = s.View.Preview(call with { Assignee = "you" });
        var avery = s.View.Preview(call with { Assignee = "avery" });
        Assert.True(mine.Accepted && avery.Accepted, string.Join("; ", mine.HardStops.Concat(avery.HardStops)));
        Assert.True(mine.EarliestStartMin > avery.EarliestStartMin);
    }

    [Fact]
    public void The_team_view_says_when_each_queue_clears_and_it_does()
    {
        var s = LoadedOnYou();
        var team = s.View.Team;
        Assert.Null(team.Single(m => m.Role == "junior").QueueClearsDay);
        var clears = team.Single(m => m.Role == "you").QueueClearsDay;
        Assert.NotNull(clears);
        Assert.True(clears > s.Day);
        var mine = s.Work.Where(w => w.Assignee == "you" && w.CompletedDay is null).ToList();
        while (s.Day <= clears!.Value) s.AdvanceDay();
        Assert.All(mine, w => Assert.True(w.CompletedDay <= clears, $"{w.Label} finished day {w.CompletedDay}, after day {clears}"));
        Assert.Contains(mine, w => w.CompletedDay == clears);
    }

    [Fact]
    public void Assignee_choices_say_who_is_free_and_when_the_work_would_come_out_with_each()
    {
        var s = LoadedOnYou();
        var visit = new MeetVendorAction { Vendor = "sanitation" };
        var choices = s.View.AssigneeChoices(visit);
        Assert.Equal(s.View.Team.Select(m => m.Id), choices.Select(c => c.MemberId));
        foreach (var c in choices)
        {
            var p = s.View.PreviewVisit(visit with { Assignee = c.MemberId });
            Assert.Equal(p.EarliestStartMin, c.ResultDayMin);
            Assert.Equal(p.ShopHoursMax, c.HoursMax);
            Assert.DoesNotContain("  ", c.Sentence);
        }
        var you = choices.Single(c => c.MemberId == "you");
        Assert.StartsWith("You: ", you.Sentence);
        Assert.Contains($"queued, clear by day {you.QueueClearsDay}.", you.Sentence);
        Assert.Contains($"it would be done day {you.ResultDayMin}.", you.Sentence);
        var avery = choices.Single(c => c.Name.EndsWith("Joan Avery"));
        Assert.StartsWith("MSgt Joan Avery: 8 hours free today, nothing queued. This takes them", avery.Sentence);
        Assert.EndsWith($"it would be done day {s.Day}.", avery.Sentence);
    }

    [Fact]
    public void A_buy_reads_as_when_it_could_start_and_a_refused_one_says_only_who_is_free()
    {
        var s = LoadedOnYou();
        var choices = s.View.AssigneeChoices(new AwardAction
        {
            Id = "call", Instrument = "bpaCall", Vendor = "harborEquipment", Vehicle = "equipBpa", Card = "tutForklift",
            Quantity = 3, DurationDays = 8, FundingLine = "om1",
        });
        Assert.All(choices, c => Assert.Contains("it could start day", c.Sentence));
        var refused = s.View.AssigneeChoices(new MeetVendorAction { Vendor = "no-such-vendor" });
        Assert.All(refused, c => { Assert.Null(c.ResultDayMin); Assert.EndsWith(".", c.Sentence); Assert.DoesNotContain("This takes", c.Sentence); });
    }
}
