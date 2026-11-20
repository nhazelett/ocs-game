using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Policies;

namespace LinesOfSupport.Sim.Tests;

/// <summary>
/// DESK-81 (gameplay review 17 Nov, section 9): Spoke B prices its lesson. Generators bought without the operators
/// Capt Ruiz names stop, after ten days of the unit covering, on days nobody is there to run them; the textbook answer
/// leaves money for the food order; the staff meeting has something to hear.
/// </summary>
public class SpokeBLessonTests
{
    /// <summary>Textbook, except Spoke B's power stays with the generators-only call for the whole stay.</summary>
    private sealed class EquipmentOnly : IPolicy
    {
        private readonly TextbookPolicy _t = new();
        public string Name => "equipment-only";
        public IEnumerable<string> PrepChoices(ScenarioBundle bundle) => _t.PrepChoices(bundle);
        public IEnumerable<PlayerAction> Decide(SessionView view)
        {
            foreach (var a in _t.Decide(view))
            {
                if (a is AwardAction aw && (aw.Card == "req0007" || aw is { Site: "spokeBSite", Capability: "power" }))
                {
                    if (aw.Vendor == "vendorB") continue;
                    yield return aw with { DurationDays = 60 };
                }
                else yield return a;
            }
        }
    }

    private static bool AllMet(Session s)
    {
        var m = s.Metrics;
        return s.Scenario.PassCriteria.All(p => p.Comparator switch
        {
            "<=" => m[p.Metric] <= p.Value,
            ">=" => m[p.Metric] >= p.Value,
            "==" => Math.Abs(m[p.Metric] - p.Value) < 1e-9,
            _ => throw new InvalidOperationException(p.Comparator),
        });
    }

    [Fact]
    public void Generators_nobody_runs_no_longer_meet_every_line_and_the_textbook_answer_still_does()
    {
        // 200-seed study when built: equipment only met every line in 14 seeds (179 before DESK-81); textbook in 184
        // (184 before). Sixty seeds here keep the test quick.
        var b = Repo.Load("spoke-b");
        var equipment = Enumerable.Range(1, 60).Count(seed => AllMet(Replay.Run(b, seed, new EquipmentOnly())));
        var textbook = Enumerable.Range(1, 60).Count(seed => AllMet(Replay.Run(b, seed, new TextbookPolicy())));
        Assert.InRange(equipment, 0, 12);
        Assert.InRange(textbook, 48, 60);
    }

    [Fact]
    public void A_set_nobody_runs_stops_only_after_the_grace_and_says_why()
    {
        var b = Repo.Load("spoke-b");
        var stops = 0;
        for (var seed = 1; seed <= 40; seed++)
        {
            var s = Replay.Run(b, seed, new EquipmentOnly());
            foreach (var r in s.Records.Where(r => r.Kind == RecordKind.VendorFailure && r.Text.Contains("nobody was there to run it")))
            {
                stops++;
                var a = s.Arrangements.Single(x => x.Id == r.Subject);
                Assert.True(r.Day - a.StartDay + 1 > 10, $"seed {seed}: stopped on day {r.Day}, started {a.StartDay}");
                Assert.Contains("stopped: nobody was there to run it (", r.Text);
                Assert.EndsWith("no operators).", r.Text);
            }
        }
        Assert.True(stops > 40, $"{stops} stops in 40 seeds");
    }

    [Fact]
    public void The_textbook_food_order_is_no_longer_refused_for_money()
    {
        var s = Replay.Run(Repo.Load("spoke-b"), 1, new TextbookPolicy());
        Assert.DoesNotContain(s.Records, r => r.Text.Contains("could not be awarded") && r.Text.Contains("O&M line"));
        Assert.Contains(s.Arrangements, a => a.SiteId == "spokeCSite" && a.CapabilityId == "food" && a.VendorId == "vendorF");
    }
}
