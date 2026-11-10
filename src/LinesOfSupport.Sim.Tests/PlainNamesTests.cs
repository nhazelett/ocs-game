using System.Text.RegularExpressions;
using LinesOfSupport.Sim.Core;
using LinesOfSupport.Sim.Data;
using LinesOfSupport.Sim.Policies;

namespace LinesOfSupport.Sim.Tests;

/// <summary>M2R-07 (design doc R6): no identifier a player could see. Every golden and the three policies on every
/// scenario; records, morning boards and leads, debrief text and the previews of every action played.</summary>
public class PlainNamesTests
{
    public static IEnumerable<object[]> Scenarios() => [["spoke-b"], ["spoke-b-90"], ["sustainment"], ["tutorial"]];

    /// <summary>Identifier-shaped ids (a digit or an inner capital) from the scenario, the rules and the session's own arrangements.</summary>
    private static Regex Ids(Session s)
    {
        var sc = s.Scenario;
        var ids = sc.Sites.Select(x => x.Id).Concat(sc.Vendors.Select(x => x.Id)).Concat(sc.RequirementCards.Select(x => x.Id))
            .Concat(sc.Islands.Select(x => x.Id)).Concat(sc.FundingLines.Select(x => x.Id)).Concat(sc.InheritedArrangements.Select(x => x.Id))
            .Concat(sc.Vehicles.Select(x => x.Id)).Concat(sc.Events.Select(x => x.Id)).Concat(sc.TransportLinks.Select(x => x.Id))
            .Concat(sc.People.Select(x => x.Id)).Concat(s.Rules.Instruments.Keys).Concat(s.Rules.PrepChoices.Select(x => x.Id))
            .Concat(s.Arrangements.Select(a => a.Id)).Concat(s.PerformanceIssues.Select(i => i.Id)).Concat(s.Commitments.Select(c => c.Id))
            .Where(id => Regex.IsMatch(id, "[0-9]|[a-z][A-Z]")).Distinct().OrderByDescending(id => id.Length).ToList();
        return new Regex(@"\b(" + string.Join("|", ids.Select(Regex.Escape)) + @")\b");
    }

    internal static IEnumerable<(string Where, string Text)> DebriefAndPreviews(Session s)
    {
        var d = s.View.Debrief();
        foreach (var g in d.Gaps)
        {
            yield return ("gap label", g.Label);
            foreach (var f in g.Factors) yield return ($"gap factor {f.Kind}", f.Text);
        }
        foreach (var e in d.Events) yield return ("debrief event", e.Text);
        foreach (var e in d.Decisions) yield return ("debrief decision", e.Text);
        foreach (var e in d.Commitments) yield return ("debrief commitment", e.Text);
        foreach (var e in d.Claims ?? []) yield return ("debrief claim", e.Text);
        foreach (var e in d.Timeline) yield return ("debrief band", e.Label);
        if (d.Summary is { } sum) yield return ("debrief summary", sum.Text);
        foreach (var r in s.Records) yield return ($"record {r.Kind}", r.Text);
        foreach (var w in s.Work) yield return ("work label", w.Label);
        foreach (var a in s.AppliedActions)
        {
            var p = a switch
            {
                AwardAction x => s.View.Preview(x),
                AcsaOrderAction x => s.View.PreviewAcsa(x),
                EstablishBpaAction x => s.View.PreviewEstablish(x),
                EstablishIdiqAction x => s.View.PreviewEstablish(x),
                RequestExternalSupportAction x => s.View.PreviewExternalSupport(x),
                HostNationRequestAction x => s.View.PreviewHostNation(x),
                ReviewFilesAction x => s.View.PreviewReviewFiles(x),
                FindFinanceAction or JoinStaffMeetingAction or AskScoAction or MeetVendorAction or EngageJ4Action => s.View.PreviewVisit(a),
                _ => null,
            };
            if (p is null) continue;
            foreach (var x in p.HardStops) yield return ($"preview stop {a.GetType().Name}", x);
            foreach (var x in p.SoftGaps) yield return ($"preview gap {a.GetType().Name}", x);
        }
    }

    private static void NoIds(Session s, IEnumerable<(string Where, string Text)> texts, string run)
    {
        var rx = Ids(s);
        foreach (var (where, text) in texts)
            if (rx.Match(text) is { Success: true } m)
                Assert.Fail($"{run}: '{m.Value}' in {where}: {text}");
    }

    [Theory]
    [MemberData(nameof(Scenarios))]
    public void Goldens_show_no_identifiers(string scenario)
    {
        var bundle = Repo.Load(scenario);
        var logs = Directory.GetFiles(Path.Combine(Repo.Scenario(scenario), "golden"), "*.json").Order(StringComparer.Ordinal).ToList();
        Assert.NotEmpty(logs);
        foreach (var path in logs)
        {
            var s = Replay.Run(bundle, InputLog.Load(path));
            NoIds(s, DebriefAndPreviews(s), $"{scenario}/{Path.GetFileName(path)}");
        }
    }

    [Theory]
    [MemberData(nameof(Scenarios))]
    public void Policies_show_no_identifiers_on_any_morning(string scenario)
    {
        var bundle = Repo.Load(scenario);
        foreach (var name in PolicyCatalog.Names)
        {
            var s = Session.Create(bundle, 1);
            var policy = PolicyCatalog.Create(name);
            var mornings = new List<(string, string)>();
            while (!s.Finished)
            {
                foreach (var a in policy.Decide(s.View)) s.Apply(a);
                s.AdvanceDay();
                var board = s.View.MorningBoard();
                mornings.AddRange(board.Items.Select(i => ($"board day {s.Day}", i.Text)));
                if (board.Lead is { } lead) mornings.Add(($"lead day {s.Day}", lead.Text));
            }
            NoIds(s, mornings.Concat(DebriefAndPreviews(s)), $"{scenario}/{name}");
        }
    }

    [Fact]
    public void Names_for_game_rows()
    {
        var s = Session.Create(Repo.Load("spoke-b-90"), 1);
        s.Apply(new AwardAction
        {
            Id = "po", Instrument = "purchaseOrder", Vendor = "vendorC", Site = "spokeBSite", Capability = "power",
            Lines = ["generation"], Quantity = 60, DurationDays = 10, FundingLine = "om1",
        });
        Assert.Equal("Harbor Equipment Co power at Spoke B", s.View.ArrangementName("po"));
        Assert.Equal("the O&M line good through day 75", s.View.FundingName("om1"));
        Assert.Equal("the O&M line from day 76", s.View.FundingName("om2"));
        Assert.Equal("the request for power for Spoke B", s.View.CardName("req0007"));
        var single = Session.Create(Repo.Load("spoke-b"), 1);
        Assert.Equal("the O&M line", single.View.FundingName("om1"));
    }
}
