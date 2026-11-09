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

    /// <summary>Each scenario's turnover paragraphs, from a fresh session as the player reads them.</summary>
    private static readonly Dictionary<string, string[]> Turnovers = Scenarios.ToDictionary(sc => sc, sc =>
    {
        var t = Session.Create(Repo.Load(sc), 1).View.Turnover();
        return t.Sections.Select(x => x.Text).ToArray();
    });

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
    public void Quoted_names_exactly_the_factors_the_paragraph_says()
    {
        // M2R-29: the evidence under the paragraph shows a quoted factor by its tag line alone, so Quoted must be exactly the
        // lead gap's factors whose text the paragraph carries, in the order it says them, and nothing it does not say.
        var quotedSomewhere = false;
        foreach (var (run, s) in AllRuns())
        {
            var d = s.View.Debrief();
            var sum = d.Summary!;
            if (sum.LeadGap is null) { Assert.Empty(sum.Quoted); continue; }
            var lead = d.Gaps[sum.LeadGap.Value];
            Assert.Equal(sum.Quoted.Distinct().Count(), sum.Quoted.Count);
            var at = -1;
            foreach (var i in sum.Quoted)
            {
                Assert.InRange(i, 0, lead.Factors.Count - 1);
                var pos = sum.Text.IndexOf(lead.Factors[i].Text.Trim(), StringComparison.Ordinal);
                Assert.True(pos > at, $"{run}: factor {i} not said, or out of order");
                at = pos;
                quotedSomewhere = true;
            }
            for (var i = 0; i < lead.Factors.Count; i++)
            {
                var t = lead.Factors[i].Text.Trim();
                if (sum.Quoted.Contains(i) || t.Length == 0 || sum.Quoted.Any(q => lead.Factors[q].Text.Trim() == t)) continue;
                Assert.False(sum.Text.Contains(t, StringComparison.Ordinal), $"{run}: factor {i} is said but not marked quoted");
            }
        }
        Assert.True(quotedSomewhere);
    }

    [Fact]
    public void The_partner_force_reads_as_a_noun_mid_sentence()
    {
        // M2R-29: "and Partner force had 4,000 gal a day to spare" read as a misprint. The partner is written as it reads
        // mid-sentence and capitalised only where it starts one.
        // M2R-31: the vendor behind the inherited ACSA fuel row follows the same rule, so no "Partner force (ACSA)" label
        // and no doubled parenthetical ("Partner force (ACSA) (acquisition and cross-servicing agreement order)").
        var midSentenceCapital = new Regex(@"[a-z,;:] Partner force\b");
        var startsLower = new Regex(@"(^|[.!?] )the partner force\b");
        var doubledParens = new Regex(@"\([^()]*\) \(");
        foreach (var (run, s) in AllRuns())
        {
            var texts = s.Records.Select(r => r.Text)
                         .Concat(s.View.Debrief().Gaps.SelectMany(g => g.Factors).Select(f => f.Text))
                         .Append(s.View.Debrief().Summary?.Text ?? "")
                         .Concat(Turnovers[run.Split('/')[0]]);
            foreach (var t in texts)
            {
                Assert.False(midSentenceCapital.IsMatch(t), $"{run}: {t}");
                Assert.False(startsLower.IsMatch(t), $"{run}: {t}");
                Assert.False(t.Contains("Partner force (ACSA)", StringComparison.Ordinal), $"{run}: {t}");
                Assert.False(doubledParens.IsMatch(t), $"{run}: {t}");
            }
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
