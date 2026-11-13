using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Policies;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>M2R-01: people in scenario data (docs/design/opening-and-guidance.md 5, R2, R6).</summary>
public class PeopleTests
{
    private static readonly string[] Cast = ["spoke-b", "spoke-b-90"];

    private static IEnumerable<string> ScenarioIds(ScenarioFile s) =>
        s.Sites.Select(x => x.Id).Concat(s.Islands.Select(x => x.Id)).Concat(s.RequirementCards.Select(x => x.Id))
         .Concat(s.Vendors.Select(x => x.Id)).Concat(s.Vehicles.Select(x => x.Id)).Concat(s.FundingLines.Select(x => x.Id))
         .Concat(s.InheritedArrangements.Select(x => x.Id)).Concat(s.Events.Select(x => x.Id)).Concat(s.People.Select(x => x.Id))
         .Concat(s.TransportLinks.Select(x => x.Id));

    /// <summary>Fails if any identifier-shaped scenario id (one with a digit or an inner capital: req0007, spokeBSite,
    /// vendorE, om1) appears as a whole word in a player-facing person string. Ids that are plain words (hub, barge)
    /// cannot be told from prose and are not checked.</summary>
    private static void NoIds(ScenarioFile s, string? text, string where)
    {
        if (string.IsNullOrEmpty(text)) return;
        foreach (var id in ScenarioIds(s).Distinct().Where(i => Regex.IsMatch(i, @"\d|^.+[A-Z]")))
            Assert.False(Regex.IsMatch(text, $@"(?<![\w]){Regex.Escape(id)}(?![\w])"), $"{where}: \"{text}\" shows the id {id}");
    }

    private static void PersonClean(ScenarioFile s, PersonView p, string where)
    {
        Assert.False(string.IsNullOrWhiteSpace(p.DisplayName), where);
        NoIds(s, p.DisplayName, where);
        NoIds(s, p.WithUnit, where);
        foreach (var (k, line) in p.Lines) NoIds(s, line, $"{where} line {k}");
    }

    [Theory]
    [InlineData("spoke-b")]
    [InlineData("spoke-b-90")]
    public void Every_card_is_brought_by_a_person_shown_by_rank_name_and_unit(string id)
    {
        var b = Repo.Load(id);
        var s = Session.Create(b, 1);
        var v = s.View;
        Assert.NotEmpty(v.People);
        foreach (var p in v.People) PersonClean(b.Scenario, p, $"{id} person {p.Id}");
        foreach (var card in v.Cards)
        {
            var who = v.CardPerson(card);
            Assert.NotNull(who);
            Assert.Equal("customer", who!.Role);
            var ed = v.RequirementEditor(card.Id);
            if (ed is null) continue; // submitted later
            Assert.Equal(who.WithUnit, ed.SubmittedBy);
            Assert.Equal(who, ed.Person);
            Assert.False(string.IsNullOrWhiteSpace(ed.Message));
            NoIds(b.Scenario, ed.Message, $"{id} card {card.Id} message");
            NoIds(b.Scenario, ed.Title, $"{id} card {card.Id} title");
            NoIds(b.Scenario, ed.SubmittedBy, $"{id} card {card.Id} submittedBy");
        }
    }

    [Fact]
    public void Spoke_b_power_comes_from_capt_ruiz_of_3_ces()
    {
        var v = Session.Create(Repo.Load("spoke-b"), 1).View;
        var ed = v.RequirementEditor("req0007")!;
        Assert.Equal("Power for Spoke B", ed.Title);
        Assert.Equal("Capt Ana Ruiz, 3 CES", ed.SubmittedBy);
        Assert.Contains("six sixty-kilowatt generators", ed.Message);
    }

    [Theory]
    [InlineData("spoke-b")]
    [InlineData("spoke-b-90")]
    public void The_actors_are_people(string id)
    {
        var v = Session.Create(Repo.Load(id), 1).View;
        Assert.Equal("scoStaff", v.ScoContact?.Role);
        Assert.Equal("finance", v.FinanceContact?.Role);
        Assert.Equal("commander", v.Commander?.Role);
        Assert.Equal("outgoingCco", v.OutgoingCco?.Role);
        Assert.NotNull(v.OutgoingCco!.Line("farewell"));
    }

    [Theory]
    [InlineData("spoke-b", "greedy")]
    [InlineData("spoke-b", "textbook")]
    [InlineData("spoke-b", "do-nothing")]
    [InlineData("spoke-b-90", "greedy")]
    [InlineData("spoke-b-90", "textbook")]
    public void Board_items_name_who_the_player_hears_them_from(string id, string policyName)
    {
        var b = Repo.Load(id);
        var froms = b.Scenario.Events.Where(e => e.From is not null).ToDictionary(e => e.Id, e => e.From!);
        var policy = PolicyCatalog.Create(policyName);
        var s = Session.Create(b, 1, policy.PrepChoices(b));
        var seen = new HashSet<string>();
        while (!s.Finished)
        {
            foreach (var a in policy.Decide(s.View).ToList()) s.Apply(a);
            s.AdvanceDay();
            foreach (var item in s.View.MorningBoard().Items)
            {
                if (item.Person is { } p) PersonClean(b.Scenario, p, $"{id} day {item.Day} {item.RecordId}");
                if (item.LinkKind == BoardLinkKind.Event && froms.TryGetValue(item.LinkId, out var from))
                {
                    Assert.Equal(from, item.Person?.Id);
                    seen.Add(item.LinkId);
                }
                var rec = s.View.Records.Single(r => r.Id == item.RecordId);
                Assert.Equal(item.Person, s.View.RecordPerson(rec));
            }
        }
        Assert.NotEmpty(seen); // at least one spoken event reached the board in every run
    }

    [Fact]
    public void A_handshake_comes_to_the_desk_through_the_vendors_rep()
    {
        var b = Repo.Load("spoke-b");
        var s = Replay.Run(b, InputLog.Load(Path.Combine(Repo.Scenario("spoke-b"), "golden", "approach1-park.json")));
        var hs = s.View.Commitments.First(c => c.Kind == CommitmentKind.Handshake);
        Assert.Equal("vendorE", hs.VendorId);
        var rep = s.View.CommitmentPerson(hs);
        Assert.Equal("Mr. Kofi Mensah", rep?.DisplayName);
        Assert.Equal("Port Fuel Haulers", rep?.VendorName);
    }

    // ------------------------------------------------------------------ bundle checks

    private static ScenarioBundle With(Action<JsonNode> change)
    {
        var root = JsonNode.Parse(File.ReadAllText(Path.Combine(Repo.Scenario("spoke-b"), "scenario.json")))!;
        change(root);
        var dir = Path.Combine(Path.GetTempPath(), $"los-people-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "scenario.json"), root.ToJsonString());
        return ScenarioBundle.Load(dir, Path.Combine(Repo.Root, "scenarios", "rules"));
    }

    private static JsonNode PersonNode(JsonNode root, string id) =>
        root["people"]!.AsArray().First(p => (string)p!["id"]! == id)!;

    [Theory]
    [InlineData("card", "person nobody not found")]
    [InlineData("event", "from nobody not found")]
    [InlineData("role", "must have role finance")]
    [InlineData("vendorRep", "vendorRep with no vendor")]
    [InlineData("duplicate", "person id ruiz is not unique")]
    [InlineData("noSubmitter", "names neither a person nor submittedBy")]
    public void Bad_people_references_are_refused(string what, string message)
    {
        var ex = Assert.Throws<InvalidDataException>(() => With(root =>
        {
            switch (what)
            {
                case "card": root["requirementCards"]![0]!["person"] = "nobody"; break;
                case "event": root["events"]![0]!["from"] = "nobody"; break;
                case "role": root["actors"]!["fm"]!["person"] = "ruiz"; break;
                case "vendorRep": PersonNode(root, "mensah").AsObject().Remove("vendor"); break;
                case "duplicate": root["people"]!.AsArray().Add(PersonNode(root, "ruiz").DeepClone()); break;
                case "noSubmitter": root["requirementCards"]![0]!.AsObject().Remove("person"); break;
            }
        }));
        Assert.Contains(message, ex.Message);
    }

    [Fact]
    public void A_card_with_only_free_text_still_loads_and_shows_it()
    {
        var b = With(root =>
        {
            var card = root["requirementCards"]![0]!.AsObject();
            card.Remove("person");
            card.Remove("message");
            card["submittedBy"] = "Spoke B advance team";
        });
        var ed = Session.Create(b, 1).View.RequirementEditor("req0007")!;
        Assert.Null(ed.Person);
        Assert.Equal("Spoke B advance team", ed.SubmittedBy);
    }

    /// <summary>ART-18: everyone the player meets has their own face. Each person in a scenario, and each member of
    /// its named team, names a portrait that exists under assets/portraits, and no two of them share one. A teammate
    /// without a face would get the game's first unused portrait, which can be someone the player meets in the same
    /// scenario (the tutorial's junior CCO wore Maj Cole's face that way), so the team names its faces too.</summary>
    [Theory]
    [InlineData("tutorial")]
    [InlineData("spoke-b")]
    [InlineData("spoke-b-90")]
    public void Every_person_has_their_own_portrait(string id)
    {
        var scenario = Repo.Load(id).Scenario;
        Assert.NotEmpty(scenario.People);
        var faces = scenario.People.Select(p => (p.Name, p.Portrait))
            .Concat((scenario.Shop.Team ?? []).Select(m => (m.Name, m.Portrait))).ToList();
        foreach (var (name, portrait) in faces)
        {
            Assert.False(string.IsNullOrWhiteSpace(portrait), $"{id}: {name} has no portrait");
            Assert.True(File.Exists(Path.Combine(Repo.Root, "assets", "portraits", portrait + ".svg")),
                $"{id}: {name} names portrait {portrait}, which has no file");
        }
        var shared = faces.GroupBy(f => f.Portrait).Where(g => g.Count() > 1)
            .Select(g => $"{g.Key} ({string.Join(", ", g.Select(f => f.Name))})").ToList();
        Assert.True(shared.Count == 0, $"{id}: portraits shared: {string.Join("; ", shared)}");
    }
}
