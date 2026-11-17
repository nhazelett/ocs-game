using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>
/// DESK-87 (Claude play 17 Nov, notes 2, 3, 29 and 36): the tutorial says the same thing everywhere. A missed delivery the
/// stock covered says so; the turnover does not call known prices estimates and keeps Mills' voice; Mills names the staff
/// meeting before the request that was raised there reaches the desk.
/// </summary>
public class SaysOneThingTests
{
    [Fact]
    public void The_dining_halls_slips_say_the_stock_covered_them()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, ["cashAndPayingAgent", "systemAccess"]);
        while (s.Day <= 12) s.AdvanceDay();
        var fails = s.Records.Where(r => r.Kind == RecordKind.VendorFailure && r.Subject == "hubDining").ToList();
        Assert.Equal([11, 12], fails.Select(r => r.Day));
        Assert.All(fails, r => Assert.EndsWith("missed today's delivery. The stock on hand covered today's demand.", r.Text));
        var reports = s.Records.Where(r => r.Kind == RecordKind.CorReport && r.Subject == "hubDining").ToList();
        Assert.Equal(2, reports.Count);
        Assert.All(reports, r => Assert.EndsWith("; the stock on hand covered it.", r.Text));
        // And the debrief's grid agrees: food was served in full both days.
        var food = s.View.Debrief().Timeline.Single(b => b.CapabilityId == "food" && b.SiteId == "hubSite");
        Assert.Equal([1000, 1000], new[] { food.PermilleByDay[10], food.PermilleByDay[11] });
    }

    [Fact]
    public void Only_a_day_the_stock_met_in_full_says_the_stock_covered_it()
    {
        // Inherited arrangements in every scenario, played with nothing done: each failure that says the stock covered it
        // falls on a day its site and capability were met in full, and no full day's failure of a consumable leaves it out.
        foreach (var id in new[] { "tutorial", "spoke-b", "sustainment" })
        {
            var bundle = Repo.Load(id);
            var s = Session.Create(bundle, 1, []);
            while (!s.Finished) s.AdvanceDay();
            var bands = s.View.Debrief().Timeline.ToDictionary(b => (b.SiteId, b.CapabilityId));
            foreach (var a in bundle.Scenario.InheritedArrangements)
            {
                if (!bands.TryGetValue((a.Site, a.Capability), out var band)) continue;
                foreach (var r in s.Records.Where(r => r.Kind == RecordKind.VendorFailure && r.Subject == a.Id && r.Text.Contains("missed today's delivery", StringComparison.Ordinal)))
                {
                    var full = band.PermilleByDay[r.Day - 1] >= 1000;
                    Assert.True(full == r.Text.Contains("The stock on hand covered today's demand.", StringComparison.Ordinal), $"{id} day {r.Day}: {r.Text}");
                }
            }
        }
    }

    [Fact]
    public void The_vendor_list_makes_the_turnover_say_the_terms_are_known()
    {
        var with = Session.Create(Repo.Load("tutorial"), 1, ["vendorLists"]).View.Turnover().Text(TurnoverSectionKind.Vendors);
        Assert.Contains("The vendor list you brought has the terms for", with);
        Assert.DoesNotContain("the prices are estimates", with);
        var without = Session.Create(Repo.Load("tutorial"), 1, []).View.Turnover().Text(TurnoverSectionKind.Vendors);
        Assert.Contains("the prices are estimates", without);
    }

    [Fact]
    public void Mills_speaks_for_himself_and_names_the_staff_meeting_before_day_6()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1, []);
        var t = s.View.Turnover();
        var all = string.Join(" ", t.Sections.Select(x => x.Text).Append(t.Briefing!.Situation).Append(t.Briefing.Due));
        Assert.DoesNotContain("the outgoing contracting officer", all);
        Assert.Contains("I'm here for your first three days", all);
        while (s.Day <= 2) s.AdvanceDay();
        Assert.Contains(s.Records, r => r.Day <= 2 && r.Kind == RecordKind.Event && r.Text.Contains("staff meeting", StringComparison.Ordinal));
    }
}
