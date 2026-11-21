using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Policies;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-168 (Claude play 21 Nov, sixth play, note 36). The player cancelled a right-sized card buy for three
/// forklifts because it left out operators, could rebuy only one for the first day, and the debrief said only "Arrangements
/// at Hub were sized for 1 forklifts against demand of 3". (a) The gap names the cancelled buy first, with its records in
/// the evidence; the sizing sentence stays after it. (b) A count agrees with its unit, from one helper.</summary>
public class BuyCutDebriefTests
{
    private static Session SixthPlay()
        => Replay.Run(Repo.Load("tutorial"), InputLog.Load(Path.Combine(Repo.Root, "docs", "reviews", "plays", "2026-11-21-6", "input-log.json")));

    private const string Cut = "The card buy with Coastal Rentals for 3 forklifts from day 8 was cancelled on day 7, before it started; it had left out operators, which SMSgt Ray Dunn said the request needs.";

    [Fact]
    public void The_sixth_play_hears_that_it_cancelled_the_buy_that_would_have_covered_day_8()
    {
        var s = SixthPlay();
        var d = s.View.Debrief();
        var gap = d.Gaps.Single(g => g.CapabilityId == "mhe" && g.StartDay == 8);
        Assert.Equal(GapFactorKind.BuyCut, gap.Factors[0].Kind);
        Assert.Equal(Cut, gap.Factors[0].Text);
        Assert.Equal(GapTag.Avoidable, gap.Factors[0].Tag);
        Assert.Equal("Arrangements at Hub were sized for 1 forklift against demand of 3.", gap.Factors[1].Text);

        // Evidence: the cancelled buy's order, its cancellation, and Dunn's answer that named the part.
        var ev = gap.Factors[0].RecordIds.Select(id => s.Records.Single(r => r.Id == id)).ToList();
        Assert.Contains(ev, r => r.Kind == RecordKind.Action && r.Text.Contains("3 forklifts/day", StringComparison.Ordinal));
        Assert.Contains(ev, r => r.Kind == RecordKind.ArrangementEnd && r.Text.Contains("cancelled with the vendor before its first delivery", StringComparison.Ordinal));
        Assert.Contains(ev, r => r.Kind == RecordKind.Visit && r.Text.Contains("The rental has to come with drivers", StringComparison.Ordinal));

        // The later short days came from execution confusion with three forklifts bought: the cut is not named there.
        Assert.All(d.Gaps.Where(g => g.CapabilityId == "mhe" && g.StartDay > 8), g => Assert.DoesNotContain(g.Factors, f => f.Kind == GapFactorKind.BuyCut));
        Assert.Contains(Cut, d.Summary!.Text);
    }

    [Fact]
    public void A_count_of_one_agrees_with_its_unit_in_records_and_the_debrief()
    {
        var s = SixthPlay();
        Assert.Contains(s.Records, r => r.Text.EndsWith("began, 1 forklift/day.", StringComparison.Ordinal));
        Assert.Contains(s.Records, r => r.Text.Contains(", 1 forklift/day for 13 days", StringComparison.Ordinal));
        foreach (var scenario in new[] { "tutorial", "sustainment" })
            foreach (var policy in new[] { "greedy", "textbook" })
            {
                var p = Replay.Run(Repo.Load(scenario), 1, PolicyCatalog.Create(policy));
                Assert.DoesNotContain(p.Records, r => System.Text.RegularExpressions.Regex.IsMatch(r.Text, @"(^|[^0-9.,])1 (forklifts|vehicles|beds|meals|links)\b"));
                Assert.DoesNotContain(p.View.Debrief().Gaps.SelectMany(g => g.Factors),
                    f => System.Text.RegularExpressions.Regex.IsMatch(f.Text, @"(^|[^0-9.,])1 (forklifts|vehicles|beds|meals|links)\b"));
            }
    }

    [Fact]
    public void A_buy_run_its_full_term_is_never_named_as_cut()
    {
        foreach (var policy in new[] { "do-nothing", "greedy", "textbook" })
        {
            var p = Replay.Run(Repo.Load("tutorial"), 1, PolicyCatalog.Create(policy));
            Assert.All(p.Arrangements.Where(a => a.CutDay is null), a => Assert.Equal(0, a.PlannedEndDay));
            Assert.DoesNotContain(p.View.Debrief().Gaps.SelectMany(g => g.Factors), f => f.Kind == GapFactorKind.BuyCut);
        }
    }
}
