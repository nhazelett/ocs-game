using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>M3-05 (Sim side): transition plans between arrangements (SPEC 2 "plan a transition", SPEC 3 "Transition
/// plan"): cut-over after verified overlap or on a day, the planner's gap warnings, and the debrief tag.</summary>
public class TransitionTests
{
    private static string Golden(string file) => Path.Combine(Repo.Scenario("spoke-b-90"), "golden", file);

    /// <summary>The approach 4 paired log up to the start of <paramref name="day"/>, without its hand termination of gens.</summary>
    private static Session PairedOn(int day, Func<PlayerAction, bool>? keep = null)
    {
        var log = InputLog.Load(Golden("approach4-external-with-uca-bridge.json"));
        log = log with { Actions = log.Actions.Where(a => a is not TerminateAction && (keep?.Invoke(a) ?? true)).ToList() };
        return Replay.Run(Repo.Load("spoke-b-90"), log, stopAfterDay: day - 1);
    }

    private static void PlayTo(Session s, int day) { while (!s.Finished && s.Day <= day) s.AdvanceDay(); }

    private static PlanTransitionAction Plan(string id, string outgoing, string incoming) => new() { Id = id, Outgoing = outgoing, Incoming = incoming };

    [Fact]
    public void A_planned_cut_over_ends_the_bridge_where_the_hand_termination_did()
    {
        var byHand = Replay.Run(Repo.Load("spoke-b-90"), InputLog.Load(Golden("approach4-external-with-uca-bridge.json")));
        var planned = Replay.Run(Repo.Load("spoke-b-90"), InputLog.Load(Golden("approach4-transition-plans.json")));
        Assert.Equal(byHand.Arrangements.Single(a => a.Id == "gens").EndDay, planned.Arrangements.Single(a => a.Id == "gens").EndDay);
        Assert.Equal(19, planned.Arrangements.Single(a => a.Id == "gens").EndDay);
        // The second plan cuts the turnkey UCA over to the external contract after two full days instead of running to day 51.
        Assert.Equal(49, planned.Arrangements.Single(a => a.Id == "turnkey").EndDay);
        var views = planned.View.Transitions();
        Assert.All(views, v => Assert.Equal("clean", v.Status));
        Assert.All(views, v => Assert.Equal(3, v.BothRunningDays));
        Assert.Equal(2, planned.Metrics["transitions.clean"]);
        Assert.Equal(0, planned.Metrics["transitions.gap"]);
        Assert.Equal(0, planned.Metrics["site.spokeBSite.power.downDays"]);
        Assert.True(planned.Metrics["cost.incurredCents"] < byHand.Metrics["cost.incurredCents"]);
    }

    [Fact]
    public void Games_without_a_plan_carry_no_transition_metrics()
    {
        var s = Replay.Run(Repo.Load("spoke-b-90"), InputLog.Load(Golden("approach4-external-with-uca-bridge.json")));
        Assert.DoesNotContain(s.Metrics.Keys, k => k.StartsWith("transitions.", StringComparison.Ordinal));
        Assert.Empty(s.View.Transitions());
    }

    [Fact]
    public void The_cut_over_waits_for_full_delivery_and_restarts_the_count_after_a_failure()
    {
        // Over many seeds the turnkey UCA starts day 17 or later; the cut-over is never started before two full days.
        for (var seed = 1; seed <= 20; seed++)
        {
            var log = InputLog.Load(Golden("approach4-transition-plans.json")) with { Seed = seed };
            var s = Replay.Run(Repo.Load("spoke-b-90"), log);
            foreach (var t in s.Transitions.Where(x => x.CutoverStartedDay is not null && x.Handoff == "incomingDelivering"))
            {
                var inc = s.Arrangements.Single(a => a.Id == t.IncomingId);
                Assert.True(t.CutoverStartedDay >= inc.StartDay + t.OverlapDays - 1, $"seed {seed}: {t.Id} cut over day {t.CutoverStartedDay}, incoming started {inc.StartDay}");
                var failures = s.Records.Where(r => r.Kind == RecordKind.VendorFailure && r.Subject == inc.Id && r.Day >= inc.StartDay && r.Day <= t.CutoverStartedDay)
                                        .Select(r => r.Day).ToList();
                foreach (var f in failures) Assert.True(t.CutoverStartedDay >= f + t.OverlapDays, $"seed {seed}: cut-over {t.CutoverStartedDay} too soon after a failure on day {f}");
                var outgoing = s.Arrangements.Single(a => a.Id == t.OutgoingId);
                Assert.True(outgoing.EndDay >= t.CutoverStartedDay + 1);
            }
        }
    }

    [Fact]
    public void An_on_day_handoff_ends_the_outgoing_on_that_day()
    {
        var s = PairedOn(6);
        Assert.True(s.Apply(Plan("bridge", "gens", "turnkey") with { Handoff = "onDay", CutoverDay = 21 }).Accepted);
        PlayTo(s, 23);
        Assert.Equal(21, s.Arrangements.Single(a => a.Id == "gens").EndDay);
        Assert.Equal("clean", s.View.Transitions().Single().Status);
        Assert.Equal(5, s.View.Transitions().Single().BothRunningDays); // days 17 to 21
    }

    [Fact]
    public void The_planner_flags_a_projected_gap_and_the_debrief_tags_it_avoidable()
    {
        var s = Replay.Run(Repo.Load("spoke-b-90"), InputLog.Load(Golden("approach4-transition-gap.json")));
        var t = s.Transitions.Single(x => x.Id == "ucaToEnduring");
        Assert.Equal(TransitionOutcome.Gap, t.Outcome);
        Assert.Equal(41 - s.Rules.Transition.WarnDays, t.WarningDay);
        var plan = s.Records.Single(r => r.Kind == RecordKind.Action && r.Subject == "ucaToEnduring");
        Assert.Contains("a gap of at least 5 day(s) (days 42 to 46)", plan.Text);
        var warning = s.Records.Single(r => r.Id == t.WarningRecordId);
        Assert.Equal(RecordKind.Transition, warning.Kind);
        Assert.Equal(1, s.Metrics["transitions.gap"]);

        var gap = s.View.Debrief().Gaps.Single(g => g.StartDay == 42);
        Assert.Equal(GapTag.Avoidable, gap.Tag);
        var f = gap.Factors.Single(x => x.Kind == GapFactorKind.TransitionGap);
        Assert.Equal(GapTag.Avoidable, f.Tag);
        Assert.Contains(t.WarningRecordId!, f.RecordIds);
        Assert.Contains("flagged it on day 36", f.Text);
    }

    [Fact]
    public void The_morning_board_shows_the_warning()
    {
        var s = Replay.Run(Repo.Load("spoke-b-90"), InputLog.Load(Golden("approach4-transition-gap.json")), stopAfterDay: 36);
        var item = s.View.MorningBoard().Items.Single(i => i.Text.StartsWith("Transition planner:", StringComparison.Ordinal));
        Assert.Equal(BoardSeverity.Warning, item.Severity);
        Assert.Equal(BoardLinkKind.Arrangement, item.LinkKind);
        Assert.Equal("turnkey", item.LinkId);
    }

    [Fact]
    public void A_lost_incoming_is_flagged_and_its_plan_ends_in_a_gap()
    {
        // The turnkey UCA is terminated before it starts: the plan's incoming will not arrive.
        var s = PairedOn(6);
        Assert.True(s.Apply(Plan("bridge", "gens", "turnkey")).Accepted);
        Assert.True(s.Apply(new TerminateAction { Arrangement = "turnkey" }).Accepted);
        PlayTo(s, 8);
        var v = s.View.Transitions().Single();
        Assert.True(v.IncomingLost);
        Assert.Contains(v.Warnings, w => w.Contains(" will not arrive", StringComparison.Ordinal));
        PlayTo(s, 70);
        var t = s.Transitions.Single();
        Assert.Equal(66 - s.Rules.Transition.WarnDays, t.WarningDay);
        Assert.Equal(TransitionOutcome.Gap, t.Outcome);
        Assert.Equal(67, t.ResolvedDay);
    }

    [Fact]
    public void A_plan_to_an_external_request_projects_its_start_range_until_the_award()
    {
        var s = PairedOn(4, a => a is not AwardAction { Id: "turnkey" });
        Assert.True(s.Apply(Plan("toEnduring", "gens", "extPower")).Accepted);
        var before = s.View.Transitions().Single();
        Assert.Null(before.IncomingStartDay);
        Assert.NotNull(before.IncomingStartMin);
        Assert.True(before.IncomingStartMax > before.IncomingStartMin);
        PlayTo(s, 16);
        var after = s.View.Transitions().Single();
        var ext = s.Arrangements.Single(a => a.Id == "extPower");
        Assert.Equal(ext.StartDay, after.IncomingStartDay);
        Assert.Equal(ext.StartDay, after.IncomingStartMin);
        Assert.Equal(ext.StartDay, after.IncomingStartMax);
        Assert.InRange(ext.StartDay, before.IncomingStartMin!.Value, before.IncomingStartMax!.Value);
    }

    public static IEnumerable<object[]> Stops() =>
    [
        [Plan("", "gens", "turnkey"), "needs an id"],
        [Plan("p", "nope", "turnkey"), "is not an arrangement"],
        [Plan("p", "gens", "nope"), "is not an arrangement or an external support request"],
        [Plan("p", "gens", "gens"), "the same arrangement"],
        [Plan("p", "gens", "hubFuelRun"), "is fuel at Spoke B"],
        [Plan("p", "gens", "turnkey") with { OverlapDays = 0 }, "at least one day"],
        [Plan("p", "gens", "turnkey") with { Owner = "sco" }, "gap owner sco"],
        [Plan("p", "gens", "turnkey") with { Handoff = "whenever" }, "handoff whenever"],
        [Plan("p", "gens", "turnkey") with { Handoff = "onDay" }, "needs a cut-over day"],
        [Plan("p", "gens", "turnkey") with { Handoff = "onDay", CutoverDay = 6 }, "after today"],
        [Plan("p", "gens", "turnkey") with { Handoff = "onDay", CutoverDay = 67 }, "ends (day 66)"],
    ];

    [Theory]
    [MemberData(nameof(Stops))]
    public void Hard_stops_reject_the_plan_and_the_preview_matches_without_changing_state(PlanTransitionAction plan, string expected)
    {
        var s = PairedOn(6);
        var hash = s.StateHash();
        var preview = s.View.PreviewTransition(plan);
        Assert.False(preview.Accepted);
        Assert.Contains(preview.HardStops, h => h.Contains(expected, StringComparison.Ordinal));
        Assert.Equal(hash, s.StateHash());
        var result = s.Apply(plan);
        Assert.False(result.Accepted);
        Assert.Equal(preview.HardStops, result.HardStops);
        Assert.Empty(s.Transitions);
    }

    [Fact]
    public void An_outgoing_in_an_open_plan_cannot_be_planned_twice_and_a_preview_changes_nothing()
    {
        var s = PairedOn(6);
        var hash = s.StateHash();
        var preview = s.View.PreviewTransition(Plan("bridge", "gens", "turnkey"));
        Assert.True(preview.Accepted);
        Assert.Equal("planned", preview.Plan!.Status);
        Assert.Equal(17, preview.Plan.IncomingStartDay);
        Assert.Equal(hash, s.StateHash());
        Assert.True(s.Apply(Plan("bridge", "gens", "turnkey")).Accepted);
        var again = s.Apply(Plan("bridge2", "gens", "turnkey"));
        Assert.False(again.Accepted);
        Assert.Contains("already the outgoing side of the hand-off", again.Message);
        Assert.False(s.Apply(Plan("bridge", "turnkey", "gens")).Accepted); // id reused
    }

    [Fact]
    public void Saving_mid_plan_and_resuming_reaches_the_same_end()
    {
        var bundle = Repo.Load("spoke-b-90");
        var log = InputLog.Load(Golden("approach4-transition-plans.json"));
        var whole = Replay.Run(bundle, log);
        foreach (var day in new[] { 6, 18, 19, 48, 50 })
        {
            var part = Replay.Run(bundle, log, stopAfterDay: day - 1);
            foreach (var a in log.Actions.Where(a => a.Day == day)) part.Apply(a);
            var resumed = Replay.Resume(bundle, part.ToSave());
            var rest = log.Actions.Where(a => a.Day > day).GroupBy(a => a.Day).ToDictionary(g => g.Key, g => g.ToList());
            while (!resumed.Finished)
            {
                if (resumed.Day != day && rest.TryGetValue(resumed.Day, out var acts)) foreach (var a in acts) resumed.Apply(a);
                resumed.AdvanceDay();
            }
            Assert.Equal(whole.StateHash(), resumed.StateHash());
        }
    }
}
