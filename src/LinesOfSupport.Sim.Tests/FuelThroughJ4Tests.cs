using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Policies;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>M2R-06 (design doc R10): Spoke B fuel is the J4's through the ACSA partner; the office buys it only as a stopgap.</summary>
public class FuelThroughJ4Tests
{
    private static Session Golden(string file, int? stopAfterDay = null)
        => Replay.Run(Repo.Load("spoke-b"), InputLog.Load(Path.Combine(Repo.Scenario("spoke-b"), "golden", file)), stopAfterDay: stopAfterDay);

    [Theory]
    [InlineData("spoke-b")]
    [InlineData("spoke-b-90")]
    public void Spoke_B_ships_with_a_partner_and_the_hub_fuel_point_is_the_J4s_ACSA_line(string scenario)
    {
        var s = Session.Create(Repo.Load(scenario), 1);
        Assert.NotNull(s.View.AcsaPartner);
        Assert.True(s.View.AcsaPartner!.Stock["fuel"] > 0);
        var hub = s.Arrangements.Single(a => a.Id == "hubFuelPoint");
        Assert.Equal("acsa", hub.Instrument);
        Assert.Equal(0, hub.DailyCostCents);
        // Someone else's paper: not a file this office reviews.
        Assert.DoesNotContain(s.View.InheritedFiles(), f => f.ArrangementId == "hubFuelPoint");
    }

    [Fact]
    public void The_dependency_checklist_names_the_usual_owner_of_fuel()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1);
        var e = s.View.RequirementEditor("req0007")!;
        var fuel = e.Dependencies.Single(d => d.CapabilityId == "fuel");
        Assert.StartsWith("the J4", fuel.UsualOwner);
        Assert.False(fuel.OwnerEngaged);
        Assert.Equal(fuel.UsualOwner, e.Lines.Single(l => l.Id == "fuelSupply").UsualOwner);
        Assert.Null(e.Lines.Single(l => l.Id == "generation").UsualOwner);
        // Asking the J4 shows on the checklist the same day, before the line exists.
        Assert.True(s.Apply(new EngageJ4Action { Id = "j4", Site = "spokeBSite", Capability = "fuel", Quantity = 1300 }).Accepted);
        Assert.True(s.View.RequirementEditor("req0007")!.Dependencies.Single(d => d.CapabilityId == "fuel").OwnerEngaged);
    }

    [Fact]
    public void Approach_1_day_10_reads_nobody_owned_the_fuel_and_day_16_is_still_the_barge()
    {
        var s = Golden("approach1-as-written.json");
        var gaps = s.View.Debrief().Gaps.Where(g => g.SiteId == "spokeBSite" && g.CapabilityId == "fuel").OrderBy(g => g.StartDay).ToList();
        Assert.True(gaps.Count >= 2);
        var day10 = gaps[0];
        Assert.Equal(10, day10.StartDay);
        Assert.Equal(GapTag.Avoidable, day10.Tag);
        var owner = day10.Factors.Single(f => f.Kind == GapFactorKind.OwnerNotEngaged);
        Assert.Equal(GapTag.Avoidable, owner.Tag);
        Assert.StartsWith("Nobody owned fuel at Spoke B.", owner.Text);
        Assert.Contains("The J4 was never asked", owner.Text);
        // The weather gap has a stopgap running (the ratified handshake), so it is the barge's, not the owner's.
        var day16 = gaps[1];
        Assert.InRange(day16.StartDay, 15, 16);
        Assert.Equal(GapTag.Discoverable, day16.Tag);
        Assert.DoesNotContain(day16.Factors, f => f.Kind == GapFactorKind.OwnerNotEngaged);
    }

    [Fact]
    public void Engaging_the_J4_on_day_1_removes_the_day_10_outage_and_the_handshake()
    {
        var s = Golden("approach1-with-j4.json");
        Assert.DoesNotContain(s.Records, r => r.Kind == RecordKind.Shortfall && r.Subject == "spokeBSite.fuel" && r.Day < 16);
        Assert.DoesNotContain(s.Commitments, c => c.Kind == CommitmentKind.Handshake);
        var fuelGaps = s.View.Debrief().Gaps.Where(g => g.SiteId == "spokeBSite" && g.CapabilityId == "fuel").ToList();
        Assert.All(fuelGaps, g => Assert.DoesNotContain(g.Factors, f => f.Kind == GapFactorKind.OwnerNotEngaged));
        Assert.All(fuelGaps, g => Assert.True(g.StartDay >= 16, "the only fuel gap is the weather closure"));
        // Nothing of it came out of the shop's money.
        Assert.Equal(0, s.Arrangements.Single(a => a.Id == "j4fuel").ObligatedCents);
    }

    [Fact]
    public void The_textbook_engages_the_J4_on_day_1_and_leaves_the_fuel_line_off_its_own_buys()
    {
        var s = Replay.Run(Repo.Load("spoke-b"), 1, PolicyCatalog.Create("textbook"));
        var ask = s.AppliedActions.OfType<EngageJ4Action>().Single(a => a.Site == "spokeBSite" && a.Capability == "fuel");
        Assert.Equal(1, ask.Day);
        var line = s.Arrangements.Single(a => a.Id == ask.Id);
        Assert.Equal("acsa", line.Instrument);
        Assert.True(line.StartDay <= 7, "the J4's fuel is there before Spoke B opens");
        Assert.DoesNotContain(s.AppliedActions.OfType<SetLineAction>(), a => a.Card == "req0007" && a.Line == "fuelSupply");
        Assert.All(s.Arrangements.Where(a => a.CardId == "req0007"), a => Assert.DoesNotContain("fuelSupply", a.Lines));
        Assert.DoesNotContain(s.View.Debrief().Gaps.SelectMany(g => g.Factors), f => f.Kind == GapFactorKind.OwnerNotEngaged);
    }

    [Fact]
    public void Policy_ordering_holds_on_Spoke_B_with_fuel_through_the_J4()
    {
        double Mean(string policy) => Enumerable.Range(0, 40).Average(seed =>
            Replay.Run(Repo.Load("spoke-b"), seed, PolicyCatalog.Create(policy)).Metrics["mission.output"]);
        var textbook = Mean("textbook"); var greedy = Mean("greedy"); var nothing = Mean("do-nothing");
        Assert.True(textbook > greedy + 0.1, $"textbook {textbook:0.000} vs greedy {greedy:0.000}");
        Assert.True(greedy > nothing + 0.05, $"greedy {greedy:0.000} vs do-nothing {nothing:0.000}");
    }
}
