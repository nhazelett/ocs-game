using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Policies;

namespace LinesOfSupport.Sim.Tests;

/// <summary>M1-04: unauthorized commitments per SPEC 4.5.</summary>
public class CommitmentTests
{
    private static readonly AwardAction Gens = new()
    {
        Day = 1, Id = "gens", Instrument = "bpaCall", Vendor = "vendorC", Vehicle = "bosBpa", Card = "req0007",
        Quantity = 360, StartDay = 7, DurationDays = 60, FundingLine = "om1",
    };

    /// <summary>Approach 1 up to the morning the handshake is in the inbox (day 12), then the given choice.</summary>
    private static Session HandshakeThen(long seed, string? choice, string scenario = "spoke-b")
    {
        var s = Session.Create(Repo.Load(scenario), seed);
        while (!s.Finished)
        {
            if (s.Day == 1) Assert.True(s.Apply(Gens).Accepted);
            if (choice is not null && s.Commitments.FirstOrDefault(c => c.Id == "uc1") is { Unresolved: true, Decision: CommitmentDecision.Open })
                Assert.True(s.Apply(new ResolveCommitmentAction { Commitment = "uc1", Choice = choice, FundingLine = "om1" }).Accepted);
            s.AdvanceDay();
        }
        return s;
    }

    [Fact]
    public void Handshake_arrives_as_an_open_decision_with_no_automatic_ratification()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1);
        s.Apply(Gens);
        while (s.Day <= 11) s.AdvanceDay();
        var c = Assert.Single(s.Commitments, x => x.Kind == CommitmentKind.Handshake);
        Assert.Equal(CommitmentDecision.Open, c.Decision);
        Assert.InRange(c.PatienceDays, 5, 15);
        Assert.DoesNotContain(s.Work, w => w.Kind == WorkKind.Ratification);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(42)]
    public void Ratify_pays_the_vendor_and_keeps_it_delivering(long seed)
    {
        var s = HandshakeThen(seed, "ratify");
        var c = s.Commitments.First(x => x.Id == "uc1");
        Assert.Equal(CommitmentDecision.Ratified, c.Decision);
        Assert.False(c.WalkedOff);
        Assert.False(c.InquiryDrawn);
        Assert.Equal(1, s.Metrics["compliance.ratifications"]);
        Assert.Equal(0, s.Metrics["compliance.handshakeWalkOffs"]);
        Assert.True(s.Arrangements.First(a => a.Id == "uc1").ObligatedCents > 0);
        Assert.Equal(0, s.Metrics["site.spokeBSite.unitHabitPermille"]);
    }

    [Fact]
    public void Park_runs_out_the_vendors_patience_then_it_walks_off_and_sours()
    {
        var walked = 0;
        for (long seed = 1; seed <= 40; seed++)
        {
            var s = HandshakeThen(seed, "park");
            var c = s.Commitments.First(x => x.Id == "uc1");
            Assert.Equal(CommitmentDecision.Parked, c.Decision);
            var walk = s.Records.FirstOrDefault(r => r.Kind == RecordKind.Withdrawal && r.Subject == "uc1");
            if (walk is null) { Assert.True(c.OpenedDay + c.PatienceDays > s.LastDay); continue; }
            walked++;
            Assert.Equal(c.OpenedDay + c.PatienceDays, walk.Day);
            Assert.True(c.WalkedOff);
            var vendor = s.View.ActionEditor("spokeBSite", "fuel")!.Vendors.Single(v => v.Id == c.VendorId);
            Assert.NotNull(vendor.Standing);
            Assert.True(s.Metrics["site.spokeBSite.unitHabitPermille"] > 0);
        }
        Assert.True(walked > 30, $"only {walked} of 40 walked off");
    }

    [Fact]
    public void Long_unresolved_handshake_draws_an_hhq_inquiry()
    {
        var s = HandshakeThen(1, null);
        var c = s.Commitments.First(x => x.Id == "uc1");
        Assert.True(c.InquiryDrawn);
        var inquiry = s.Records.First(r => r.Kind == RecordKind.Compliance && r.Subject == "uc1" && r.Text.StartsWith("Higher HQ"));
        Assert.Equal(c.OpenedDay + s.Rules.Unauthorized.ParkedInquiryAfterDays, inquiry.Day);
    }

    [Fact]
    public void Refuse_stops_supply_today_and_costs_commander_confidence()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1);
        s.Apply(Gens);
        while (s.Day <= 11) s.AdvanceDay();
        Assert.True(s.Apply(new ResolveCommitmentAction { Commitment = "uc1", Choice = "refuse" }).Accepted);
        var arr = s.Arrangements.First(a => a.Id == "uc1");
        Assert.Equal(ArrangementStatus.Ended, arr.Status);
        Assert.True(arr.EndDay < s.Day);
        Assert.Equal(1, s.Metrics["compliance.commanderConfidenceHits"]);
        Assert.Equal(s.Rules.Unauthorized.HabitRiseParkOrRefusePermille, s.View.UnitHabit("spokeBSite"));
    }

    [Fact]
    public void Wrong_or_repeated_choices_are_hard_stops_that_leave_the_decision_open()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1);
        s.Apply(Gens);
        while (s.Day <= 11) s.AdvanceDay();
        Assert.False(s.Apply(new ResolveCommitmentAction { Commitment = "uc1", Choice = "deny" }).Accepted);
        Assert.False(s.Apply(new ResolveCommitmentAction { Commitment = "nope", Choice = "ratify" }).Accepted);
        Assert.False(s.Apply(new ResolveCommitmentAction { Commitment = "uc1", Choice = "ratify", FundingLine = "nope" }).Accepted);
        Assert.Equal(CommitmentDecision.Open, s.Commitments.First(c => c.Id == "uc1").Decision);
        Assert.Equal(3, s.Metrics["actions.rejected"]);
        Assert.True(s.Apply(new ResolveCommitmentAction { Commitment = "uc1", Choice = "ratify" }).Accepted);
        Assert.False(s.Apply(new ResolveCommitmentAction { Commitment = "uc1", Choice = "refuse" }).Accepted);
    }

    [Fact]
    public void Out_of_scope_work_needs_an_active_contract_and_deny_costs_no_money()
    {
        var none = Session.Create(Repo.Load("spoke-b"), 1);
        while (!none.Finished) none.AdvanceDay();
        Assert.DoesNotContain(none.Commitments, c => c.Kind == CommitmentKind.OutOfScope);

        long Obligated(string choice)
        {
            var s = Session.Create(Repo.Load("spoke-b"), 1);
            s.Apply(Gens);
            while (!s.Finished)
            {
                if (s.Commitments.FirstOrDefault(c => c.Kind == CommitmentKind.OutOfScope) is { Unresolved: true } c)
                    Assert.True(s.Apply(new ResolveCommitmentAction { Commitment = c.Id, Choice = choice, FundingLine = "om1" }).Accepted);
                if (s.Commitments.FirstOrDefault(c => c.Id == "uc1") is { Unresolved: true })
                    s.Apply(new ResolveCommitmentAction { Commitment = "uc1", Choice = "ratify", FundingLine = "om1" });
                s.AdvanceDay();
            }
            var oos = s.Commitments.Single(c => c.Kind == CommitmentKind.OutOfScope);
            Assert.False(oos.InquiryDrawn);
            return (long)s.Metrics["cost.obligatedCents"];
        }
        Assert.Equal(240000, Obligated("ratify") - Obligated("deny"));
    }

    [Fact]
    public void Unresolved_out_of_scope_work_becomes_a_claim_and_an_inquiry()
    {
        var s = Replay.Run(Repo.Load("spoke-b"), InputLog.Load(Path.Combine(Repo.Scenario("spoke-b"), "golden", "approach4-enduring-only.json")));
        var oos = s.Commitments.Single(c => c.Kind == CommitmentKind.OutOfScope);
        Assert.True(oos.Unresolved);
        Assert.True(oos.InquiryDrawn);
        Assert.Contains(s.Records, r => r.Subject == oos.Id && r.Text.Contains("filed a claim"));
    }

    [Fact]
    public void Habit_handshakes_only_happen_once_a_unit_has_a_habit()
    {
        for (long seed = 1; seed <= 30; seed++)
        {
            var textbook = Replay.Run(Repo.Load("spoke-b-90"), seed, PolicyCatalog.Create("textbook"));
            Assert.Equal(0, textbook.Metrics["compliance.habitHandshakes"]);
        }
        var parked = HandshakeThen(3, "park", "spoke-b-90");
        Assert.True(parked.Metrics["compliance.habitHandshakes"] > 0);
        Assert.DoesNotContain(parked.Commitments.Where(c => c.Id != "uc1" && c.Kind == CommitmentKind.Handshake),
                              c => parked.Commitments.Any(w => w.WalkedOff && w.VendorId == c.VendorId && w.OpenedDay < c.OpenedDay));
    }

    [Fact]
    public void Preview_shows_a_soured_vendor_as_a_hard_stop_or_a_markup()
    {
        for (long seed = 1; seed <= 40; seed++)
        {
            var s = HandshakeThen(seed, "park");
            var c = s.Commitments.First(x => x.Id == "uc1");
            if (!c.WalkedOff) continue;
            var ed = s.View.ActionEditor("spokeBSite", "fuel")!;
            var v = ed.Vendors.Single(x => x.Id == c.VendorId);
            Assert.NotNull(v.Standing);
            if (v.Standing!.StartsWith("declines"))
            {
                // Run again, stop on the day after walk-off, and check the award preview names the refusal.
                var again = Session.Create(Repo.Load("spoke-b"), seed);
                while (again.Day <= c.OpenedDay + c.PatienceDays)
                {
                    if (again.Day == 1) again.Apply(Gens);
                    if (again.Commitments.FirstOrDefault(x => x.Id == "uc1") is { Decision: CommitmentDecision.Open })
                        again.Apply(new ResolveCommitmentAction { Commitment = "uc1", Choice = "park" });
                    again.AdvanceDay();
                }
                var p = again.View.Preview(new AwardAction { Id = "f1", Instrument = "purchaseOrder", Vendor = c.VendorId!, Site = "spokeBSite", Capability = "fuel", Quantity = 100, DurationDays = 10, FundingLine = "om1" });
                Assert.Contains(p.HardStops, h => h.Contains("declines work from this office"));
                return;
            }
        }
    }
}
