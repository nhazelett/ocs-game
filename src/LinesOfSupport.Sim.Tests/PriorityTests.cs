using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>M2-14: a refused handshake puts that site's shop work behind other work when hours are short.</summary>
public class PriorityTests
{
    private static readonly AwardAction Gens = new()
    {
        Day = 1, Id = "gens", Instrument = "bpaCall", Vendor = "vendorC", Vehicle = "bosBpa", Card = "req0007",
        Quantity = 360, StartDay = 7, DurationDays = 60, FundingLine = "om1",
    };

    /// <summary>Approach 1 to day 12; optionally refuse the handshake; award a Spoke B UCA; on the day its shop work
    /// becomes ready, queue a survey and a BPA setup (work tied to no site) behind it: more ready work than the
    /// shop's 16 hours a day.</summary>
    private static Session Day12(bool refuse, bool surveys = true)
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1);
        Assert.True(s.Apply(Gens).Accepted);
        while (s.Day < 12) s.AdvanceDay();
        if (refuse) Assert.True(s.Apply(new ResolveCommitmentAction { Commitment = "uc1", Choice = "refuse" }).Accepted);
        var r = s.Apply(new AwardAction
        {
            Day = 12, Id = "turnkey", Instrument = "uca", Vendor = "vendorB", Card = "req0007",
            Quantity = 470, StartDay = 20, DurationDays = 60, FundingLine = "om1",
        });
        Assert.True(r.Accepted, string.Join("; ", r.HardStops));
        while (s.Day < Award(s).WaitUntilDay) s.AdvanceDay();
        if (surveys)
        {
            Assert.True(s.Apply(new SurveyAction { Day = s.Day, Island = "spokeB" }).Accepted);
            var bpa = s.Apply(new EstablishBpaAction { Day = s.Day, Id = "fuelBpa", Vendor = "vendorD", Capability = "fuel", ScopeLines = ["delivery"], CallLimitCents = 5_000_000 });
            Assert.True(bpa.Accepted, string.Join("; ", bpa.HardStops));
        }
        return s;
    }

    private static WorkItem Award(Session s) => s.Work.Single(w => w.Kind == WorkKind.Award && w.Label.EndsWith("turnkey"));

    [Fact]
    public void Refusal_marks_the_site_and_only_that_site()
    {
        var s = Day12(refuse: true, surveys: false);
        Assert.Equal(["spokeBSite"], s.View.LostConfidenceSites);
        Assert.Empty(Day12(refuse: false, surveys: false).View.LostConfidenceSites);
    }

    [Fact]
    public void Held_back_work_waits_behind_other_work_and_is_recorded()
    {
        var s = Day12(refuse: true);
        var award = Award(s);
        var before = award.HoursRemainingMilli;
        var others = s.Work.Where(w => w != award && w.CompletedDay is null && w.WaitUntilDay <= s.Day).ToList();
        Assert.All(others, w => Assert.True(w.Kind is WorkKind.Survey or WorkKind.EstablishVehicle, w.Label));
        var otherHours = others.Sum(w => w.HoursRemainingMilli);
        Assert.True(before + otherHours > 16_000, "the test needs the shop to be short today");
        s.AdvanceDay();

        // The other work was served first although it was queued after; the award got what was left of the day.
        Assert.Equal(before - Math.Max(0, 16_000 - otherHours), award.HoursRemainingMilli);
        var rec = Assert.Single(s.Records, r => r.Kind == RecordKind.Compliance && r.Subject == award.Id);
        Assert.Contains("waits behind other work", rec.Text);
        Assert.Contains("uc1", rec.Text);
        var refusal = s.Records.Single(r => r.Kind == RecordKind.ArrangementEnd && r.Subject == "uc1");
        Assert.Contains(refusal.Id, rec.CausedBy);
    }

    [Fact]
    public void Without_a_refusal_the_queue_is_first_come_first_served()
    {
        var s = Day12(refuse: false);
        var award = Award(s);
        var before = award.HoursRemainingMilli;
        Assert.Equal(award, s.Work.First(w => w.CompletedDay is null && w.WaitUntilDay <= s.Day));
        s.AdvanceDay();
        Assert.Equal(Math.Max(0, before - 16_000), award.HoursRemainingMilli);
        Assert.DoesNotContain(s.Records, r => r.Text.Contains("waits behind other work"));
    }

    [Fact]
    public void When_the_shop_is_not_short_nothing_is_held_back_or_recorded()
    {
        var s = Day12(refuse: true, surveys: false);
        var award = Award(s);
        var hoursToday = s.Work.Where(w => w.CompletedDay is null && w.WaitUntilDay <= s.Day).Sum(w => w.HoursRemainingMilli);
        s.AdvanceDay();
        if (hoursToday <= 16_000) Assert.True(award.HoursRemainingMilli == 0 || award.CompletedDay is not null);
        Assert.DoesNotContain(s.Records, r => r.Text.Contains("waits behind other work"));
    }

    [Fact]
    public void Held_back_award_shows_as_a_gap_factor_when_nothing_else_supplies_the_site()
    {
        var s = Day12(refuse: true);
        while (!s.Finished) s.AdvanceDay();
        var debrief = s.View.Debrief();
        var factors = debrief.Gaps.Where(g => g.SiteId == "spokeBSite").SelectMany(g => g.Factors).Where(f => f.Kind == GapFactorKind.PriorityDeferred).ToList();
        Assert.NotEmpty(factors);
        Assert.All(factors, f => Assert.Equal(GapTag.Avoidable, f.Tag));
        Assert.Contains(factors, f => f.Text.Contains("lost confidence"));
    }
}
