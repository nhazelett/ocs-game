using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Views;

namespace LinesOfSupport.Sim.Tests;

/// <summary>M2R-05 (lead part): the one decision in front (docs/design/opening-and-guidance.md R5, 6.3).</summary>
public class LeadTests
{
    private static string Golden(string scenario, string name) => Path.Combine(Repo.Scenario(scenario), "golden", $"{name}.json");

    /// <summary>Plays a golden and returns each morning's board, keyed by day.</summary>
    private static Dictionary<int, (MorningBoardView Board, Session Session)> Mornings(string scenario, string log, params int[] days)
    {
        var b = Repo.Load(scenario);
        var l = InputLog.Load(Golden(scenario, log));
        var s = Session.Create(b, l.Seed, l.PrepChoices);
        var result = new Dictionary<int, (MorningBoardView, Session)>();
        while (!s.Finished)
        {
            if (days.Contains(s.Day)) result[s.Day] = (s.View.MorningBoard(), s);
            foreach (var a in l.Actions.Where(a => a.Day == s.Day)) s.Apply(a);
            s.AdvanceDay();
        }
        return result;
    }

    [Fact]
    public void Day_one_of_spoke_b_leads_with_capt_ruiz_and_power()
    {
        var lead = Mornings("spoke-b", "approach1-as-written", 1)[1].Board.Lead!;
        Assert.Equal(LeadKind.Decision, lead.Kind);
        Assert.Equal(BoardLinkKind.Card, lead.LinkKind);
        Assert.Equal("req0007", lead.LinkId);
        Assert.Equal("ruiz", lead.Person?.Id);
        Assert.Equal("Capt Ana Ruiz, 3 CES, is here about power for Spoke B, wanted from day 7.", lead.Text);
    }

    [Fact]
    public void Approach_one_sees_the_fuel_gap_lead_on_day_11_and_the_handshake_on_day_12()
    {
        var m = Mornings("spoke-b", "approach1-as-written", 11, 12);
        var d11 = m[11].Board;
        Assert.Equal(LeadKind.Alert, d11.Lead?.Kind);
        Assert.Equal(d11.Items.First(i => i.Severity == BoardSeverity.Alert).RecordId, d11.Lead!.RecordId);
        Assert.Equal("spokeBSite.fuel", d11.Lead.LinkId);
        Assert.Equal("ruiz", d11.Lead.Person?.Id); // M2R-20: the failure comes from Spoke B's customer, not from nowhere
        var d12 = m[12].Board.Lead!;
        Assert.Equal(LeadKind.Decision, d12.Kind);
        Assert.Equal("Mr. Kofi Mensah", d12.Person?.DisplayName);
        Assert.Contains("Port Fuel Haulers is supplying fuel at Spoke B on a handshake", d12.Text);
    }

    public static IEnumerable<object[]> AllGoldens() =>
        Directory.GetDirectories(Path.Combine(Repo.Root, "scenarios"))
                 .Where(d => Directory.Exists(Path.Combine(d, "golden")))
                 .SelectMany(d => Directory.GetFiles(Path.Combine(d, "golden"), "*.json")
                                           .Select(f => new object[] { Path.GetFileName(d), Path.GetFileNameWithoutExtension(f) }));

    /// <summary>M2R-20: a board item about a capability at a site names who would bring it to the shop whenever the cast
    /// has someone at that site, and the person on that site's card for the capability when there is one.</summary>
    [Theory]
    [MemberData(nameof(AllGoldens))]
    public void Site_news_comes_from_someone_at_the_site(string scenario, string log)
    {
        var b = Repo.Load(scenario);
        var l = InputLog.Load(Golden(scenario, log));
        var s = Session.Create(b, l.Seed, l.PrepChoices);
        while (!s.Finished)
        {
            var board = s.View.MorningBoard();
            foreach (var item in board.Items.Where(i => i.LinkKind == BoardLinkKind.SiteCapability))
            {
                var site = item.LinkId[..item.LinkId.IndexOf('.')];
                var cap = item.LinkId[(item.LinkId.IndexOf('.') + 1)..];
                var card = b.Scenario.RequirementCards.Where(c => c.Site == site && c.Capability == cap && c.Person is not null)
                                                      .OrderBy(c => c.SubmittedDay).FirstOrDefault();
                var cast = b.Scenario.People.FirstOrDefault(p => p.Site == site && p.Role is "customer" or "commander");
                var expected = card?.Person ?? cast?.Id;
                Assert.Equal(expected, item.Person?.Id);
            }
            if (board.Lead is { LinkKind: BoardLinkKind.SiteCapability } lead)
                Assert.Equal(board.Items.First(i => i.RecordId == lead.RecordId).Person?.Id, lead.Person?.Id);
            foreach (var a in l.Actions.Where(a => a.Day == s.Day)) s.Apply(a);
            s.AdvanceDay();
        }
    }

    [Theory]
    [InlineData("spoke-b", "approach1-as-written")]
    [InlineData("spoke-b", "approach1-park")]
    [InlineData("spoke-b", "approach2-fix-scope-and-bridge")]
    [InlineData("spoke-b", "approach3-uca-turnkey-with-bridge")]
    [InlineData("spoke-b", "approach4-enduring-only")]
    public void The_fixed_rule_holds_every_morning(string scenario, string log)
    {
        var b = Repo.Load(scenario);
        var l = InputLog.Load(Golden(scenario, log));
        var s = Session.Create(b, l.Seed, l.PrepChoices);
        var quiet = 0;
        while (!s.Finished)
        {
            var hash = s.StateHash();
            var board = s.View.MorningBoard();
            Assert.Equal(hash, s.StateHash()); // building the lead changes nothing
            var lead = board.Lead;
            var openCommitment = s.View.Commitments.Any(c => c.Decision == CommitmentDecision.Open);
            if (openCommitment) Assert.Equal(LeadKind.Decision, lead?.Kind);
            if (lead is null)
            {
                quiet++;
                Assert.DoesNotContain(board.Items, i => i.Severity == BoardSeverity.Alert);
            }
            else
            {
                Assert.DoesNotContain('\n', lead.Text);
                if (lead.Kind == LeadKind.Clock) Assert.InRange(lead.DueDay!.Value - s.Day, 1, Session.LeadClockDays);
                if (lead.Kind == LeadKind.Alert) Assert.Equal(board.Items.First(i => i.Severity == BoardSeverity.Alert).Text, lead.Text);
            }
            foreach (var a in l.Actions.Where(a => a.Day == s.Day)) s.Apply(a);
            s.AdvanceDay();
        }
        Assert.True(quiet > 0, "some days should be quiet so Continue can skip them");
        Assert.Null(s.View.MorningBoard().Lead);
    }
}
