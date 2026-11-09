using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>M2R-02: turnover and briefing data (docs/design/opening-and-guidance.md 3.1, 3.2, R6, R9).</summary>
public class TurnoverTests
{
    private static IEnumerable<string> IdShaped(ScenarioFile s) =>
        s.Sites.Select(x => x.Id).Concat(s.Islands.Select(x => x.Id)).Concat(s.RequirementCards.Select(x => x.Id))
         .Concat(s.Vendors.Select(x => x.Id)).Concat(s.Vehicles.Select(x => x.Id)).Concat(s.FundingLines.Select(x => x.Id))
         .Concat(s.InheritedArrangements.Select(x => x.Id)).Concat(s.People.Select(x => x.Id))
         .Distinct().Where(i => Regex.IsMatch(i, @"\d|^.+[A-Z]"));

    /// <summary>Game text is prose (R9) and carries no identifiers (R6).</summary>
    private static void Prose(ScenarioFile s, string? text, string where)
    {
        if (text is null) return;
        Assert.DoesNotContain('\n', text);
        Assert.False(Regex.IsMatch(text, @"^\s*([-*•#]|\d+\.)\s|\s[*•#]\s"), $"{where}: list or header marks in \"{text}\"");
        Assert.DoesNotContain("**", text);
        foreach (var id in IdShaped(s))
            Assert.False(Regex.IsMatch(text, $@"(?<!\w){Regex.Escape(id)}(?!\w)"), $"{where}: \"{text}\" shows the id {id}");
    }

    private static TurnoverView Turnover(string id, IEnumerable<string>? prep = null) => Session.Create(Repo.Load(id), 1, prep).View.Turnover();

    public static IEnumerable<object[]> AllScenarios() =>
        Directory.GetDirectories(Path.Combine(Repo.Root, "scenarios"))
                 .Where(d => File.Exists(Path.Combine(d, "scenario.json")))
                 .Select(d => new object[] { Path.GetFileName(d) });

    [Theory]
    [MemberData(nameof(AllScenarios))]
    public void Every_scenario_has_a_turnover_of_four_prose_paragraphs(string id)
    {
        var b = Repo.Load(id);
        var t = Turnover(id);
        Assert.Equal([TurnoverSectionKind.Shop, TurnoverSectionKind.Contracts, TurnoverSectionKind.Vendors, TurnoverSectionKind.Money],
            t.Sections.Select(x => x.Kind));
        foreach (var sec in t.Sections)
        {
            Assert.False(string.IsNullOrWhiteSpace(sec.Text));
            Prose(b.Scenario, sec.Text, $"{id} {sec.Kind}");
        }
        Prose(b.Scenario, t.Greeting, $"{id} greeting");
        Prose(b.Scenario, t.LastLine, $"{id} last line");
        if (t.Briefing is { } br)
            foreach (var x in new[] { br.Title, br.Situation, br.Due }) Prose(b.Scenario, x, $"{id} briefing");
        Assert.Equal(b.Scenario.InheritedArrangements.Count, t.Contracts.Count);
        Assert.Equal(b.Scenario.Vendors.Count, t.Vendors.Count);
        Assert.Equal(b.Scenario.FundingLines.Count, t.Money.Count);
    }

    [Theory]
    [InlineData("spoke-b")]
    [InlineData("spoke-b-90")]
    public void Spoke_b_is_handed_over_by_the_outgoing_cco_who_ends_on_ce(string id)
    {
        var t = Turnover(id);
        Assert.Equal("outgoingCco", t.From?.Role);
        Assert.NotNull(t.Briefing);
        Assert.Contains("day 7", t.Briefing!.Due);
        Assert.Equal("Spoke B opens in a week and nobody's bought them power yet. CE's outside.", t.LastLine);
        Assert.StartsWith("Here's who I've used. Half of these I've never called.", t.Text(TurnoverSectionKind.Vendors));
        Assert.Contains("Port Fuel Haulers came by last week asking for work.", t.Text(TurnoverSectionKind.Vendors));
        Assert.Equal("Port Fuel Haulers came by last week asking for work.", t.Vendors.Single(v => v.VendorId == "vendorE").Remark);
        Assert.Contains("$900,000", t.Text(TurnoverSectionKind.Money));
    }

    [Fact]
    public void The_shop_paragraph_is_the_shop_as_prep_left_it()
    {
        var none = Turnover("spoke-b").Text(TurnoverSectionKind.Shop);
        Assert.Contains("down until about day 12", none);
        Assert.Contains("No purchase cards this time.", none);
        Assert.Contains("starts on day 5", none);

        var picked = Turnover("spoke-b", ["gpcAccounts", "cashAndPayingAgent", "systemAccess"]).Text(TurnoverSectionKind.Shop);
        Assert.Contains("down until about day 3", picked);
        Assert.DoesNotContain("No purchase cards", picked);
        Assert.Contains("There are two of you, 16 hours a day between you.", picked);
    }

    [Fact]
    public void Hidden_inherited_problems_stay_hidden_at_the_turnover()
    {
        var b = Repo.Load("sustainment");
        Assert.Contains(b.Scenario.InheritedArrangements, a => a.VisibleProblem == false);
        var t = Turnover("sustainment");
        var all = string.Join(" ", t.Sections.Select(x => x.Text));
        foreach (var word in new[] { "UCA", "definitiz", "overdue", "past due", "ceiling", "%" })
            Assert.DoesNotContain(word, all, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void The_prep_screen_speaks_and_its_defaults_fit()
    {
        var b = Repo.Load("spoke-b");
        var v = PrepScreen.View(b);
        Assert.Equal("scoStaff", v.From?.Role);
        Assert.StartsWith("You have fourteen days before wheels up.", v.Intro);
        Assert.All(v.Options, o => Assert.False(string.IsNullOrWhiteSpace(o.Says)));
        Assert.All(v.Options, o => Prose(b.Scenario, o.Says, $"prep {o.Id}"));
        Assert.Equal(["gpcAccounts", "cashAndPayingAgent", "systemAccess"], v.Defaults);
        Assert.True(PrepScreen.Check(b, v.Defaults!).Accepted);
        Assert.Null(PrepScreen.View(Repo.Load("sustainment")).From);
    }

    private static ScenarioBundle With(Action<JsonNode> change)
    {
        var root = JsonNode.Parse(File.ReadAllText(Path.Combine(Repo.Scenario("spoke-b"), "scenario.json")))!;
        change(root);
        var dir = Path.Combine(Path.GetTempPath(), $"los-turnover-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "scenario.json"), root.ToJsonString());
        return ScenarioBundle.Load(dir, Path.Combine(Repo.Root, "scenarios", "rules"));
    }

    [Theory]
    [InlineData("from", "must have role outgoingCco")]
    [InlineData("prepChoice", "is not in rules")]
    [InlineData("prepBudget", "prep-days; the scenario allows")]
    [InlineData("prepFrom", "prepBriefing.from person nobody not found")]
    public void Bad_turnover_and_prep_data_is_refused(string what, string message)
    {
        var ex = Assert.Throws<InvalidDataException>(() => With(root =>
        {
            var t = root["turnover"]!;
            var pb = root["prepBriefing"]!;
            switch (what)
            {
                case "from": t["from"] = "ruiz"; break;
                case "prepChoice": pb["choices"]!["nope"] = "x"; break;
                case "prepBudget": pb["defaults"] = new JsonArray("gpcAccounts", "cashAndPayingAgent", "systemAccess", "vendorLists", "warrantOrThreshold", "pullVehicles"); break;
                case "prepFrom": pb["from"] = "nobody"; break;
            }
        }));
        Assert.Contains(message, ex.Message);
    }

    [Fact]
    public void A_remark_for_an_item_that_is_not_there_is_ignored()
    {
        // The game's quiet smoke fixture empties inheritedArrangements and keeps the turnover.
        var b = With(root => root["inheritedArrangements"] = new JsonArray());
        var t = Session.Create(b, 1).View.Turnover();
        Assert.Empty(t.Contracts);
        Assert.DoesNotContain("never let us down", string.Join(" ", t.Sections.Select(x => x.Text)));
        Assert.Contains("You're not inheriting any contracts.", t.Text(TurnoverSectionKind.Contracts));
    }
}
