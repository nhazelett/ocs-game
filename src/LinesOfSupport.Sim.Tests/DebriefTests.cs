using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Policies;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>M2-08: the debrief tags the seeded Spoke B gaps the way SPEC 5 describes them.</summary>
public class DebriefTests
{
    private static DebriefView Debrief(string scenario, string approach)
        => Replay.Run(Repo.Load(scenario), InputLog.Load(Path.Combine(Repo.Scenario(scenario), "golden", approach + ".json"))).View.Debrief();

    private static GapView GapOn(DebriefView d, string siteCap, int day)
        => d.Gaps.Single(g => $"{g.SiteId}.{g.CapabilityId}" == siteCap && g.StartDay <= day && day <= g.EndDay);

    [Theory]
    [InlineData("spoke-b")]
    [InlineData("spoke-b-90")]
    public void Day_10_gap_is_avoidable_because_the_fuel_line_was_missing(string scenario)
    {
        var d = Debrief(scenario, "approach1-as-written");
        var fuel = GapOn(d, "spokeBSite.fuel", 10);
        Assert.Equal(GapTag.Avoidable, fuel.Tag);
        Assert.Contains(fuel.Factors, f => f.Kind == GapFactorKind.MissingSupplyLine && f.Tag == GapTag.Avoidable && f.Text.Contains("dependency checklist"));
        var power = GapOn(d, "spokeBSite.power", 10);
        Assert.Equal(GapTag.Avoidable, power.Tag);
        Assert.Contains(power.Factors, f => f.Kind == GapFactorKind.Dependency && f.DependsOnGap == fuel.Index);
    }

    [Theory]
    [InlineData("spoke-b")]
    [InlineData("spoke-b-90")]
    public void Day_16_gap_is_discoverable_because_no_survey_showed_the_barge_dependency(string scenario)
    {
        var fuel = GapOn(Debrief(scenario, "approach1-as-written"), "spokeBSite.fuel", 16);
        Assert.Equal(GapTag.Discoverable, fuel.Tag);
        Assert.Contains(fuel.Factors, f => f.Kind == GapFactorKind.OffIslandDependency && f.Text.Contains("A survey"));
    }

    [Fact]
    public void The_same_barge_gap_is_avoidable_once_a_survey_has_shown_it()
    {
        var d = Debrief("spoke-b", "approach2-fix-scope-and-bridge");
        var off = d.Gaps.SelectMany(g => g.Factors).Where(f => f.Kind == GapFactorKind.OffIslandDependency).ToList();
        Assert.NotEmpty(off);
        Assert.All(off, f => Assert.Equal(GapTag.Avoidable, f.Tag));
    }

    [Fact]
    public void Coordination_treated_as_a_contract_is_avoidable()
    {
        var d = Debrief("spoke-b", "approach2-fix-scope-and-bridge");
        var fuel = d.Gaps.First(g => g.CapabilityId == "fuel" && g.Factors.Any(f => f.Kind == GapFactorKind.CoordinationWithdrawn));
        Assert.Equal(GapTag.Avoidable, fuel.Tag);
        Assert.True(fuel.StartDay >= 12);
    }

    [Fact]
    public void Consumable_bought_for_opening_population_names_demand_above_supply()
    {
        // Greedy buys Spoke C bottled water at 1,800 gal a day for 120 people; on day 15 the site grows to 160.
        var d = Replay.Run(Repo.Load("spoke-b"), 1, PolicyCatalog.Create("greedy")).View.Debrief();
        var water = GapOn(d, "spokeCSite.water", 15);
        Assert.Equal(GapTag.Avoidable, water.Tag);
        Assert.Contains(water.Factors, f => f.Kind == GapFactorKind.DemandAboveSupply
                                            && f.Text.Contains("1,800 gal a day against demand of 2,400 a day")
                                            && f.Text.Contains("day 15"));
    }

    [Theory]
    [InlineData("approach1-as-written")]
    [InlineData("approach2-fix-scope-and-bridge")]
    public void Day_20_population_growth_is_avoidable(string approach)
    {
        var power = GapOn(Debrief("spoke-b", approach), "spokeBSite.power", 20);
        Assert.Contains(power.Factors, f => f.Kind == GapFactorKind.DemandAboveSupply && f.Tag == GapTag.Avoidable && f.FirstDay == 20 && f.Text.Contains("plan"));
    }

    [Fact]
    public void Approach_3_has_no_gaps_and_approach_4_is_late_not_unlucky()
    {
        // Approach 3 covers every planned need. With the team (DESK-12) one gap at seed 1 is a day of the turnkey
        // package's equipment down with full operator and maintenance coverage, tagged unforeseeable. Since DESK-81 the
        // Harbor bridge it never ends has nobody to run it after ten days and stops on day 19, the same day Meridian has a
        // set down: discoverable (Capt Ruiz knew), never avoidable, and nothing at Spoke B is ever down.
        var gaps = Debrief("spoke-b", "approach3-uca-turnkey-with-bridge").Gaps;
        Assert.NotEmpty(gaps);
        Assert.All(gaps, g => Assert.NotEqual(GapTag.Avoidable, g.Tag));
        Assert.All(gaps, g => Assert.Equal(0, g.DownDays));
        Assert.Contains(gaps, g => g.Tag == GapTag.Discoverable && g.StartDay == 19
                                   && g.Factors.Any(f => f.Text.Contains("Capt Ana Ruiz knew it needed operators")));
        var power = GapOn(Debrief("spoke-b", "approach4-enduring-only"), "spokeBSite.power", 7);
        Assert.Equal(GapTag.Avoidable, power.Tag);
        Assert.Contains(power.Factors, f => f.Kind == GapFactorKind.Uncovered && f.Text.Contains("did not start until"));
    }

    [Fact]
    public void Ucas_left_undefinitized_show_stop_work_as_avoidable()
    {
        // DESK-12: with the team the UCA lands a day later, so an earlier one-day fuel dip comes first; the stop work is
        // on the power gap that follows it.
        var power = Debrief("spoke-b-90", "approach3b-uca-never-definitized").Gaps.Where(g => g.CapabilityId == "power");
        Assert.Contains(power.SelectMany(g => g.Factors), f => f.Kind == GapFactorKind.StopWork && f.Tag == GapTag.Avoidable && f.Text.Contains("past due"));
    }

    [Fact]
    public void A_parked_handshake_that_walks_off_is_avoidable()
    {
        var d = Debrief("spoke-b", "approach1-park");
        Assert.Contains(d.Gaps.SelectMany(g => g.Factors), f => f.Kind == GapFactorKind.HandshakeLost && f.Tag == GapTag.Avoidable);
        Assert.Contains(d.Commitments, c => c.Id == "uc1" && c.WalkedOff && c.Decision == "parked");
    }

    [Fact]
    public void Timeline_bands_scorecard_and_decisions_are_consistent()
    {
        var s = Replay.Run(Repo.Load("spoke-b"), InputLog.Load(Path.Combine(Repo.Scenario("spoke-b"), "golden", "approach1-as-written.json")));
        var d = s.View.Debrief();
        Assert.Equal(s.LastDay, d.DaysPlayed);
        var power = d.Timeline.Single(b => b.SiteId == "spokeBSite" && b.CapabilityId == "power");
        Assert.Equal(s.LastDay, power.PermilleByDay.Count);
        Assert.All(power.PermilleByDay.Take(6), p => Assert.Equal(-1, p));
        Assert.Equal(0, power.PermilleByDay[9]);
        Assert.Equal(d.Gaps.Count, d.Scorecard.AvoidableGaps + d.Scorecard.DiscoverableGaps + d.Scorecard.UnforeseeableGaps);
        Assert.Contains(d.Decisions, x => x.Day == 1 && x.Known.Any(k => k.IslandId == "spokeB" && k.Tier != "surveyed"));
        Assert.Contains(d.Events, e => e.Kind == "Event" && e.Subject == "bargeWeather");
    }

    [Fact]
    public void Mid_game_debrief_covers_only_the_days_played()
    {
        var s = Replay.Run(Repo.Load("spoke-b"), InputLog.Load(Path.Combine(Repo.Scenario("spoke-b"), "golden", "approach1-as-written.json")), stopAfterDay: 12);
        var d = s.View.Debrief();
        Assert.Equal(12, d.DaysPlayed);
        Assert.All(d.Gaps, g => Assert.True(g.EndDay <= 12));
        Assert.All(d.Timeline, b => Assert.All(b.PermilleByDay.Skip(12), p => Assert.Equal(-1, p)));
    }
}
