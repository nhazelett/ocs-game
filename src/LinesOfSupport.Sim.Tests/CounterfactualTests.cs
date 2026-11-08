using System.Diagnostics;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Policies;

namespace LinesOfSupport.Sim.Tests;

/// <summary>
/// M3-02: one changed decision, same seed, re-run and compared. The golden pairs that differ by a single decision
/// (approach 1 ratify vs refuse or park; with or without the day 43 cure notice or termination) are exact checks.
/// </summary>
public class CounterfactualTests
{
    private static InputLog Golden(string scenario, string name)
        => InputLog.Load(Path.Combine(Repo.Scenario(scenario), "golden", name + ".json"));

    private static int IndexOf(InputLog log, Func<PlayerAction, bool> pick)
    {
        var i = log.Actions.FindIndex(a => pick(a));
        Assert.True(i >= 0, "action not found in the log");
        return i;
    }

    [Theory]
    [InlineData("refuse")]
    [InlineData("park")]
    public void Changing_the_handshake_decision_reproduces_the_golden_for_that_choice(string choice)
    {
        var bundle = Repo.Load("spoke-b");
        var log = Golden("spoke-b", "approach1-as-written");
        var ratify = IndexOf(log, a => a is ResolveCommitmentAction { Choice: "ratify" });
        var r = Counterfactual.Run(bundle, log, CounterfactualChange.Replace(ratify, new ResolveCommitmentAction { Commitment = "uc1", Choice = choice }));

        Assert.Equal(Replay.Run(bundle, log).StateHash(), r.Original.StateHash());
        Assert.Equal(Replay.Run(bundle, Golden("spoke-b", "approach1-" + choice)).StateHash(), r.Alternative.StateHash());
        Assert.Equal(12, r.View.FromDay);
        Assert.Equal($"Day 12: {choice} uc1 instead of ratify uc1.", r.View.ChangeText);
        Assert.Equal(r.View.Alternative.MissionOutput - r.View.Original.MissionOutput, r.View.MissionOutputDelta, 9);
        Assert.All(r.View.Gaps.Where(g => g.EndDay < 12), g => Assert.Equal(GapChange.Unchanged, g.Change));
    }

    [Theory]
    [InlineData("approach1-cure")]
    [InlineData("approach1-terminate")]
    public void Adding_the_day_43_decision_reproduces_its_golden_and_removing_it_undoes_it(string name)
    {
        var bundle = Repo.Load("spoke-b-90");
        var asWritten = Golden("spoke-b-90", "approach1-as-written");
        var withDecision = Golden("spoke-b-90", name);
        static string J(PlayerAction a) => System.Text.Json.JsonSerializer.Serialize(a, LinesOfSupport.Sim.Data.Json.Options);
        var known = asWritten.Actions.Select(J).ToHashSet();
        var addedAt = withDecision.Actions.FindIndex(a => !known.Contains(J(a)));
        Assert.Single(withDecision.Actions, a => !known.Contains(J(a)));
        var added = withDecision.Actions[addedAt];

        var insert = Counterfactual.Run(bundle, asWritten, CounterfactualChange.Insert(added.Day, added));
        Assert.Equal(Replay.Run(bundle, withDecision).StateHash(), insert.Alternative.StateHash());
        Assert.Equal(43, insert.View.FromDay);

        var remove = Counterfactual.Run(bundle, withDecision, CounterfactualChange.Remove(addedAt));
        Assert.Equal(Replay.Run(bundle, asWritten).StateHash(), remove.Alternative.StateHash());
        Assert.Equal(-insert.View.MissionOutputDelta, remove.View.MissionOutputDelta, 9);
    }

    [Fact]
    public void Spec_5_story_hub_fuel_coordination_on_day_3_removes_the_day_10_gap_and_shrinks_day_15()
    {
        var bundle = Repo.Load("spoke-b");
        var log = Golden("spoke-b", "approach1-as-written");
        var r = Counterfactual.Run(bundle, log, CounterfactualChange.Insert(3,
            new CoordinateAction { Id = "hubFuelRun", Site = "spokeBSite", Capability = "fuel", Quantity = 900, FromSite = "hubSite" }));

        GapDiff Gap(string cap, int start) => r.View.Gaps.Single(g => g.SiteId == "spokeBSite" && g.CapabilityId == cap && g.StartDay == start && g.Change != GapChange.New);
        Assert.Equal(GapChange.Gone, Gap("fuel", 10).Change);
        Assert.Equal(GapChange.Gone, Gap("power", 10).Change);
        Assert.Equal(GapChange.Shrunk, Gap("fuel", 15).Change);
        Assert.Equal(3, Gap("fuel", 10).OriginalDownDays);
        Assert.Equal(0, Gap("fuel", 10).AlternativeDownDays);
        // No day 10 outage, so no day 11 handshake: the logged day 12 ratification no longer applies, and the view says so.
        Assert.Contains(r.View.NoLongerApplied, s => s.StartsWith("Day 12: ratify uc1", StringComparison.Ordinal));
        // The withdrawn coordination brings new later fuel gaps; the diff lists them rather than hiding them.
        Assert.Contains(r.View.Gaps, g => g.Change == GapChange.New && g.CapabilityId == "fuel");
    }

    [Fact]
    public void A_line_switched_on_before_the_award_breaks_its_scope_and_the_view_names_it()
    {
        var bundle = Repo.Load("spoke-b");
        var log = Golden("spoke-b", "approach1-as-written");
        var gens = IndexOf(log, a => a is AwardAction { Id: "gens" });
        var before = Counterfactual.Run(bundle, log, CounterfactualChange.Insert(1, new SetLineAction { Card = "req0007", Line = "fuelSupply", On = true }, gens));
        Assert.Contains(before.View.NoLongerApplied, s => s.StartsWith("Day 1: bpaCall gens: ", StringComparison.Ordinal));
        Assert.True(before.View.MissionOutputDelta < 0);

        // After the award, the same line changes nothing about the award.
        var after = Counterfactual.Run(bundle, log, CounterfactualChange.Insert(1, new SetLineAction { Card = "req0007", Line = "fuelSupply", On = true }));
        Assert.DoesNotContain(after.View.NoLongerApplied, s => s.Contains("gens", StringComparison.Ordinal));
        Assert.Throws<ArgumentException>(() => Counterfactual.Run(bundle, log, CounterfactualChange.Insert(2, new SurveyAction { Island = "spokeB" }, gens)));
    }

    [Fact]
    public void Same_prep_changes_nothing()
    {
        var bundle = Repo.Load("spoke-b");
        var log = Golden("spoke-b", "approach2-fix-scope-and-bridge");
        var r = Counterfactual.Run(bundle, log, CounterfactualChange.SetPrep(log.PrepChoices));
        Assert.Equal(r.Original.StateHash(), r.Alternative.StateHash());
        Assert.Equal(0, r.View.MissionOutputDelta);
        Assert.All(r.View.Gaps, g => Assert.Equal(GapChange.Unchanged, g.Change));
        Assert.Empty(r.View.NoLongerApplied);
        Assert.Equal(1, r.View.FromDay);
    }

    [Fact]
    public void A_mid_game_save_is_compared_over_the_days_played()
    {
        var bundle = Repo.Load("spoke-b-90");
        var log = Golden("spoke-b-90", "approach1-as-written");
        var live = Replay.Run(bundle, log, stopAfterDay: 19);
        var save = live.ToSave();
        Assert.Equal(20, save.SavedDay);
        var r = Counterfactual.Run(bundle, save, CounterfactualChange.Insert(5, new SurveyAction { Island = "spokeB" }));
        Assert.Equal(19, r.View.HorizonDay);
        Assert.Equal(20, r.Alternative.Day);
        Assert.Equal(live.StateHash(), r.Original.StateHash());
        Assert.All(r.View.Gaps, g => Assert.True(g.StartDay <= 19));
        Assert.Throws<ArgumentException>(() => Counterfactual.Run(bundle, save, CounterfactualChange.Insert(25, new SurveyAction { Island = "spokeB" })));
    }

    [Fact]
    public void Choices_list_every_logged_action_with_its_rejection()
    {
        var bundle = Repo.Load("spoke-b");
        var log = Golden("spoke-b", "approach1-as-written");
        var bad = log with { Actions = [.. log.Actions, new SurveyAction { Day = 20, Island = "nowhere" }] };
        var played = Replay.Run(bundle, bad);
        var choices = Counterfactual.Choices(played);
        Assert.Equal(bad.Actions.Count, choices.Count);
        Assert.Equal(Enumerable.Range(0, choices.Count), choices.Select(c => c.ActionIndex));
        Assert.True(choices[^1].Rejected);
        Assert.Equal("survey", choices[^1].Kind);
        Assert.All(choices.Take(choices.Count - 1), c => Assert.False(c.Rejected));
        Assert.Throws<ArgumentOutOfRangeException>(() => Counterfactual.Run(bundle, log, CounterfactualChange.Remove(log.Actions.Count)));
    }

    [Fact]
    public void A_90_day_counterfactual_runs_in_well_under_two_seconds()
    {
        var bundle = Repo.Load("spoke-b-90");
        var log = Replay.Run(bundle, 1, PolicyCatalog.Create("textbook")).ToInputLog();
        Counterfactual.Run(bundle, log, CounterfactualChange.Remove(0)); // warm up the JIT
        var sw = Stopwatch.StartNew();
        var r = Counterfactual.Run(bundle, log, CounterfactualChange.Remove(log.Actions.Count / 2));
        sw.Stop();
        Assert.True(r.Alternative.Finished);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(2), $"took {sw.Elapsed.TotalMilliseconds:F0} ms");
    }
}
