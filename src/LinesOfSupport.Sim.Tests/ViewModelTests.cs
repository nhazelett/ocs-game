using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Policies;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>M0-07: the view-model contracts are read-only, and Preview tells the truth about Apply.</summary>
public class ViewModelTests
{
    private static readonly string[] Goldens =
    [
        "approach1-as-written", "approach2-fix-scope-and-bridge", "approach3-uca-turnkey-with-bridge", "approach4-enduring-only",
    ];

    private static InputLog Golden(string scenario, string name) => InputLog.Load(Path.Combine(Repo.Scenario(scenario), "golden", name + ".json"));

    /// <summary>Builds every view the desk could ask for today, plus a preview of each pending award.</summary>
    private static void TouchEveryView(Session s, IEnumerable<PlayerAction> pending)
    {
        var v = s.View;
        _ = v.TopStrip();
        _ = v.MorningBoard();
        foreach (var c in v.Cards)
        {
            _ = v.RequirementEditor(c.Id);
            _ = v.ActionEditor(c.SiteId, c.CapabilityId, c.Id);
        }
        foreach (var site in s.Scenario.Sites)
            foreach (var cap in s.Rules.Model.Capabilities)
                _ = v.ActionEditor(site.Id, cap.Id);
        foreach (var a in pending.OfType<AwardAction>()) _ = v.Preview(a);
    }

    [Theory]
    [InlineData("spoke-b")]
    [InlineData("spoke-b-90")]
    public void Building_views_never_changes_the_game(string scenario)
    {
        var bundle = Repo.Load(scenario);
        foreach (var seed in new long[] { 1, 2, 3, 17, 99 })
        {
            var plain = Replay.Run(bundle, seed, PolicyCatalog.Create("textbook"));

            var watched = Session.Create(bundle, seed, PolicyCatalog.Create("textbook").PrepChoices(bundle));
            var policy = PolicyCatalog.Create("textbook");
            while (!watched.Finished)
            {
                var today = policy.Decide(watched.View).ToList();
                TouchEveryView(watched, today);
                foreach (var a in today) watched.Apply(a);
                TouchEveryView(watched, []);
                watched.AdvanceDay();
            }
            Assert.Equal(plain.StateHash(), watched.StateHash());
        }
    }

    [Theory]
    [InlineData("spoke-b")]
    [InlineData("spoke-b-90")]
    public void Preview_agrees_with_apply_on_every_golden_award(string scenario)
    {
        var bundle = Repo.Load(scenario);
        var checkedAwards = 0;
        foreach (var name in Goldens)
        {
            var path = Path.Combine(Repo.Scenario(scenario), "golden", name + ".json");
            if (!File.Exists(path)) continue;
            var log = InputLog.Load(path);
            var s = Session.Create(bundle, log.Seed, log.PrepChoices);
            var byDay = log.Actions.GroupBy(a => a.Day).ToDictionary(g => g.Key, g => g.ToList());
            while (!s.Finished)
            {
                if (byDay.TryGetValue(s.Day, out var today))
                    foreach (var a in today)
                    {
                        if (a is AwardAction award)
                        {
                            var p = s.View.Preview(award);
                            var r = s.Apply(a);
                            Assert.Equal(r.Accepted, p.Accepted);
                            Assert.Equal(r.HardStops, p.HardStops);
                            checkedAwards++;
                        }
                        else s.Apply(a);
                    }
                s.AdvanceDay();
            }
        }
        Assert.True(checkedAwards >= 4, $"only {checkedAwards} awards checked");
    }

    [Fact]
    public void Preview_reports_hard_stops_for_a_bad_award_and_changes_nothing()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1);
        var before = s.StateHash();
        var p = s.View.Preview(new AwardAction { Id = "x1", Instrument = "sf44", Vendor = "nobody", Card = "req0007", Quantity = 1, DurationDays = 5 });
        Assert.False(p.Accepted);
        Assert.NotEmpty(p.HardStops);
        Assert.Equal(before, s.StateHash());
        Assert.DoesNotContain(s.Records, r => r.Kind == RecordKind.Rejected);
    }

    [Fact]
    public void Requirement_editor_shows_the_spoke_b_card_as_written()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1);
        var ed = s.View.RequirementEditor("req0007");
        Assert.NotNull(ed);
        Assert.Equal("power", ed!.CapabilityId);
        Assert.True(ed.Lines.Single(l => l.Id == "generation").On);
        Assert.Null(ed.Lines.Single(l => l.Id == "generation").GapIfOff);
        foreach (var id in new[] { "fuelSupply", "operator", "maintenance" })
        {
            var line = ed.Lines.Single(l => l.Id == id);
            Assert.False(line.On);
            Assert.False(string.IsNullOrWhiteSpace(line.GapIfOff));
        }
        var fuel = ed.Dependencies.Single(d => d.CapabilityId == "fuel");
        Assert.Equal("none", fuel.Coverage);
        Assert.True(ed.PeakDemandUnits > 0);

        s.Apply(new SetLineAction { Card = "req0007", Line = "fuelSupply", On = true });
        Assert.Equal("inRequirement", s.View.RequirementEditor("req0007")!.Dependencies.Single(d => d.CapabilityId == "fuel").Coverage);
        Assert.Null(s.View.RequirementEditor("no-such-card"));
    }

    [Fact]
    public void Action_editor_lists_vendors_instruments_and_funding()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1);
        var ed = s.View.ActionEditor("spokeBSite", "power", "req0007");
        Assert.NotNull(ed);
        Assert.NotEmpty(ed!.Vendors);
        Assert.All(ed.Vendors, v => Assert.True(v.CapacityFreeToday <= v.CapacityPerDay));
        Assert.Contains(ed.Instruments, i => i.Id == "uca");
        Assert.Contains(ed.Funding, f => f.UsableToday);
        Assert.Equal(["generation"], ed.SelectedLines);
        Assert.Null(s.View.ActionEditor("nowhere", "power"));
    }

    [Fact]
    public void Morning_board_is_empty_on_day_one_and_reports_overnight_changes_alerts_first()
    {
        var bundle = Repo.Load("spoke-b");
        var log = Golden("spoke-b", "approach1-as-written");
        var s = Session.Create(bundle, log.Seed, log.PrepChoices);
        Assert.Empty(s.View.MorningBoard().Items);

        var byDay = log.Actions.GroupBy(a => a.Day).ToDictionary(g => g.Key, g => g.ToList());
        var sawItems = false;
        var sawAlert = false;
        while (!s.Finished)
        {
            if (byDay.TryGetValue(s.Day, out var today)) foreach (var a in today) s.Apply(a);
            var playedDay = s.Day;
            s.AdvanceDay();
            var board = s.View.MorningBoard();
            Assert.All(board.Items, i => Assert.Equal(playedDay, i.Day));
            Assert.DoesNotContain(board.Items, i => i.Text.Length == 0);
            var sev = board.Items.Select(i => (int)i.Severity).ToList();
            Assert.Equal(sev.OrderByDescending(x => x).ToList(), sev);
            sawItems |= board.Items.Count > 0;
            sawAlert |= board.Items.Any(i => i.Severity == BoardSeverity.Alert && i.LinkKind != BoardLinkKind.None);
            Assert.Equal(s.Capabilities.Count, board.Tiles.Count);
        }
        Assert.True(sawItems);
        Assert.True(sawAlert, "approach 1 should produce at least one linked alert (the day-10 power shortfall chain)");
    }

    [Fact]
    public void Top_strip_gauges_stay_inside_their_ranges()
    {
        var s = Replay.Run(Repo.Load("spoke-b"), 1, PolicyCatalog.Create("textbook"));
        var t = s.View.TopStrip();
        Assert.True(t.ShopHoursPerDay > 0);
        Assert.InRange(t.CashCents, 0, t.CashFullCents);
        Assert.True(t.QueueDays >= 0);
    }
}
