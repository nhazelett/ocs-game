using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Policies;

namespace LinesOfSupport.Sim.Tests;

/// <summary>
/// DESK-61 (Claude play 16 Nov, Sim pass): the tutorial's textbook policy plays the tutorial right. Before, it bought the
/// forklifts and the waste on UCAs (a short UCA on a small buy stops work at its obligation cap before a proposal comes
/// in) and missed two pass criteria in every seed, and greedy beat it. Nobody called the BPA, so declines were never
/// measured.
/// </summary>
public class TutorialPoliciesTests
{
    private static bool Met(IReadOnlyDictionary<string, double> m, string metric, string comparator, double value)
        => m.TryGetValue(metric, out var v) && comparator switch
        {
            "<=" => v <= value, ">=" => v >= value, "==" => v == value, "<" => v < value, ">" => v > value, _ => false,
        };

    [Fact]
    public void Textbook_meets_every_tutorial_pass_criterion_in_every_seed_and_beats_greedy()
    {
        var bundle = Repo.Load("tutorial");
        var textbook = new List<double>();
        var greedy = new List<double>();
        for (var seed = 0; seed < 200; seed++)
        {
            var t = Replay.Run(bundle, seed, PolicyCatalog.Create("textbook"));
            foreach (var p in bundle.Scenario.PassCriteria)
                Assert.True(Met(t.Metrics, p.Metric, p.Comparator, p.Value), $"seed {seed}: textbook missed {p.Label}");
            textbook.Add(t.Metrics["mission.output"]);
            var g = Replay.Run(bundle, seed, PolicyCatalog.Create("greedy"));
            greedy.Add(g.Metrics["mission.output"]);
            Assert.True(g.Metrics["work.bpaDeclines"] >= 1, $"seed {seed}: greedy never had a call declined");
        }
        static double Median(List<double> x) { x.Sort(); return (x[99] + x[100]) / 2; }
        Assert.True(Median(textbook) > Median(greedy), $"textbook {Median(textbook)} against greedy {Median(greedy)}");
    }

    [Fact]
    public void Greedy_calls_the_BPA_first_and_buys_elsewhere_when_it_is_declined()
    {
        var s = Replay.Run(Repo.Load("tutorial"), 1, PolicyCatalog.Create("greedy"));
        var forklifts = s.Arrangements.Where(a => a.CardId == "tutForklift").OrderBy(a => a.CreatedDay).ToList();
        Assert.Equal("bpaCall", forklifts[0].Instrument);
        Assert.Equal(ArrangementStatus.Declined, forklifts[0].Status);
        Assert.Contains(forklifts.Skip(1), a => a.VendorId != forklifts[0].VendorId && a.Status != ArrangementStatus.Declined);
    }
}
