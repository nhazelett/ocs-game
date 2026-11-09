using System.Text.RegularExpressions;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Policies;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>M2R-27: the debrief opens with a paragraph in a person's words that names the gap that mattered most and its
/// evidence (R9, R4), labels come as plain words, and no Sim text says "day(s)".</summary>
public class DebriefSummaryTests
{
    private static readonly string[] Scenarios = ["spoke-b", "spoke-b-90", "sustainment", "tutorial"];

    private static IEnumerable<(string Run, Session S)> AllRuns()
    {
        foreach (var sc in Scenarios)
        {
            var bundle = Repo.Load(sc);
            foreach (var path in Directory.GetFiles(Path.Combine(Repo.Scenario(sc), "golden"), "*.json").Order(StringComparer.Ordinal))
                yield return ($"{sc}/{Path.GetFileName(path)}", Replay.Run(bundle, InputLog.Load(path)));
            foreach (var name in PolicyCatalog.Names)
                yield return ($"{sc}/{name}", Replay.Run(bundle, 1, PolicyCatalog.Create(name)));
        }
    }

    private static GapView? FirstFuelGapAtSpokeB(DebriefView d)
        => d.Gaps.Where(g => g.SiteId == "spokeBSite" && g.CapabilityId == "fuel").OrderBy(g => g.StartDay).FirstOrDefault();

    [Fact]
    public void Approach_1_opens_on_the_unowned_fuel_from_the_sco_staff()
    {
        var bundle = Repo.Load("spoke-b");
        var s = Replay.Run(bundle, InputLog.Load(Path.Combine(Repo.Scenario("spoke-b"), "golden", "approach1-as-written.json")));
        var d = s.View.Debrief();
        var sum = d.Summary!;
        Assert.Equal(bundle.Scenario.Actors.Sco.Person, sum.From!.Id);
        var fuel = FirstFuelGapAtSpokeB(d)!;
        Assert.Equal(fuel.Index, sum.LeadGap);
        Assert.Contains("The gap that mattered most was fuel at Spoke B", sum.Text);
        Assert.Contains("Nobody owned fuel at Spoke B", sum.Text);
        Assert.Contains("went down with it", sum.Text);
        Assert.StartsWith("Over thirty days", sum.Text);
    }

    [Fact]
    public void Asking_the_j4_moves_the_lead_off_the_unowned_fuel()
    {
        var s = Replay.Run(Repo.Load("spoke-b"), InputLog.Load(Path.Combine(Repo.Scenario("spoke-b"), "golden", "approach1-with-j4.json")));
        var sum = s.View.Debrief().Summary!;
        Assert.DoesNotContain("Nobody owned fuel", sum.Text);
    }

    [Fact]
    public void Every_run_opens_with_prose_about_a_real_gap()
    {
        var bullets = new Regex(@"(^|\n)\s*([-*•·]|\d+\.)\s|\n|\(s\)|\*\*|#");
        foreach (var (run, s) in AllRuns())
        {
            var d = s.View.Debrief();
            var sum = d.Summary;
            Assert.True(sum is not null, run);
            Assert.False(string.IsNullOrWhiteSpace(sum!.Text), run);
            Assert.False(bullets.IsMatch(sum.Text), $"{run}: not prose: {sum.Text}");
            if (d.Gaps.Count == 0) { Assert.Null(sum.LeadGap); Assert.Contains("no site ran short", sum.Text); continue; }
            var lead = d.Gaps[sum.LeadGap!.Value];
            Assert.Contains($"{lead.Label.ToLowerInvariant()} at ", sum.Text);
            // The lead is the most controllable gap with a cause of its own.
            Assert.DoesNotContain(d.Gaps, g => g.Tag > lead.Tag && (g.Factors.Count == 0 || g.Factors.Any(f => f.DependsOnGap is null)));
            foreach (var f in lead.Factors.Where(f => f.DependsOnGap is null).OrderByDescending(f => f.Tag).ThenBy(f => f.FirstDay).Take(2))
                Assert.Contains(f.Text.Trim(), sum.Text);
        }
    }

    [Fact]
    public void No_sim_text_counts_in_day_s()
    {
        foreach (var (run, s) in AllRuns())
        {
            foreach (var r in s.Records) Assert.False(r.Text.Contains("(s)"), $"{run}: {r.Text}");
            foreach (var g in s.View.Debrief().Gaps.SelectMany(g => g.Factors)) Assert.False(g.Text.Contains("(s)"), $"{run}: {g.Text}");
        }
    }

    [Fact]
    public void Every_label_has_plain_words()
    {
        foreach (var k in Enum.GetValues<GapFactorKind>())
        {
            var w = DebriefWords.Factor(k);
            Assert.NotEqual("Another cause", w);
            Assert.DoesNotMatch("[a-z][A-Z]", w);
        }
        foreach (var t in Enum.GetValues<GapTag>()) Assert.StartsWith(t.ToString(), DebriefWords.TagMeaning(t));
        var kinds = AllRuns().SelectMany(r => r.S.View.Debrief().Events.Select(e => e.Kind)).Distinct().ToList();
        Assert.NotEmpty(kinds);
        foreach (var k in kinds) Assert.NotEqual("Update", DebriefWords.TimelineKind(k));
    }

    [Fact]
    public void Number_words_read_as_spoken()
    {
        Assert.Equal("thirty", DebriefWords.Number(30));
        Assert.Equal("fourteen", DebriefWords.Number(14));
        Assert.Equal("twenty-one", DebriefWords.Number(21));
        Assert.Equal("ninety", DebriefWords.Number(90));
        Assert.Equal("120", DebriefWords.Number(120));
    }

    [Fact]
    public void Print_summaries_for_review()
    {
        if (Environment.GetEnvironmentVariable("PRINT_SUMMARIES") is not { } path) return;
        File.WriteAllLines(path, AllRuns().Select(r => $"{r.Run}\n  {r.S.View.Debrief().Summary?.From?.DisplayName}: {r.S.View.Debrief().Summary?.Text}\n"));
    }
}
