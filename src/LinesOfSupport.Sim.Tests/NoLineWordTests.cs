using System.Text.RegularExpressions;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Policies;

namespace LinesOfSupport.Sim.Tests;

/// <summary>DESK-36 (docs/design/desk-redesign.md D3): the request has no checkboxes, so nothing the player reads calls a
/// part of a request or a buy a "line". Funding lines keep their name: that is what the money is called.</summary>
public class NoLineWordTests
{
    private static readonly string[] Scenarios = ["spoke-b", "spoke-b-90", "sustainment", "tutorial"];

    // The only words allowed in front of "line": the money ("the O&M line", "a funding line", "another line", "the line").
    private static readonly HashSet<string> FundingWords = new(StringComparer.OrdinalIgnoreCase) { "O&M", "funding", "another", "the", "ACSA" };

    private static IEnumerable<string> LineWords(string text)
        => Regex.Matches(text, @"([A-Za-z&-]+) lines?\b").Select(m => m.Groups[1].Value).Where(w => !FundingWords.Contains(w));

    public static IEnumerable<object[]> Runs()
        => Scenarios.SelectMany(sc => Directory.GetFiles(Path.Combine(Repo.Scenario(sc), "golden"), "approach*.json")
                .OrderBy(p => p, StringComparer.Ordinal).Select(p => new object[] { sc, Path.GetFileName(p) })
            .Concat(new[] { "greedy", "do-nothing" }.Select(p => new object[] { sc, p })));

    [Theory]
    [MemberData(nameof(Runs))]
    public void Records_and_the_debrief_never_call_a_part_of_a_buy_a_line(string scenario, string run)
    {
        var bundle = Repo.Load(scenario);
        var session = run.EndsWith(".json", StringComparison.Ordinal)
            ? Replay.Run(bundle, InputLog.Load(Path.Combine(Repo.Scenario(scenario), "golden", run)))
            : Replay.Run(bundle, 1, PolicyCatalog.Create(run));
        Assert.Empty(LineWords(SessionReport.ToText(session, "")));
    }

    [Theory]
    [MemberData(nameof(ScenarioIds))]
    public void What_a_left_out_part_costs_reads_without_the_word(string scenario)
    {
        var s = Session.Create(Repo.Load(scenario), 1);
        var seen = 0;
        while (true)
        {
            foreach (var card in s.Scenario.RequirementCards.Where(c => c.SubmittedDay == s.Day))
                foreach (var line in s.View.RequirementEditor(card.Id)!.Lines.Where(l => l.GapIfOff is not null))
                {
                    seen++;
                    Assert.Empty(LineWords(line.GapIfOff!));
                }
            if (s.Finished) break;
            s.AdvanceDay();
        }
        Assert.True(seen > 0, "every scenario has at least one request with a part that can be left out");
    }

    public static IEnumerable<object[]> ScenarioIds() => Scenarios.Select(sc => new object[] { sc });

    [Fact]
    public void The_forklift_operators_read_as_what_they_are()
    {
        var s = Session.Create(Repo.Load("tutorial"), 1);
        var day = s.Scenario.RequirementCards.First(c => c.Id == "tutForklift").SubmittedDay;
        while (s.Day < day) s.AdvanceDay();
        Assert.True(s.Apply(new AskAboutAction { Card = "tutForklift", Person = "dunn" }).Accepted);
        s.AdvanceDay();
        var line = s.View.RequirementEditor("tutForklift")!.Lines.Single(l => l.Id == "operator");
        Assert.Equal($"Without {line.Label.ToLowerInvariant()}, it fails more often every day it runs.", line.GapIfOff);
        var added = s.Apply(new AddLineAction { Card = "tutForklift", Line = "operator" });
        Assert.True(added.Accepted);
        Assert.Equal($"{line.Label} added to the request.", added.Message);
    }
}
