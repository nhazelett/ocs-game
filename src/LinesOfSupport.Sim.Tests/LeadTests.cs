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
        var d12 = m[12].Board.Lead!;
        Assert.Equal(LeadKind.Decision, d12.Kind);
        Assert.Equal("Mr. Kofi Mensah", d12.Person?.DisplayName);
        Assert.Contains("Port Fuel Haulers is supplying fuel at Spoke B on a handshake", d12.Text);
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
