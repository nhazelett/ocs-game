using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>M2R-09 (OBJ-0004): the map draws what the Sim says moved.</summary>
public class MapDayTests
{
    private static Session Approach1(int stopAfterDay)
        => Replay.Run(Repo.Load("spoke-b"), InputLog.Load(Path.Combine(Repo.Scenario("spoke-b"), "golden", "approach1-as-written.json")), stopAfterDay: stopAfterDay);

    [Fact]
    public void Before_any_day_the_map_is_empty_and_links_are_listed()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1);
        var m = s.View.MapDay();
        Assert.Equal(0, m.Day);
        Assert.Equal(s.Scenario.TransportLinks.Count, m.Links.Count);
        Assert.Empty(m.Runs); Assert.Empty(m.Held); Assert.Empty(m.Changes);
    }

    [Fact]
    public void A_barge_day_shows_the_run_and_a_closed_day_shows_cargo_held()
    {
        // Approach 1 ratifies a Main Island fuel vendor on day 12; fuel then crosses the barge and waits on it when the weather closes it around day 17.
        var seenRun = false; var seenHeld = false;
        for (var d = 8; d <= 20; d++)
        {
            var s = Approach1(d);
            var m = s.View.MapDay();
            Assert.Equal(d, m.Day);
            var barge = m.Links.First(l => l.Mode == "barge" && (l.ToIslandId == "spokeB" || l.FromIslandId == "spokeB"));
            foreach (var r in m.Runs)
            {
                Assert.NotEqual("", r.LinkId);
                Assert.True(m.Links.First(l => l.Id == r.LinkId).Open, "a run on a closed link");
                Assert.True(m.Links.First(l => l.Id == r.LinkId).ScheduledToday, "a run on an unscheduled day");
                Assert.True(r.Quantity > 0);
            }
            if (m.Runs.Any(r => r.CapabilityId == "fuel" && r.SiteId == "spokeBSite")) seenRun = true;
            if (!barge.Open)
            {
                Assert.NotNull(barge.ClosureRecordId);
                Assert.DoesNotContain(m.Runs, r => r.LinkId == barge.Id);
                if (m.Held.Any(h => h.Reason == "closed" && h.LinkId == barge.Id)) seenHeld = true;
            }
        }
        Assert.True(seenRun, "no fuel run over the barge");
        Assert.True(seenHeld, "no cargo held by the closed barge");
    }

    [Fact]
    public void Changes_carry_known_flags_and_match_the_records()
    {
        var s = Approach1(10);
        var m = s.View.MapDay();
        // Day 10 is the first Spoke B shortfall in the as-written story.
        Assert.Contains(m.Changes, c => c.Kind == "shortfall" && c.SiteId == "spokeBSite");
        foreach (var c in m.Changes) Assert.Contains(s.Records, r => r.Id == c.RecordId);
    }

    [Fact]
    public void Building_the_map_changes_nothing()
    {
        var s = Approach1(12);
        var before = s.StateHash();
        _ = s.View.MapDay(); _ = s.View.MapDay();
        Assert.Equal(before, s.StateHash());
    }
}
