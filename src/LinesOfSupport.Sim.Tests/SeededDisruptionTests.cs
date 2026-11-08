using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Policies;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>
/// M2-19: event chains 2 and 3 on Spoke B, both seeded. (a) Weather closes the Third Island ferry for 2 to 4 days
/// inside a window, with a forecast a day ahead only when Third Island has been surveyed; Coastal Provisions stocks
/// food from Main Island, so its deliveries ride that ferry. (b) A follow-on element lifts Spoke C's population by
/// about 40% on 3 to 5 days' notice.
/// </summary>
public class SeededDisruptionTests
{
    private static readonly ScenarioBundle Bundle = Repo.Load("spoke-b");

    private static Session Run(long seed, string policy) => Replay.Run(Bundle, seed, PolicyCatalog.Create(policy));

    private static CauseRecord EventRecord(Session s, string id) => s.Records.Single(r => r.Kind == RecordKind.Event && r.Subject == id);

    private static CauseRecord JumpRecord(Session s) => s.Records.Single(r => r.Kind == RecordKind.Event && r.Subject == "spokeCSite");

    [Fact]
    public void Five_seeds_give_visibly_different_days()
    {
        var ferry = new HashSet<int>();
        var jump = new HashSet<int>();
        foreach (var seed in new long[] { 1, 2, 3, 4, 5 })
        {
            var s = Run(seed, "do-nothing");
            ferry.Add(EventRecord(s, "ferryWeather").Day);
            jump.Add(EventRecord(s, "spokeCFollowOn").Day);
        }
        Assert.True(ferry.Count >= 4, $"ferry days: {string.Join(",", ferry)}");
        Assert.True(jump.Count >= 4, $"follow-on days: {string.Join(",", jump)}");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(12)]
    [InlineData(77)]
    public void Ferry_closes_two_to_four_days_inside_its_window(long seed)
    {
        var s = Run(seed, "do-nothing");
        var start = EventRecord(s, "ferryWeather").Day;
        Assert.InRange(start, 8, 24);
        // LinkOpen answers for today and later; the Sim never asks about a day before a closure began.
        var closed = Enumerable.Range(start, s.LastDay - start + 1).Where(d => !s.LinkOpen("ferry", d)).ToList();
        Assert.InRange(closed.Count, 2, 4);
        Assert.Equal(start, closed[0]);
        Assert.Equal(Enumerable.Range(start, closed.Count), closed);
        Assert.True(Enumerable.Range(16, s.LastDay - 15).All(d => s.LinkOpen("barge", d) || d is >= 16 and <= 18), "the ferry event must not touch the barge");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(12)]
    public void Forecast_comes_a_day_ahead_only_after_a_survey(long seed)
    {
        var surveyed = Run(seed, "textbook");   // surveys every island on day 1
        var closure = EventRecord(surveyed, "ferryWeather").Day;
        var forecast = surveyed.Records.Single(r => r.Kind == RecordKind.Knowledge && r.Subject == "ferryWeather");
        Assert.Equal(closure - 1, forecast.Day);
        Assert.Contains($"days {closure} to", forecast.Text);

        var blind = Run(seed, "do-nothing");    // never surveys
        Assert.DoesNotContain(blind.Records, r => r.Kind == RecordKind.Knowledge && r.Subject == "ferryWeather");
        Assert.Equal(closure, EventRecord(blind, "ferryWeather").Day);   // same seed, same day, whatever the player does
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(9)]
    public void Follow_on_element_adds_about_40_percent_on_three_to_five_days_notice(long seed)
    {
        var s = Run(seed, "do-nothing");
        var notice = JumpRecord(s);
        Assert.Equal(EventRecord(s, "spokeCFollowOn").Day, notice.Day);
        var arrival = Enumerable.Range(notice.Day, 10).First(d => s.View.Population("spokeCSite", d) > s.View.PlannedPopulation("spokeCSite", d));
        Assert.InRange(arrival - notice.Day, 3, 5);
        var planned = s.View.PlannedPopulation("spokeCSite", arrival);
        var actual = s.View.Population("spokeCSite", arrival);
        Assert.Equal(planned * 4 / 10, actual - planned);
        Assert.Equal(s.View.PlannedPopulation("spokeCSite", arrival - 1), s.View.Population("spokeCSite", arrival - 1));
        Assert.Contains($"arrive day {arrival}", notice.Text);
    }

    [Fact]
    public void An_unplanned_arrival_is_not_known_before_its_notice()
    {
        var s = Session.Create(Bundle, 1);
        var noticeDay = Run(1, "do-nothing").Records.Single(r => r.Kind == RecordKind.Event && r.Subject == "spokeCSite").Day;
        while (s.Day < noticeDay)
        {
            for (var d = s.Day; d <= s.LastDay; d++) Assert.Equal(s.View.PlannedPopulation("spokeCSite", d), s.View.Population("spokeCSite", d));
            s.AdvanceDay();
        }
        s.AdvanceDay();
        Assert.Contains(Enumerable.Range(s.Day, s.LastDay - s.Day + 1), d => s.View.Population("spokeCSite", d) > s.View.PlannedPopulation("spokeCSite", d));
    }

    [Fact]
    public void Coastal_Provisions_food_ships_from_the_hub_and_the_survey_says_so()
    {
        var s = Run(1, "textbook");
        var survey = s.Records.Single(r => r.Kind == RecordKind.Knowledge && r.Subject == "survey spokeC");
        Assert.Contains("Coastal Provisions stocks food service from Main Island", survey.Text);

        var g = Session.Create(Bundle, 1, PolicyCatalog.Create("textbook").PrepChoices(Bundle));
        while (g.Day < 3) g.AdvanceDay();
        var award = new AwardAction { Id = "cp-food", Instrument = "sf44", Vendor = "vendorG", Site = "spokeCSite", Capability = "food", Quantity = 100, DurationDays = 5 };
        var result = g.Apply(award);
        Assert.True(result.Accepted, result.Message);
        Assert.Equal("hub", g.Arrangements.Single(a => a.Id == "cp-food").SupplierIslandId);
        var water = new AwardAction { Id = "cp-water", Instrument = "sf44", Vendor = "vendorG", Site = "spokeCSite", Capability = "water", Quantity = 100, DurationDays = 5 };
        Assert.True(g.Apply(water).Accepted);
        Assert.Equal("spokeC", g.Arrangements.Single(a => a.Id == "cp-water").SupplierIslandId);
    }

    [Fact]
    public void Jump_gaps_tag_unforeseeable_at_first_and_avoidable_a_week_on()
    {
        // Approach 3 sizes every buy to the plan and never resizes, so across seeds some follow-on elements land
        // early enough for the shortfall to outlast the react window.
        var log = InputLog.Load(Path.Combine(Repo.Scenario("spoke-b"), "golden", "approach3-uca-turnkey-with-bridge.json"));
        var factors = Enumerable.Range(0, 40).SelectMany(seed => Replay.Run(Bundle, log with { Seed = seed }).View.Debrief().Gaps)
                                .SelectMany(g => g.Factors).Where(f => f.Kind == GapFactorKind.PopulationJump).ToList();
        Assert.Contains(factors, f => f.Tag == GapTag.Unforeseeable && f.Text.Contains("Nothing before the notice"));
        Assert.Contains(factors, f => f.Tag == GapTag.Avoidable && f.Text.Contains("left time to resize"));
    }

    [Fact]
    public void Off_island_factor_cites_only_closures_on_its_own_route()
    {
        var d = Replay.Run(Bundle, InputLog.Load(Path.Combine(Repo.Scenario("spoke-b"), "golden", "approach1-as-written.json"))).View.Debrief();
        var spokeB = d.Gaps.Where(g => g.SiteId == "spokeBSite").SelectMany(g => g.Factors).Where(f => f.Kind == GapFactorKind.OffIslandDependency);
        Assert.All(spokeB, f => Assert.DoesNotContain("Third Island crossing", f.Text));
    }

    [Fact]
    public void Same_seed_same_disruptions()
    {
        Assert.Equal(Run(5, "textbook").StateHash(), Run(5, "textbook").StateHash());
        Assert.NotEqual(Run(5, "do-nothing").StateHash(), Run(6, "do-nothing").StateHash());
    }
}
