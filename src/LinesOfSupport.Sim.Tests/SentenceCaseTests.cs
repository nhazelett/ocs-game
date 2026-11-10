using System.Text.RegularExpressions;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Policies;

namespace LinesOfSupport.Sim.Tests;

/// <summary>M2R-40 (fifth pre-gate walk): every sentence a player reads starts with a capital letter, including those that
/// open on a name written to read mid-sentence ("the partner force"). Same coverage as <see cref="PlainNamesTests"/>
/// (every golden and the three policies on every scenario: records, mornings, leads, debrief, every played action's
/// preview) plus the award editor's reasons for every request on every morning a policy plays.</summary>
public class SentenceCaseTests
{
    private static readonly Regex LowerStart = new(@"(^|[.!?]\s+)([a-z])", RegexOptions.CultureInvariant);

    private static void Capitalized(IEnumerable<(string Where, string Text)> texts, string run)
    {
        foreach (var (where, text) in texts)
            if (LowerStart.Match(text) is { Success: true } m)
                Assert.Fail($"{run}: sentence starts lower case at '{text[m.Index..Math.Min(text.Length, m.Index + 40)].Trim()}' in {where}: {text}");
    }

    private static IEnumerable<(string, string)> ChoiceReasons(Session s)
    {
        foreach (var card in s.Scenario.RequirementCards.Where(c => c.SubmittedDay <= s.Day))
        {
            var site = card.Site;
            var cap = card.Capability;
            var open = s.View.AwardChoices(site, cap, card.Id);
            if (open is null) continue;
            var picks = new List<(string?, string?)> { (null, null) };
            picks.AddRange(open.Vendors.Select(v => ((string?)v.Id, (string?)null)));
            picks.AddRange(open.Instruments.Select(i => ((string?)null, (string?)i.Id)));
            foreach (var (vendor, instrument) in picks)
            {
                var c = s.View.AwardChoices(site, cap, card.Id, vendor, instrument)!;
                foreach (var o in c.Instruments.Concat(c.Vendors).Concat(c.Vehicles).Concat(c.Funding))
                    if (o.Reason is { } r) yield return ($"award choice day {s.Day}", r);
            }
        }
    }

    [Theory]
    [MemberData(nameof(PlainNamesTests.Scenarios), MemberType = typeof(PlainNamesTests))]
    public void Goldens_start_every_sentence_with_a_capital(string scenario)
    {
        var bundle = Repo.Load(scenario);
        foreach (var path in Directory.GetFiles(Path.Combine(Repo.Scenario(scenario), "golden"), "*.json").Order(StringComparer.Ordinal))
        {
            var s = Replay.Run(bundle, InputLog.Load(path));
            Capitalized(PlainNamesTests.DebriefAndPreviews(s), $"{scenario}/{Path.GetFileName(path)}");
        }
    }

    [Theory]
    [MemberData(nameof(PlainNamesTests.Scenarios), MemberType = typeof(PlainNamesTests))]
    public void Policies_start_every_sentence_with_a_capital(string scenario)
    {
        var bundle = Repo.Load(scenario);
        foreach (var name in PolicyCatalog.Names)
        {
            var s = Session.Create(bundle, 1);
            var policy = PolicyCatalog.Create(name);
            var texts = new List<(string, string)>(ChoiceReasons(s));
            while (!s.Finished)
            {
                foreach (var a in policy.Decide(s.View)) s.Apply(a);
                s.AdvanceDay();
                var board = s.View.MorningBoard();
                texts.AddRange(board.Items.Select(i => ($"board day {s.Day}", i.Text)));
                if (board.Lead is { } lead) texts.Add(($"lead day {s.Day}", lead.Text));
                if (s.Day % 5 == 1) texts.AddRange(ChoiceReasons(s));
            }
            Capitalized(texts.Concat(PlainNamesTests.DebriefAndPreviews(s)), $"{scenario}/{name}");
        }
    }

    /// <summary>The J4 ask the fifth walk saw on screen: Spoke B fuel on day 1 shows the partner's soft gap as a sentence.</summary>
    [Fact]
    public void J4_ask_preview_reads_as_sentences()
    {
        var s = Session.Create(Repo.Load("spoke-b"), 1);
        var p = s.View.PreviewVisit(new EngageJ4Action { Site = "spokeBSite", Capability = "fuel", Quantity = 1170 });
        Assert.NotEmpty(p.SoftGaps);
        Assert.StartsWith("The partner force can cut this back", p.SoftGaps[0]);
        Capitalized(p.HardStops.Concat(p.SoftGaps).Select(t => ("j4 preview", t)), "spoke-b day 1");
    }
}
